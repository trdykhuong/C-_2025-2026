using InteractHub.API.DTOs;
using InteractHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InteractHub.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InteractHub.API.Controllers;


[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _auth.RegisterAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _auth.LoginAsync(dto);
        return result.Success ? Ok(result) : Unauthorized(result);
    }


    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var fullName = User.FindFirstValue("fullName")!;
        return Ok(ApiResponse<object>.Ok(new { userId, userName, fullName }));
    }
}