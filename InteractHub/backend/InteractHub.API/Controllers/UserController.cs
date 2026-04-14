using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : BaseController
{
    private readonly UserService _userService;
    public UserController(UserService userService) => _userService = userService;

    // GET /api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(string id)
    {
        var result = await _userService.GetProfileAsync(id, CurrentUserId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // PUT /api/users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
    {
        var result = await _userService.UpdateProfileAsync(CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/users/search?keyword=abc
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
        => Ok(await _userService.SearchAsync(keyword));
}
