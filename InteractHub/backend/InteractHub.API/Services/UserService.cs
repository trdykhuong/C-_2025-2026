using InteractHub.API.DTOs;
using InteractHub.API.Repositories.Interfaces;

namespace InteractHub.API.Services;

public class UserService
{
    private readonly IUserRepository _repo;
    public UserService(IUserRepository repo) => _repo = repo;

    public async Task<ApiResult<UserProfileDTO>> GetProfileAsync(string targetId, string currentUserId)
    {
        var user = await _repo.FindByIdAsync(targetId);
        if (user == null) return ApiResult<UserProfileDTO>.Fail("Không tìm thấy người dùng.");

        var postCount   = await _repo.CountPostsAsync(targetId);
        var friendCount = await _repo.CountFriendsAsync(targetId);
        var friendship  = await _repo.FindFriendshipAsync(currentUserId, targetId);

        string status = "none";
        if (friendship != null)
        {
            if      (friendship.Status == "accepted")                  status = "accepted";
            else if (friendship.SenderId == currentUserId)             status = "pending_sent";
            else                                                        status = "pending_received";
        }

        return ApiResult<UserProfileDTO>.Ok(new UserProfileDTO
        {
            Id               = user.Id,
            UserName         = user.UserName!,
            FullName         = user.FullName,
            Bio              = user.Bio,
            AvatarUrl        = user.AvatarUrl,
            CoverUrl         = user.CoverUrl,
            CreatedAt        = user.CreatedAt,
            PostCount        = postCount,
            FriendCount      = friendCount,
            FriendshipStatus = status,
            FriendshipId     = friendship?.Id,
        });
    }

    public async Task<ApiResult<bool>> UpdateProfileAsync(string userId, UpdateProfileDTO dto)
    {
        var user = await _repo.FindByIdAsync(userId);
        if (user == null) return ApiResult<bool>.Fail("Không tìm thấy.");

        if (dto.FullName  != null)                user.FullName  = dto.FullName;
        if (dto.Bio       != null)                user.Bio       = dto.Bio;
        if (!string.IsNullOrEmpty(dto.AvatarUrl)) user.AvatarUrl = dto.AvatarUrl;
        if (!string.IsNullOrEmpty(dto.CoverUrl))  user.CoverUrl  = dto.CoverUrl;

        var result = await _repo.UpdateAsync(user);
        if (!result.Succeeded)
            return ApiResult<bool>.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));

        return ApiResult<bool>.Ok(true, "Cập nhật thành công!");
    }

    public async Task<List<UserSummaryDTO>> SearchAsync(string keyword)
    {
        var users = await _repo.SearchAsync(keyword);
        return users.Select(u => new UserSummaryDTO
        {
            Id        = u.Id,
            UserName  = u.UserName!,
            FullName  = u.FullName,
            AvatarUrl = u.AvatarUrl,
        }).ToList();
    }
}
