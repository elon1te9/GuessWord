using Microsoft.AspNetCore.SignalR;

namespace GuessWord.Api.Hubs
{
    public class GameHub : Hub
    {
        public async Task JoinRoomGroup(string roomCode)
        {
            var normalizedCode = NormalizeRoomCode(roomCode);
            await Groups.AddToGroupAsync(Context.ConnectionId, $"room-{normalizedCode}");
        }

        public async Task LeaveRoomGroup(string roomCode)
        {
            var normalizedCode = NormalizeRoomCode(roomCode);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room-{normalizedCode}");
        }

        public async Task JoinGameGroup(int gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"game-{gameId}");
        }

        public async Task LeaveGameGroup(int gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game-{gameId}");
        }

        private static string NormalizeRoomCode(string roomCode)
        {
            return roomCode.Trim().ToUpperInvariant();
        }
    }
}
