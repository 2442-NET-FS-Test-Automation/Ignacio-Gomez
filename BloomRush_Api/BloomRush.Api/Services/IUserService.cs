namespace BloomRush.Api.Services;

public interface IUserService 
{
    public Task<string?> RegisterAsync(string username, string password);
    public Task<AuthUser?> ValidateAsync(string username, string password);
}

public record AuthUser(string Username, string Role);
