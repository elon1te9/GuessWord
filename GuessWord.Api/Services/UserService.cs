using GuessWord.Api.Data;
using GuessWord.Api.Interfaces;
using GuessWord.Api.Models;
using GuessWord.Shared.Enums;
using GuessWord.Shared.Requests;
using GuessWord.Shared.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GuessWord.Api.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly PasswordHasher<User> _passwordHasher;

        public UserService(AppDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<AuthResponseDto?> Register(RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
                return null;

            var normalizedLogin = request.Login.Trim();
            var loginExists = await _context.Users.AnyAsync(x => x.Login == normalizedLogin);
            if (loginExists)
                return null;

            var normalizedName = request.Name.Trim();
            var nameExists = await _context.Users.AnyAsync(x => x.Name == normalizedName);
            if (nameExists)
                return null;

            var user = new User
            {
                Login = normalizedLogin,
                Name = normalizedName,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var token = _jwtService.GenerateToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Token = token,
                Login = user.Login,
                Name = user.Name
            };
        }

        public async Task<AuthResponseDto?> Login(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
                return null;

            var normalizedLogin = request.Login.Trim();

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Login == normalizedLogin);
            if (user is null)
                return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed)
                return null;

            var token = _jwtService.GenerateToken(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Token = token,
                Login = user.Login,
                Name = user.Name!
            };
        }

        public async Task<UserProfileResponseDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user is null)
                return null;

            var completedResults = await _context.GamePlayers
                .AsNoTracking()
                .Where(gp => gp.UserId == userId && gp.Game.Status == GameStatus.Finished)
                .Select(gp => gp.Result)
                .ToListAsync();

            var gamesCount = completedResults.Count;
            var winsCount = completedResults.Count(result => result == GamePlayerResult.Won);
            var lossesCount = completedResults.Count(result => result != GamePlayerResult.Won);
            var winRate = gamesCount == 0 ? 0 : winsCount * 100 / gamesCount;

            return new UserProfileResponseDto
            {
                Login = user.Login,
                Name = user.Name ?? user.Login,
                GamesCount = gamesCount,
                WinsCount = winsCount,
                LossesCount = lossesCount,
                WinRate = winRate
            };
        }

        public async Task<LeaderboardResponseDto> GetLeaderboardAsync()
        {
            const int topLimit = 50;

            var leaderboardData = await _context.GamePlayers
                .AsNoTracking()
                .Where(gp => gp.Game.Status == GameStatus.Finished)
                .GroupBy(gp => new
                {
                    gp.UserId,
                    gp.User.Login,
                    gp.User.Name
                })
                .Select(group => new
                {
                    group.Key.UserId,
                    DisplayName = string.IsNullOrWhiteSpace(group.Key.Name)
                        ? group.Key.Login
                        : group.Key.Name!,
                    GamesCount = group.Count(),
                    WinsCount = group.Count(gp => gp.Result == GamePlayerResult.Won)
                })
                .ToListAsync();

            var orderedEntries = leaderboardData
                .Select(entry => new
                {
                    entry.UserId,
                    entry.DisplayName,
                    entry.GamesCount,
                    entry.WinsCount,
                    WinRate = entry.GamesCount == 0 ? 0 : entry.WinsCount * 100 / entry.GamesCount
                })
                .OrderByDescending(entry => entry.WinsCount)
                .ThenByDescending(entry => entry.WinRate)
                .ThenByDescending(entry => entry.GamesCount)
                .ThenBy(entry => entry.DisplayName)
                .Take(topLimit)
                .ToList();

            var entries = orderedEntries
                .Select((entry, index) => new LeaderboardEntryResponseDto
                {
                    Place = index + 1,
                    UserId = entry.UserId,
                    DisplayName = entry.DisplayName,
                    WinsCount = entry.WinsCount,
                    GamesCount = entry.GamesCount,
                    WinRate = entry.WinRate
                })
                .ToList();

            return new LeaderboardResponseDto
            {
                Entries = entries,
                TotalPlayers = leaderboardData.Count
            };
        }
    }
}
