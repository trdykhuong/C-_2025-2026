using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;

namespace InteractHub.API.Services;

public class FriendService
{
    private readonly IFriendRepository   _repo;
    private readonly NotificationService _notifService;

    public FriendService(IFriendRepository repo, NotificationService notifService)
    {
        _repo         = repo;
        _notifService = notifService;
    }

    public async Task<ApiResult<FriendshipResponseDTO>> SendRequestAsync(string senderId, string receiverId)
    {
        if (senderId == receiverId)
            return ApiResult<FriendshipResponseDTO>.Fail("Không thể kết bạn với chính mình.");

        var receiver = await _repo.FindUserAsync(receiverId);
        if (receiver == null)
            return ApiResult<FriendshipResponseDTO>.Fail("Người dùng không tồn tại.");

        if (await _repo.ExistsAsync(senderId, receiverId))
            return ApiResult<FriendshipResponseDTO>.Fail("Đã có quan hệ bạn bè hoặc đang chờ xác nhận.");

        var friendship = new Friendship { SenderId = senderId, ReceiverId = receiverId };
        _repo.Add(friendship);
        await _repo.SaveChangesAsync();

        await _notifService.CreateAsync(receiverId, senderId, "friend_request", "đã gửi lời mời kết bạn.");

        return ApiResult<FriendshipResponseDTO>.Ok(new FriendshipResponseDTO
        {
            Id        = friendship.Id,
            Status    = friendship.Status,
            CreatedAt = friendship.CreatedAt,
            OtherUser = new UserSummaryDTO
            {
                Id        = receiver.Id,
                UserName  = receiver.UserName!,
                FullName  = receiver.FullName,
                AvatarUrl = receiver.AvatarUrl,
            },
        });
    }

    public async Task<ApiResult<FriendshipResponseDTO>> RespondAsync(int friendshipId, string userId, bool accept)
    {
        var friendship = await _repo.FindByIdWithUsersAsync(friendshipId);
        if (friendship == null)
            return ApiResult<FriendshipResponseDTO>.Fail("Không tìm thấy lời mời.");

        if (friendship.ReceiverId != userId)
            return ApiResult<FriendshipResponseDTO>.Fail("Không có quyền xử lý lời mời này.");

        friendship.Status    = accept ? "accepted" : "rejected";
        friendship.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        if (accept)
            await _notifService.CreateAsync(friendship.SenderId, userId, "friend_accepted", "đã chấp nhận lời mời kết bạn.");

        var otherUser = friendship.Sender;
        return ApiResult<FriendshipResponseDTO>.Ok(new FriendshipResponseDTO
        {
            Id        = friendship.Id,
            Status    = friendship.Status,
            CreatedAt = friendship.CreatedAt,
            OtherUser = new UserSummaryDTO
            {
                Id        = otherUser.Id,
                UserName  = otherUser.UserName!,
                FullName  = otherUser.FullName,
                AvatarUrl = otherUser.AvatarUrl,
            },
        });
    }

    public async Task<List<FriendshipResponseDTO>> GetFriendsAsync(string userId)
    {
        var list = await _repo.GetAcceptedAsync(userId);
        return list.Select(f =>
        {
            var other = f.SenderId == userId ? f.Receiver : f.Sender;
            return new FriendshipResponseDTO
            {
                Id        = f.Id,
                Status    = f.Status,
                CreatedAt = f.CreatedAt,
                OtherUser = new UserSummaryDTO
                {
                    Id        = other.Id,
                    UserName  = other.UserName!,
                    FullName  = other.FullName,
                    AvatarUrl = other.AvatarUrl,
                },
            };
        }).ToList();
    }

    public async Task<List<FriendshipResponseDTO>> GetPendingAsync(string userId)
    {
        var list = await _repo.GetPendingAsync(userId);
        return list.Select(f => new FriendshipResponseDTO
        {
            Id        = f.Id,
            Status    = f.Status,
            CreatedAt = f.CreatedAt,
            OtherUser = new UserSummaryDTO
            {
                Id        = f.Sender.Id,
                UserName  = f.Sender.UserName!,
                FullName  = f.Sender.FullName,
                AvatarUrl = f.Sender.AvatarUrl,
            },
        }).ToList();
    }

    public async Task<ApiResult<bool>> RemoveAsync(int friendshipId, string userId)
    {
        var f = await _repo.FindByIdAsync(friendshipId);
        if (f == null) return ApiResult<bool>.Fail("Không tìm thấy.");
        if (f.SenderId != userId && f.ReceiverId != userId) return ApiResult<bool>.Fail("Không có quyền.");

        _repo.Remove(f);
        await _repo.SaveChangesAsync();
        return ApiResult<bool>.Ok(true, "Đã xóa bạn bè.");
    }

    public async Task<ApiResult<bool>> RemoveByUserIdAsync(string currentUserId, string otherUserId)
    {
        var f = await _repo.FindByUsersAsync(currentUserId, otherUserId);
        if (f == null) return ApiResult<bool>.Fail("Không tìm thấy quan hệ.");

        _repo.Remove(f);
        await _repo.SaveChangesAsync();
        return ApiResult<bool>.Ok(true, "Đã hủy.");
    }
}
