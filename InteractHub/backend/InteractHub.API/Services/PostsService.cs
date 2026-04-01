using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Services;

public class PostsService : IPostsService
{
    private readonly AppDbContext _db;
    private readonly INotificationsService _notifications;

    public PostsService(AppDbContext db, INotificationsService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task<PagedResultDto<PostResponseDto>> GetFeedAsync(string currentUserId, int page, int pageSize)
    {
        var friendIds = await _db.Friendships
            .Where(f => (f.SenderId == currentUserId || f.ReceiverId == currentUserId) && f.Status == "accepted")
            .Select(f => f.SenderId == currentUserId ? f.ReceiverId : f.SenderId)
            .ToListAsync();

        friendIds.Add(currentUserId);

        var query = _db.Posts
            .Where(p => !p.IsDeleted && friendIds.Contains(p.UserId))
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedAsync(query, currentUserId, page, pageSize);
    }

    public async Task<PagedResultDto<PostResponseDto>> GetUserPostsAsync(string userId, string currentUserId, int page, int pageSize)
    {
        var query = _db.Posts
            .Where(p => !p.IsDeleted && p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedAsync(query, currentUserId, page, pageSize);
    }

    public async Task<ApiResponse<PostResponseDto>> GetByIdAsync(int id, string currentUserId)
    {
        var post = await _db.Posts
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (post == null) return ApiResponse<PostResponseDto>.Fail("Post not found.");
        return ApiResponse<PostResponseDto>.Ok(MapPost(post, currentUserId));
    }

    public async Task<ApiResponse<PostResponseDto>> CreateAsync(string userId, CreatePostDto dto)
    {
        var post = new Post
        {
            Content = dto.Content,
            ImageUrl = dto.ImageUrl,
            UserId = userId
        };

        foreach (var tag in dto.Hashtags.Distinct())
        {
            var hashtag = await _db.Hashtags.FirstOrDefaultAsync(h => h.Name == tag.ToLower())
                          ?? new Hashtag { Name = tag.ToLower() };

            post.PostHashtags.Add(new PostHashtag { Hashtag = hashtag });
        }

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        await _db.Entry(post).Reference(p => p.User).LoadAsync();
        return ApiResponse<PostResponseDto>.Ok(MapPost(post, userId), "Post created.");
    }

    public async Task<ApiResponse<PostResponseDto>> UpdateAsync(int id, string userId, UpdatePostDto dto)
    {
        var post = await _db.Posts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (post == null) return ApiResponse<PostResponseDto>.Fail("Post not found.");
        if (post.UserId != userId) return ApiResponse<PostResponseDto>.Fail("Unauthorized.");

        post.Content = dto.Content;
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ApiResponse<PostResponseDto>.Ok(MapPost(post, userId));
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, string userId)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return ApiResponse<bool>.Fail("Post not found.");
        if (post.UserId != userId) return ApiResponse<bool>.Fail("Unauthorized.");

        post.IsDeleted = true;
        await _db.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true, "Post deleted.");
    }

    public async Task<ApiResponse<bool>> ToggleLikeAsync(int postId, string userId)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return ApiResponse<bool>.Fail("Post not found.");

        var like = await _db.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        if (like != null)
        {
            _db.Likes.Remove(like);
            await _db.SaveChangesAsync();
            return ApiResponse<bool>.Ok(false, "Unliked.");
        }

        _db.Likes.Add(new Like { PostId = postId, UserId = userId });
        await _db.SaveChangesAsync();

        if (post.UserId != userId)
            await _notifications.CreateNotificationAsync(post.UserId, userId, "like", "liked your post.", postId);

        return ApiResponse<bool>.Ok(true, "Liked.");
    }

    public async Task<PagedResultDto<PostResponseDto>> SearchAsync(string query, string currentUserId, int page, int pageSize)
    {
        var q = _db.Posts
            .Where(p => !p.IsDeleted && (p.Content.Contains(query) ||
                        p.PostHashtags.Any(ph => ph.Hashtag.Name.Contains(query.ToLower()))))
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedAsync(q, currentUserId, page, pageSize);
    }

    private async Task<PagedResultDto<PostResponseDto>> ToPagedAsync(IQueryable<Post> query, string currentUserId, int page, int pageSize)
    {
        var total = await query.CountAsync();
        var posts = await query
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<PostResponseDto>
        {
            Items = posts.Select(p => MapPost(p, currentUserId)).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static PostResponseDto MapPost(Post p, string currentUserId) => new()
    {
        Id = p.Id,
        Content = p.Content,
        ImageUrl = p.ImageUrl,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Author = new UserSummaryDto
        {
            Id = p.User.Id,
            UserName = p.User.UserName!,
            FullName = p.User.FullName,
            AvatarUrl = p.User.AvatarUrl
        },
        LikeCount = p.Likes.Count,
        CommentCount = p.Comments.Count(c => !c.IsDeleted),
        IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
        Hashtags = p.PostHashtags.Select(ph => ph.Hashtag.Name).ToList()
    };
}
