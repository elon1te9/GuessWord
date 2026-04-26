using GuessWord.Shared.Responses;

namespace GuessWord.Api.Interfaces
{
    public interface IRoomService
    {
        Task<RoomResponseDto> CreateRoomAsync(int userId);
        Task<RoomResponseDto?> JoinRoomAsync(int userId, string code);
        Task<RoomResponseDto?> GetRoomAsync(int userId, string code);
        Task<bool> LeaveRoomAsync(int userId, string code);
    }
}
