using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Toolify.AuthService.Data;
using Toolify.AuthService.DTO;
using Toolify.AuthService.Models;
using Toolify.AuthService.Security;

namespace Toolify.AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly UserRepository _repo;

    public AuthController(IConfiguration config, UserRepository repo)
    {
        _config = config;
        _repo = repo;
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return BadRequest("Passwords do not match");

        if (_repo.UserExists(request.Email))
            return BadRequest("User already exists");

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            Password = PasswordHasher.Hash(request.Password),
            Role = "User"
        };

        _repo.CreateUser(user);

        return Ok("User registered");
    }

    [HttpPost("login")]
    public IActionResult Login(LoginRequest request)
    {
        var passwordHash = PasswordHasher.Hash(request.Password);
        var user = _repo.GetUserByEmailAndPassword(request.Email, passwordHash);

        if (user == null)
            return Unauthorized("Invalid login or password");

        var token = GenerateJwt(user);

        return Ok(new
        {
            token = token
        });
    }

    private string GenerateJwt(User user)
    {
        var jwtSettings = _config.GetSection("Jwt");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim("id", user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.Now.AddMinutes(
                int.Parse(jwtSettings["ExpireMinutes"]!)
            ),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    [HttpGet("user")]
    public IActionResult GetUser(string email)
    {
        var user = _repo.GetUserByEmail(email);

        if (user == null)
            return NotFound("User not found");

        return Ok(new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Phone
        });
    }
}

