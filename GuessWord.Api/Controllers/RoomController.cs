using GuessWord.Api.Interfaces;
using GuessWord.Shared.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GuessWord.Api.Controllers
{
    [ApiController]
    [Route("api/room")]
    [Authorize]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateRoom()
        {
            var userId = GetUserId();
            return Ok(await _roomService.CreateRoomAsync(userId));
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinRoom([FromBody] JoinRoomRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Code))
                return BadRequest("Invalid room code.");

            var userId = GetUserId();
            var room = await _roomService.JoinRoomAsync(userId, request.Code);
            return room is null ? NotFound() : Ok(room);
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> GetRoom(string code)
        {
            var userId = GetUserId();
            var room = await _roomService.GetRoomAsync(userId, code);
            return room is null ? NotFound() : Ok(room);
        }

        [HttpPost("{code}/leave")]
        public async Task<IActionResult> LeaveRoom(string code)
        {
            var userId = GetUserId();
            var result = await _roomService.LeaveRoomAsync(userId, code);
            return result ? Ok() : NotFound();
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var userId))
                throw new Exception("User is not authorized.");

            return userId;
        }
    }
}
