namespace BloomRush.Api.Services;

public interface ITokenService
{
    string Issue(string username, string role);
}