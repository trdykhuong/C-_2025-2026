using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Services;

// ─── FRIENDS ────────────────────────────────────────────────────────────────
public class FriendsService : IFriendsService
{
    private readonly AppDbContext _db;
    private readonly INotificationsService _notifications;

    public FriendsService(AppDbContext db, INotificationsService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<ApiResponse<FriendshipResponseDto>> SendRequestAsync(string senderId, string receiverId)
    {
        if (senderId == receiverId) return ApiResponse<FriendshipResponseDto>.Fail("Cannot send request to yourself.");

        var exists = await _db.Friendships.AnyAsync(f =>
            (f.SenderId == senderId && f.ReceiverId == receiverId) ||
            (f.SenderId == receiverId && f.ReceiverId == senderId));

        if (exists) return ApiResponse<FriendshipResponseDto>.Fail("Request already exists.");

        var receiver = await _db.Users.FindAsync(receiverId);
        if (receiver == null) return ApiResponse<FriendshipResponseDto>.Fail("User not found.");

        var friendship = new Friendship { SenderId = senderId, ReceiverId = receiverId };
        _db.Friendships.Add(friendship);
        await _db.SaveChangesAsync();

        await _notifications.CreateNotificationAsync(receiverId, senderId, "friend_request", "sent you a friend request.");

        return ApiResponse<FriendshipResponseDto>.Ok(await MapAsync(friendship, senderId));
    }

    public async Task<ApiResponse<FriendshipResponseDto>> RespondToRequestAsync(int friendshipId, string userId, bool accept)
    {
        var friendship = await _db.Friendships
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .FirstOrDefaultAsync(f => f.Id == friendshipId);

        if (friendship == null) return ApiResponse<FriendshipResponseDto>.Fail("Request not found.");
        if (friendship.ReceiverId != userId) return ApiResponse<FriendshipResponseDto>.Fail("Unauthorized.");

        friendship.Status = accept ? "accepted" : "rejected";
        friendship.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (accept)
            await _notifications.CreateNotificationAsync(friendship.SenderId, userId, "friend_accepted", "accepted your friend request.");

        return ApiResponse<FriendshipResponseDto>.Ok(await MapAsync(friendship, userId));
    }

    public async Task<List<FriendshipResponseDto>> GetFriendsAsync(string userId)
    {
        var friendships = await _db.Friendships
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted")
            .ToListAsync();

        return friendships.Select(f => MapSync(f, userId)).ToList();
    }

    public async Task<List<FriendshipResponseDto>> GetPendingRequestsAsync(string userId)
    {
        var friendships = await _db.Friendships
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f => f.ReceiverId == userId && f.Status == "pending")
            .ToListAsync();

        return friendships.Select(f => MapSync(f, userId)).ToList();
    }

    public async Task<ApiResponse<bool>> RemoveFriendAsync(int friendshipId, string userId)
    {
        var friendship = await _db.Friendships.FindAsync(friendshipId);
        if (friendship == null) return ApiResponse<bool>.Fail("Not found.");
        if (friendship.SenderId != userId && friendship.ReceiverId != userId)
            return ApiResponse<bool>.Fail("Unauthorized.");

        _db.Friendships.Remove(friendship);
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }

    private async Task<FriendshipResponseDto> MapAsync(Friendship f, string userId)
    {
        if (f.Sender == null) await _db.Entry(f).Reference(x => x.Sender).LoadAsync();
        if (f.Receiver == null) await _db.Entry(f).Reference(x => x.Receiver).LoadAsync();
        return MapSync(f, userId);
    }

    private static FriendshipResponseDto MapSync(Friendship f, string userId)
    {
        var other = f.SenderId == userId ? f.Receiver : f.Sender;
        return new FriendshipResponseDto
        {
            Id = f.Id,
            Status = f.Status,
            CreatedAt = f.CreatedAt,
            OtherUser = new UserSummaryDto
            {
                Id = other.Id,
                UserName = other.UserName!,
                FullName = other.FullName,
                AvatarUrl = other.AvatarUrl
            }
        };
    }
}

// ─── COMMENTS ───────────────────────────────────────────────────────────────
public class CommentsService : ICommentsService
{
    private readonly AppDbContext _db;
    private readonly INotificationsService _notifications;

    public CommentsService(AppDbContext db, INotificationsService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<List<CommentResponseDto>> GetByPostAsync(int postId)
    {
        return await _db.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponseDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                Author = new UserSummaryDto
                {
                    Id = c.User.Id,
                    UserName = c.User.UserName!,
                    FullName = c.User.FullName,
                    AvatarUrl = c.User.AvatarUrl
                }
            })
            .ToListAsync();
    }

    public async Task<ApiResponse<CommentResponseDto>> CreateAsync(int postId, string userId, CreateCommentDto dto)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return ApiResponse<CommentResponseDto>.Fail("Post not found.");

        var comment = new Comment { PostId = postId, UserId = userId, Content = dto.Content };
        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
        await _db.Entry(comment).Reference(c => c.User).LoadAsync();

        if (post.UserId != userId)
            await _notifications.CreateNotificationAsync(post.UserId, userId, "comment", "commented on your post.", postId);

        return ApiResponse<CommentResponseDto>.Ok(new CommentResponseDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            Author = new UserSummaryDto
            {
                Id = comment.User.Id,
                UserName = comment.User.UserName!,
                FullName = comment.User.FullName,
                AvatarUrl = comment.User.AvatarUrl
            }
        });
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int commentId, string userId)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment == null) return ApiResponse<bool>.Fail("Comment not found.");
        if (comment.UserId != userId) return ApiResponse<bool>.Fail("Unauthorized.");

        comment.IsDeleted = true;
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }
}

// ─── NOTIFICATIONS ──────────────────────────────────────────────────────────
public class NotificationsService : INotificationsService
{
    private readonly AppDbContext _db;

    public NotificationsService(AppDbContext db) => _db = db;

    public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(string userId)
    {
        return await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                Message = n.Message,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                RelatedPostId = n.RelatedPostId
            })
            .ToListAsync();
    }

    public async Task<ApiResponse<bool>> MarkAsReadAsync(int id, string userId)
    {
        var notification = await _db.Notifications.FindAsync(id);
        if (notification == null || notification.UserId != userId)
            return ApiResponse<bool>.Fail("Not found.");

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }

    public async Task CreateNotificationAsync(string userId, string actorId, string type, string message, int? relatedPostId = null)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            ActorId = actorId,
            Type = type,
            Message = message,
            RelatedPostId = relatedPostId
        });
        await _db.SaveChangesAsync();
    }
}

// ─── STORIES ────────────────────────────────────────────────────────────────
public class StoriesService : IStoriesService
{
    private readonly AppDbContext _db;

    public StoriesService(AppDbContext db) => _db = db;

    public async Task<List<StoryResponseDto>> GetFeedAsync(string userId)
    {
        var friendIds = await _db.Friendships
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted")
            .Select(f => f.SenderId == userId ? f.ReceiverId : f.SenderId)
            .ToListAsync();

        friendIds.Add(userId);

        return await _db.Stories
            .Include(s => s.User)
            .Where(s => friendIds.Contains(s.UserId) && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StoryResponseDto
            {
                Id = s.Id,
                ImageUrl = s.ImageUrl,
                Caption = s.Caption,
                CreatedAt = s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                Author = new UserSummaryDto
                {
                    Id = s.User.Id,
                    UserName = s.User.UserName!,
                    FullName = s.User.FullName,
                    AvatarUrl = s.User.AvatarUrl
                }
            })
            .ToListAsync();
    }

    public async Task<ApiResponse<StoryResponseDto>> CreateAsync(string userId, CreateStoryDto dto)
    {
        var story = new Story { UserId = userId, ImageUrl = dto.ImageUrl, Caption = dto.Caption };
        _db.Stories.Add(story);
        await _db.SaveChangesAsync();
        await _db.Entry(story).Reference(s => s.User).LoadAsync();

        return ApiResponse<StoryResponseDto>.Ok(new StoryResponseDto
        {
            Id = story.Id,
            ImageUrl = story.ImageUrl,
            Caption = story.Caption,
            CreatedAt = story.CreatedAt,
            ExpiresAt = story.ExpiresAt,
            Author = new UserSummaryDto
            {
                Id = story.User.Id,
                UserName = story.User.UserName!,
                FullName = story.User.FullName,
                AvatarUrl = story.User.AvatarUrl
            }
        });
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, string userId)
    {
        var story = await _db.Stories.FindAsync(id);
        if (story == null) return ApiResponse<bool>.Fail("Not found.");
        if (story.UserId != userId) return ApiResponse<bool>.Fail("Unauthorized.");

        _db.Stories.Remove(story);
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }
}

// ─── FILE UPLOAD (Azure Blob Storage) ───────────────────────────────────────
public class FileUploadService : IFileUploadService
{
    private readonly IConfiguration _config;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(IConfiguration config, ILogger<FileUploadService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        // Azure Blob Storage integration
        // Requires: Azure.Storage.Blobs NuGet package
        // var connectionString = _config["Azure:BlobStorage:ConnectionString"];
        // var containerName = _config["Azure:BlobStorage:ContainerName"] ?? "interacthub";
        // var blobClient = new BlobContainerClient(connectionString, containerName);
        // await blobClient.CreateIfNotExistsAsync(PublicAccessType.Blob);
        // var blob = blobClient.GetBlobClient($"{Guid.NewGuid()}-{fileName}");
        // await blob.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });
        // return blob.Uri.ToString();

        // Placeholder for local dev
        _logger.LogInformation("File upload called for {FileName}", fileName);
        return $"https://placeholder.blob.core.windows.net/files/{Guid.NewGuid()}-{fileName}";
    }

    public Task DeleteAsync(string fileUrl)
    {
        _logger.LogInformation("File delete called for {Url}", fileUrl);
        return Task.CompletedTask;
    }
}
