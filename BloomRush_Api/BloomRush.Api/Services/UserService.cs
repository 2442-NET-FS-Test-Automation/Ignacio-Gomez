namespace BloomRush.Api.Services;
using Microsoft.EntityFrameworkCore;
using BloomRush.Data;
using BloomRush.Data.Entities;
using Microsoft.AspNetCore.Identity;

public class UserService : IUserService
{
    private readonly BloomRushDbContext _context;
    public UserService(BloomRushDbContext context)
    {
        _context = context;
    }


    public async Task<string?> RegisterAsync(string username, string password)
    {
        var existingUser = await _context.Users
            .Where(user => user.Username == username)
            .FirstOrDefaultAsync();

        if (existingUser is not null)
        {
            return "Username already exists.";
        }

        var user = new User
        {
            Username = username,
            Role = "consumer"
        };

        var hasher = new PasswordHasher<User>();
        user.PasswordHash = hasher.HashPassword(user, password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return null;
    }
        
        
    public async Task<AuthUser?> ValidateAsync(string username, string password)
    {
        var user = await _context.Users
            .Where(user => user.Username == username)
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return null;
        }

        var hasher = new PasswordHasher<User>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return new AuthUser(user.Username, user.Role);
    }
}