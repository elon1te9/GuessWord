using GuessWord.Api.Data;
using GuessWord.Api.Hubs;
using GuessWord.Api.Interfaces;
using GuessWord.Shared.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GuessWord.Api.Controllers
{
    [ApiController]
    [Route("api/room")]
    [Authorize]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly AppDbContext _context;

        public RoomController(
            IRoomService roomService,
            IHubContext<GameHub> hubContext,
            AppDbContext context)
        {
            _roomService = roomService;
            _hubContext = hubContext;
            _context = context;
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

            if (room is null)
                return NotFound();

            await _hubContext.Clients.Group($"room-{NormalizeRoomCode(room.Code)}")
                .SendAsync("RoomUpdated", room);

            return Ok(room);
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
            var normalizedCode = NormalizeRoomCode(code);

            var existingRoom = await _context.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Code == normalizedCode);

            var result = await _roomService.LeaveRoomAsync(userId, normalizedCode);
            if (!result)
                return NotFound();

            if (existingRoom is not null)
            {
                if (existingRoom.HostUserId == userId)
                {
                    await _hubContext.Clients.Group($"room-{normalizedCode}")
                        .SendAsync("RoomClosed", normalizedCode);
                }
                else
                {
                    var updatedRoom = await _roomService.GetRoomAsync(existingRoom.HostUserId, normalizedCode);

                    if (updatedRoom is not null)
                    {
                        await _hubContext.Clients.Group($"room-{normalizedCode}")
                            .SendAsync("RoomUpdated", updatedRoom);
                    }
                    else
                    {
                        await _hubContext.Clients.Group($"room-{normalizedCode}")
                            .SendAsync("RoomClosed", normalizedCode);
                    }
                }
            }

            return Ok();
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var userId))
                throw new Exception("User is not authorized.");

            return userId;
        }

        private static string NormalizeRoomCode(string code)
        {
            return code.Trim().ToUpperInvariant();
        }
    }
}
