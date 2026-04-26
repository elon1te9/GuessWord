using GuessWord.Api.Data;
using GuessWord.Api.Interfaces;
using GuessWord.Api.Models;
using GuessWord.Shared.Enums;
using GuessWord.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace GuessWord.Api.Services
{
    public class RoomService : IRoomService
    {
        private const int RoomCodeLength = 6;
        private const string RoomCodeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        private readonly AppDbContext _context;

        public RoomService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<RoomResponseDto?> CreateRoomAsync(int userId)
        {
            var hasActiveGame = await _context.GamePlayers
                .AsNoTracking()
                .AnyAsync(gp =>
                    gp.UserId == userId &&
                    gp.Game.Status == GameStatus.InProgress &&
                    (gp.Game.Mode == GameMode.Multiplayer || gp.IsActiveSingleGame));

            if (hasActiveGame)
                return null;

            var existingRoom = await _context.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(r =>
                    r.HostUserId == userId &&
                    r.GameId == null &&
                    r.Status != RoomStatus.Closed);

            if (existingRoom is not null)
            {
                return await BuildRoomResponseAsync(existingRoom.Code, userId)
                    ?? throw new InvalidOperationException("Failed to load existing room.");
            }

            var room = new Room
            {
                Code = await GenerateUniqueCodeAsync(),
                HostUserId = userId,
                Status = RoomStatus.Waiting
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return await BuildRoomResponseAsync(room.Code, userId)
                ?? throw new InvalidOperationException("Failed to create room.");
        }

        public async Task<RoomResponseDto?> JoinRoomAsync(int userId, string code)
        {
            var normalizedCode = NormalizeCode(code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
                return null;

            var hasActiveGame = await _context.GamePlayers
                .AsNoTracking()
                .AnyAsync(gp =>
                    gp.UserId == userId &&
                    gp.Game.Status == GameStatus.InProgress &&
                    (gp.Game.Mode == GameMode.Multiplayer || gp.IsActiveSingleGame));

            if (hasActiveGame)
                return null;

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Code == normalizedCode);

            if (room is null)
                return null;

            if (room.HostUserId == userId)
                return await BuildRoomResponseAsync(room.Code, userId);

            if (room.Status != RoomStatus.Waiting && room.Status != RoomStatus.Full)
                return null;

            if (room.GuestUserId.HasValue && room.GuestUserId.Value != userId)
                return null;

            if (room.GuestUserId != userId)
            {
                room.GuestUserId = userId;
                room.Status = RoomStatus.Full;
                await _context.SaveChangesAsync();
            }

            return await BuildRoomResponseAsync(room.Code, userId);
        }

        public async Task<RoomResponseDto?> GetRoomAsync(int userId, string code)
        {
            var normalizedCode = NormalizeCode(code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
                return null;

            var roomExists = await _context.Rooms
                .AsNoTracking()
                .AnyAsync(r =>
                    r.Code == normalizedCode &&
                    (r.HostUserId == userId || r.GuestUserId == userId));

            if (!roomExists)
                return null;

            return await BuildRoomResponseAsync(normalizedCode, userId);
        }

        public async Task<bool> LeaveRoomAsync(int userId, string code)
        {
            var normalizedCode = NormalizeCode(code);
            if (string.IsNullOrWhiteSpace(normalizedCode))
                return false;

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Code == normalizedCode);

            if (room is null)
                return false;

            if (room.Status == RoomStatus.InGame)
                return false;

            if (room.GuestUserId == userId)
            {
                room.GuestUserId = null;
                room.Status = RoomStatus.Waiting;
            }
            else if (room.HostUserId == userId)
            {
                _context.Rooms.Remove(room);
            }
            else
            {
                return false;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<RoomResponseDto?> BuildRoomResponseAsync(string code, int userId)
        {
            var room = await _context.Rooms
                .AsNoTracking()
                .Include(r => r.HostUser)
                .Include(r => r.GuestUser)
                .FirstOrDefaultAsync(r => r.Code == code);

            if (room is null)
                return null;

            return new RoomResponseDto
            {
                Code = room.Code,
                HostName = GetDisplayName(room.HostUser),
                GuestName = room.GuestUser is null ? null : GetDisplayName(room.GuestUser),
                Status = room.Status,
                GameId = room.GameId,
                IsFull = room.GuestUserId.HasValue,
                CanStartGame = room.GuestUserId.HasValue && room.Status == RoomStatus.Full,
                IsHost = room.HostUserId == userId
            };
        }

        private async Task<string> GenerateUniqueCodeAsync()
        {
            while (true)
            {
                var code = new string(Enumerable.Range(0, RoomCodeLength)
                    .Select(_ => RoomCodeAlphabet[Random.Shared.Next(RoomCodeAlphabet.Length)])
                    .ToArray());

                var exists = await _context.Rooms
                    .AsNoTracking()
                    .AnyAsync(r => r.Code == code);

                if (!exists)
                    return code;
            }
        }

        private static string NormalizeCode(string code)
        {
            return code.Trim().ToUpperInvariant();
        }

        private static string GetDisplayName(User user)
        {
            return string.IsNullOrWhiteSpace(user.Name) ? user.Login : user.Name;
        }
    }
}
