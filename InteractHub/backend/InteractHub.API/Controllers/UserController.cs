using InteractHub.API.DTOs;
using InteractHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InteractHub.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InteractHub.API.Controllers;

// ─── USERS CONTROLLER ────────────────────────────────────────────────────────
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly Microsoft.AspNetCore.Identity.UserManager<Models.AppUser> _userManager;
    private readonly AppDbContext _db;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public UsersController(
        Microsoft.AspNetCore.Identity.UserManager<Models.AppUser> userManager,
        AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <summary>Get a user's public profile</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(ApiResponse<UserProfileDto>.Fail("User not found."));

        var postCount = await _db.Posts.CountAsync(p => p.UserId == id && !p.IsDeleted);
        var friendCount = await _db.Friendships.CountAsync(f =>
            (f.SenderId == id || f.ReceiverId == id) && f.Status == "accepted");

        var friendship = await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.SenderId == UserId && f.ReceiverId == id) ||
            (f.SenderId == id && f.ReceiverId == UserId));

        return Ok(ApiResponse<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullName = user.FullName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            CoverUrl = user.CoverUrl,
            CreatedAt = user.CreatedAt,
            PostCount = postCount,
            FriendCount = friendCount,
            FriendshipStatus = friendship?.Status ?? "none"
        }));
    }

    /// <summary>Update own profile</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.Bio != null) user.Bio = dto.Bio;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Ok(ApiResponse<bool>.Ok(true)) : BadRequest(result.Errors);
    }

    /// <summary>Search users by username or full name</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var users = await _db.Users
            .Where(u => u.UserName!.Contains(q) || u.FullName.Contains(q))
            .Take(20)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl
            })
            .ToListAsync();

        return Ok(ApiResponse<List<UserSummaryDto>>.Ok(users));
    }
}

