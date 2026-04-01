using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Services;

public interface IUsersService
{
    Task<ApiResponse<UserProfileDto>> GetProfileAsync(string targetId, string currentUserId);
    Task<ApiResponse<bool>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
    Task<List<UserSummaryDto>> SearchAsync(string query, string currentUserId);
    Task<ApiResponse<bool>> UploadAvatarAsync(string userId, Stream stream, string fileName, string contentType);
}

public class UsersService : IUsersService
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IFileUploadService _fileUpload;

    public UsersService(AppDbContext db, UserManager<AppUser> userManager, IFileUploadService fileUpload)
    {
        _db = db;
        _userManager = userManager;
        _fileUpload = fileUpload;
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(string targetId, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(targetId);
        if (user == null) return ApiResponse<UserProfileDto>.Fail("User not found.");

        var postCount = await _db.Posts.CountAsync(p => p.UserId == targetId && !p.IsDeleted);
        var friendCount = await _db.Friendships.CountAsync(f =>
            (f.SenderId == targetId || f.ReceiverId == targetId) && f.Status == "accepted");

        var friendship = await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.SenderId == currentUserId && f.ReceiverId == targetId) ||
            (f.SenderId == targetId && f.ReceiverId == currentUserId));

        return ApiResponse<UserProfileDto>.Ok(new UserProfileDto
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
        });
    }

    public async Task<ApiResponse<bool>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ApiResponse<bool>.Fail("User not found.");

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.Bio != null) user.Bio = dto.Bio;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? ApiResponse<bool>.Ok(true, "Profile updated.")
            : ApiResponse<bool>.Fail(result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<List<UserSummaryDto>> SearchAsync(string query, string currentUserId)
    {
        return await _db.Users
            .Where(u => u.Id != currentUserId &&
                        (u.UserName!.Contains(query) || u.FullName.Contains(query)))
            .Take(20)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl
            })
            .ToListAsync();
    }

    public async Task<ApiResponse<bool>> UploadAvatarAsync(string userId, Stream stream, string fileName, string contentType)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ApiResponse<bool>.Fail("User not found.");

        var url = await _fileUpload.UploadAsync(stream, fileName, contentType);
        user.AvatarUrl = url;
        await _userManager.UpdateAsync(user);

        return ApiResponse<bool>.Ok(true, "Avatar updated.");
    }
}
