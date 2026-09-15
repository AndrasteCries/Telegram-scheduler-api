using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TelegramScheduler.Data;
using TelegramScheduler.DTOs.Auth;
using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.DTOs.User;
using TelegramScheduler.Models;

namespace TelegramScheduler.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _config;

    private long CurrentUserId =>
        long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public AuthController(AppDbContext db, IPasswordHasher<User> passwordHasher, IConfiguration config)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<ActionResult<PostResponse>> Register(RegisterRequest request)
    {
        var login = request.Login.Trim();
        var normalizedLogin = login.ToUpperInvariant();

        var alreadyExist = await _db.Users.AnyAsync(user => user.NormalizedLogin == normalizedLogin);

        if (alreadyExist)
            return Conflict(new ProblemDetails
            {
                Title = "Login is already in use",
                Status = StatusCodes.Status409Conflict
            });

        var user = new User
        {
            Login = login,
            NormalizedLogin = normalizedLogin,
            CreatedAtUtc = DateTime.UtcNow,
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created(string.Empty, new RegisterResponse(user.Id, user.Login));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var normalizedLogin = request.Login.Trim().ToUpperInvariant();

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.NormalizedLogin == normalizedLogin);

        if (user == null)
            return Unauthorized();

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized();

        var token = GenerateJwtToken(user);

        Response.Cookies.Append("auth_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = "/"
        });

        return Ok(new { userId = user.Id });
    }

    [Authorize]
    [HttpGet("user")]
    public async Task<ActionResult<UserResponse>> GetUser()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FindAsync(userId);
        if (user == null) return BadRequest(new { status = "not_found" });

        return Ok(new UserResponse
        {
            Id = user.Id,
            Login = user.Login,
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(new { userId });
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}