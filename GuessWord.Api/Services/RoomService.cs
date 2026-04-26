using GuessWord.Api.Data;
using GuessWord.Api.Interfaces;
using GuessWord.Api.Models;
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

        public async Task<RoomResponseDto> CreateRoomAsync(int userId)
        {
            var room = new Room
            {
                Code = await GenerateUniqueCodeAsync(),
                HostUserId = userId
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

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.Code == normalizedCode);

            if (room is null)
                return null;

            if (room.HostUserId == userId)
                return await BuildRoomResponseAsync(room.Code, userId);

            if (room.GuestUserId.HasValue && room.GuestUserId.Value != userId)
                return null;

            if (room.GuestUserId != userId)
            {
                room.GuestUserId = userId;
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

            if (room.GuestUserId == userId)
            {
                room.GuestUserId = null;
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
                IsFull = room.GuestUserId.HasValue,
                CanStartGame = room.HostUserId > 0 && room.GuestUserId.HasValue,
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
