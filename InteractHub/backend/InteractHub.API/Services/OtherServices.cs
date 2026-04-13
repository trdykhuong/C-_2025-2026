using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Services;

// ════════════════════════════════════════════════════════════
// COMMENT SERVICE
// ════════════════════════════════════════════════════════════
public class CommentService
{
    private readonly AppDbContext        _db;
    private readonly NotificationService _notifService;

    public CommentService(AppDbContext db, NotificationService notifService)
    {
        _db           = db;
        _notifService = notifService;
    }

    public async Task<List<CommentResponseDTO>> GetByPostAsync(int postId)
    {
        return await _db.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponseDTO
            {
                Id        = c.Id,
                Content   = c.Content,
                CreatedAt = c.CreatedAt,
                Author    = new UserSummaryDTO
                {
                    Id        = c.User.Id,
                    UserName  = c.User.UserName!,
                    FullName  = c.User.FullName,
                    AvatarUrl = c.User.AvatarUrl,
                },
            })
            .ToListAsync();
    }

    public async Task<ApiResult<CommentResponseDTO>> CreateAsync(int postId, string userId, CreateCommentDTO dto)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return ApiResult<CommentResponseDTO>.Fail("Bài đăng không tồn tại.");

        var comment = new Comment { PostId = postId, UserId = userId, Content = dto.Content };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
        await _db.Entry(comment).Reference(c => c.User).LoadAsync();

        if (post.UserId != userId)
            await _notifService.CreateAsync(post.UserId, userId, "comment", "đã bình luận bài viết của bạn.", postId);

        return ApiResult<CommentResponseDTO>.Ok(new CommentResponseDTO
        {
            Id        = comment.Id,
            Content   = comment.Content,
            CreatedAt = comment.CreatedAt,
            Author    = new UserSummaryDTO
            {
                Id        = comment.User.Id,
                UserName  = comment.User.UserName!,
                FullName  = comment.User.FullName,
                AvatarUrl = comment.User.AvatarUrl,
            },
        });
    }

    public async Task<ApiResult<CommentResponseDTO>> UpdateAsync(int commentId, string userId, UpdateCommentDTO dto)
    {
        var comment = await _db.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted);
        if (comment == null)           return ApiResult<CommentResponseDTO>.Fail("Không tìm thấy comment.");
        if (comment.UserId != userId)  return ApiResult<CommentResponseDTO>.Fail("Không có quyền sửa.");

        comment.Content = dto.Content;
        await _db.SaveChangesAsync();

        return ApiResult<CommentResponseDTO>.Ok(new CommentResponseDTO
        {
            Id        = comment.Id,
            Content   = comment.Content,
            CreatedAt = comment.CreatedAt,
            Author    = new UserSummaryDTO
            {
                Id        = comment.User.Id,
                UserName  = comment.User.UserName!,
                FullName  = comment.User.FullName,
                AvatarUrl = comment.User.AvatarUrl,
            },
        });
    }

    public async Task<ApiResult<bool>> DeleteAsync(int commentId, string userId, bool isAdmin = false)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment == null)           return ApiResult<bool>.Fail("Không tìm thấy comment.");
        if (comment.UserId != userId && !isAdmin) return ApiResult<bool>.Fail("Không có quyền xóa.");

        comment.IsDeleted = true;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Ok(true, "Đã xóa comment.");
    }
}

// ════════════════════════════════════════════════════════════
// FRIEND SERVICE
// ════════════════════════════════════════════════════════════
public class FriendService
{
    private readonly AppDbContext        _db;
    private readonly NotificationService _notifService;

    public FriendService(AppDbContext db, NotificationService notifService)
    {
        _db           = db;
        _notifService = notifService;
    }

    public async Task<ApiResult<FriendshipResponseDTO>> SendRequestAsync(string senderId, string receiverId)
    {
        if (senderId == receiverId)
            return ApiResult<FriendshipResponseDTO>.Fail("Không thể kết bạn với chính mình.");

        var receiver = await _db.Users.FindAsync(receiverId);
        if (receiver == null)
            return ApiResult<FriendshipResponseDTO>.Fail("Người dùng không tồn tại.");

        var existed = await _db.Friendships.AnyAsync(f =>
            (f.SenderId == senderId && f.ReceiverId == receiverId) ||
            (f.SenderId == receiverId && f.ReceiverId == senderId));

        if (existed)
            return ApiResult<FriendshipResponseDTO>.Fail("Đã có quan hệ bạn bè hoặc đang chờ xác nhận.");

        var friendship = new Friendship { SenderId = senderId, ReceiverId = receiverId };
        _db.Friendships.Add(friendship);
        await _db.SaveChangesAsync();

        await _notifService.CreateAsync(receiverId, senderId, "friend_request", "đã gửi lời mời kết bạn.");

        return ApiResult<FriendshipResponseDTO>.Ok(new FriendshipResponseDTO
        {
            Id        = friendship.Id,
            Status    = friendship.Status,
            CreatedAt = friendship.CreatedAt,
            OtherUser = new UserSummaryDTO
            {
                Id       = receiver.Id,
                UserName = receiver.UserName!,
                FullName = receiver.FullName,
                AvatarUrl= receiver.AvatarUrl,
            },
        });
    }

    public async Task<ApiResult<FriendshipResponseDTO>> RespondAsync(int friendshipId, string userId, bool accept)
    {
        var friendship = await _db.Friendships
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .FirstOrDefaultAsync(f => f.Id == friendshipId);

        if (friendship == null)
            return ApiResult<FriendshipResponseDTO>.Fail("Không tìm thấy lời mời.");

        if (friendship.ReceiverId != userId)
            return ApiResult<FriendshipResponseDTO>.Fail("Không có quyền xử lý lời mời này.");

        friendship.Status    = accept ? "accepted" : "rejected";
        friendship.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

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
        var list = await _db.Friendships
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted")
            .ToListAsync();

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
        var list = await _db.Friendships
            .Include(f => f.Sender)
            .Where(f => f.ReceiverId == userId && f.Status == "pending")
            .ToListAsync();

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
        var f = await _db.Friendships.FindAsync(friendshipId);
        if (f == null) return ApiResult<bool>.Fail("Không tìm thấy.");
        if (f.SenderId != userId && f.ReceiverId != userId) return ApiResult<bool>.Fail("Không có quyền.");

        _db.Friendships.Remove(f);
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Ok(true, "Đã xóa bạn bè.");
    }

    // Hủy kết bạn / hủy lời mời bằng userId đối phương
    public async Task<ApiResult<bool>> RemoveByUserIdAsync(string currentUserId, string otherUserId)
    {
        var f = await _db.Friendships.FirstOrDefaultAsync(x =>
            (x.SenderId == currentUserId && x.ReceiverId == otherUserId) ||
            (x.SenderId == otherUserId   && x.ReceiverId == currentUserId));

        if (f == null) return ApiResult<bool>.Fail("Không tìm thấy quan hệ.");

        _db.Friendships.Remove(f);
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Ok(true, "Đã hủy.");
    }
}

// ════════════════════════════════════════════════════════════
// STORY SERVICE
// ════════════════════════════════════════════════════════════
public class StoryService
{
    private readonly AppDbContext _db;
    public StoryService(AppDbContext db) => _db = db;

    public async Task<List<StoryResponseDTO>> GetFeedAsync(string userId)
    {
        var friendIds = await _db.Friendships
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted")
            .Select(f => f.SenderId == userId ? f.ReceiverId : f.SenderId)
            .ToListAsync();

        friendIds.Add(userId);

        return await _db.Stories
            .Include(s => s.User)
            .Include(s => s.StoryViews)
            .Where(s => friendIds.Contains(s.UserId) && s.ExpiresAt > DateTime.UtcNow)
            .Where(s => s.Visibility == "public"
                     || s.Visibility == "friends"
                     || (s.Visibility == "private" && s.UserId == userId))
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StoryResponseDTO
            {
                Id         = s.Id,
                ImageUrl   = s.ImageUrl,
                Caption    = s.Caption,
                Visibility = s.Visibility,
                CreatedAt  = s.CreatedAt,
                ExpiresAt  = s.ExpiresAt,
                ViewCount  = s.StoryViews.Count,
                Author     = new UserSummaryDTO
                {
                    Id        = s.User.Id,
                    UserName  = s.User.UserName!,
                    FullName  = s.User.FullName,
                    AvatarUrl = s.User.AvatarUrl,
                },
            })
            .ToListAsync();
    }

    public async Task<ApiResult<StoryResponseDTO>> CreateAsync(string userId, CreateStoryDTO dto)
    {
        var story = new Story { UserId = userId, ImageUrl = dto.ImageUrl, Caption = dto.Caption, Visibility = dto.Visibility };
        _db.Stories.Add(story);
        await _db.SaveChangesAsync();
        await _db.Entry(story).Reference(s => s.User).LoadAsync();

        return ApiResult<StoryResponseDTO>.Ok(new StoryResponseDTO
        {
            Id         = story.Id,
            ImageUrl   = story.ImageUrl,
            Caption    = story.Caption,
            Visibility = story.Visibility,
            CreatedAt  = story.CreatedAt,
            ExpiresAt  = story.ExpiresAt,
            ViewCount  = 0,
            Author     = new UserSummaryDTO
            {
                Id        = story.User.Id,
                UserName  = story.User.UserName!,
                FullName  = story.User.FullName,
                AvatarUrl = story.User.AvatarUrl,
            },
        });
    }

    public async Task RecordViewAsync(int storyId, string userId)
    {
        var story = await _db.Stories.FindAsync(storyId);
        if (story == null || story.UserId == userId) return; // không tính lượt xem của chính mình
        var exists = await _db.StoryViews.AnyAsync(v => v.StoryId == storyId && v.UserId == userId);
        if (!exists)
        {
            _db.StoryViews.Add(new StoryView { StoryId = storyId, UserId = userId });
            await _db.SaveChangesAsync();
        }
    }

    public async Task<ApiResult<bool>> DeleteAsync(int storyId, string userId)
    {
        var story = await _db.Stories.FindAsync(storyId);
        if (story == null)           return ApiResult<bool>.Fail("Không tìm thấy story.");
        if (story.UserId != userId)  return ApiResult<bool>.Fail("Không có quyền xóa.");

        _db.Stories.Remove(story);
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Ok(true);
    }
}

// ════════════════════════════════════════════════════════════
// NOTIFICATION SERVICE
// ════════════════════════════════════════════════════════════
public class NotificationService
{
    private readonly AppDbContext _db;
    public NotificationService(AppDbContext db) => _db = db;

    public async Task CreateAsync(string toUserId, string fromUserId, string type, string message, int? postId = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId        = toUserId,
            ActorId       = fromUserId,
            Type          = type,
            Message       = message,
            RelatedPostId = postId,
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<NotificationResponseDTO>> GetAllAsync(string userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponseDTO
            {
                Id            = n.Id,
                Message       = n.Message,
                Type          = n.Type,
                IsRead        = n.IsRead,
                CreatedAt     = n.CreatedAt,
                RelatedPostId = n.RelatedPostId,
            })
            .ToListAsync();
    }

    public async Task<ApiResult<bool>> MarkReadAsync(int id, string userId)
    {
        var n = await _db.Notifications.FindAsync(id);
        if (n == null || n.UserId != userId) return ApiResult<bool>.Fail("Không tìm thấy.");
        n.IsRead = true;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Ok(true);
    }

    public async Task MarkAllReadAsync(string userId)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}

// ════════════════════════════════════════════════════════════
// USER SERVICE
// ════════════════════════════════════════════════════════════
public class UserService
{
    private readonly AppDbContext        _db;
    private readonly UserManager<AppUser> _userManager;

    public UserService(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    public async Task<ApiResult<UserProfileDTO>> GetProfileAsync(string targetId, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(targetId);
        if (user == null) return ApiResult<UserProfileDTO>.Fail("Không tìm thấy người dùng.");

        var postCount   = await _db.Posts.CountAsync(p => p.UserId == targetId && !p.IsDeleted);
        var friendCount = await _db.Friendships.CountAsync(f =>
            (f.SenderId == targetId || f.ReceiverId == targetId) && f.Status == "accepted");

        var friendship = await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.SenderId == currentUserId && f.ReceiverId == targetId) ||
            (f.SenderId == targetId && f.ReceiverId == currentUserId));

        string status = "none";
        if (friendship != null)
        {
            if (friendship.Status == "accepted") status = "accepted";
            else if (friendship.SenderId == currentUserId) status = "pending_sent";
            else status = "pending_received";
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
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ApiResult<bool>.Fail("Không tìm thấy.");

        if (dto.FullName  != null)                     user.FullName  = dto.FullName;
        if (dto.Bio       != null)                     user.Bio       = dto.Bio;
        if (!string.IsNullOrEmpty(dto.AvatarUrl))      user.AvatarUrl = dto.AvatarUrl;
        if (!string.IsNullOrEmpty(dto.CoverUrl))       user.CoverUrl  = dto.CoverUrl;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ApiResult<bool>.Fail(string.Join(", ", result.Errors.Select(e => e.Description)));

        return ApiResult<bool>.Ok(true, "Cập nhật thành công!");
    }

    public async Task<List<UserSummaryDTO>> SearchAsync(string keyword)
    {
        return await _db.Users
            .Where(u => u.UserName!.Contains(keyword) || u.FullName.Contains(keyword))
            .Take(20)
            .Select(u => new UserSummaryDTO
            {
                Id        = u.Id,
                UserName  = u.UserName!,
                FullName  = u.FullName,
                AvatarUrl = u.AvatarUrl,
            })
            .ToListAsync();
    }
}
