using System.Security.Claims;
using BloomRush.Api.DTOs;
using BloomRush.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BloomRush.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokens;
    private readonly IUserService _users;

    public AuthController(ITokenService tokens, IUserService users)
    {
        _tokens = tokens;
        _users = users;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var error = await _users.RegisterAsync(dto.Username, dto.Password);

        if (error is not null)
        {
            return Conflict(new { error });
        }

        return CreatedAtAction(nameof(Me), null);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _users.ValidateAsync(dto.Username, dto.Password);

        if (user is null)
        {
            return Unauthorized(new { error = "bad credentials" });
        }

        return Ok(new
        {
            token = _tokens.Issue(user.Username, user.Role)
        });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            name = User.Identity?.Name,
            role = User.FindFirstValue(ClaimTypes.Role)
        });
    }
}
