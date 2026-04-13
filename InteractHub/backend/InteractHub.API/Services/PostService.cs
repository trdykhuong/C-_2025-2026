using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Services;

public class PostService
{
    private readonly AppDbContext       _db;
    private readonly NotificationService _notifService;

    public PostService(AppDbContext db, NotificationService notifService)
    {
        _db           = db;
        _notifService = notifService;
    }

    // ── Lấy feed (bài đăng của bạn bè + bản thân) ───────────────────────────
    public async Task<PagedResult<PostResponseDTO>> GetFeedAsync(string currentUserId, int page, int pageSize)
    {
        // Lấy danh sách friend id
        var friendIds = await _db.Friendships
            .Where(f => (f.SenderId == currentUserId || f.ReceiverId == currentUserId)
                        && f.Status == "accepted")
            .Select(f => f.SenderId == currentUserId ? f.ReceiverId : f.SenderId)
            .ToListAsync();

        friendIds.Add(currentUserId);

        var query = _db.Posts
            .Where(p => !p.IsDeleted && friendIds.Contains(p.UserId))
            .Where(p => p.Visibility == "public"
                     || p.Visibility == "friends"
                     || (p.Visibility == "private" && p.UserId == currentUserId))
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedResultAsync(query, currentUserId, page, pageSize);
    }

    // ── Lấy bài đăng của 1 user cụ thể ─────────────────────────────────────
    public async Task<PagedResult<PostResponseDTO>> GetByUserAsync(string userId, string currentUserId, int page, int pageSize)
    {
        var query = _db.Posts
            .Where(p => !p.IsDeleted && p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedResultAsync(query, currentUserId, page, pageSize);
    }

    // ── Tìm kiếm bài đăng ───────────────────────────────────────────────────
    public async Task<PagedResult<PostResponseDTO>> SearchAsync(string keyword, string currentUserId, int page, int pageSize)
    {
        var query = _db.Posts
            .Where(p => !p.IsDeleted && p.Content.Contains(keyword))
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedResultAsync(query, currentUserId, page, pageSize);
    }

    // ── Lấy bài đăng theo hashtag ────────────────────────────────────────────
    public async Task<PagedResult<PostResponseDTO>> GetByHashtagAsync(string tag, string currentUserId, int page, int pageSize)
    {
        var normalizedTag = tag.ToLower().Trim().TrimStart('#');
        var query = _db.Posts
            .Where(p => !p.IsDeleted && p.PostHashtags.Any(ph => ph.Hashtag.Name == normalizedTag))
            .OrderByDescending(p => p.CreatedAt);

        return await ToPagedResultAsync(query, currentUserId, page, pageSize);
    }

    // ── Tạo bài đăng mới ────────────────────────────────────────────────────
    public async Task<ApiResult<PostResponseDTO>> CreateAsync(string userId, CreatePostDTO dto)
    {
        var post = new Post
        {
            Content    = dto.Content,
            ImageUrl   = dto.ImageUrl,
            Visibility = dto.Visibility,
            UserId     = userId,
        };

        // Xử lý hashtag
        foreach (var tagName in dto.Hashtags.Distinct())
        {
            var name    = tagName.ToLower().Trim().TrimStart('#');
            var hashtag = await _db.Hashtags.FirstOrDefaultAsync(h => h.Name == name)
                          ?? new Hashtag { Name = name };

            post.PostHashtags.Add(new PostHashtag { Hashtag = hashtag });
        }

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        // Load navigation để map DTO
        await _db.Entry(post).Reference(p => p.User).LoadAsync();

        return ApiResult<PostResponseDTO>.Ok(MapToDTO(post, userId), "Đăng bài thành công!");
    }

    // ── Cập nhật bài đăng ───────────────────────────────────────────────────
    public async Task<ApiResult<PostResponseDTO>> UpdateAsync(int postId, string userId, UpdatePostDTO dto)
    {
        var post = await _db.Posts.Include(p => p.User)
                                  .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted);

        if (post == null)  return ApiResult<PostResponseDTO>.Fail("Không tìm thấy bài đăng.");
        if (post.UserId != userId) return ApiResult<PostResponseDTO>.Fail("Bạn không có quyền sửa bài này.");

        post.Content   = dto.Content;
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ApiResult<PostResponseDTO>.Ok(MapToDTO(post, userId));
    }

    // ── Xóa bài đăng (soft delete) ──────────────────────────────────────────
    public async Task<ApiResult<bool>> DeleteAsync(int postId, string userId, bool isAdmin = false)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return ApiResult<bool>.Fail("Không tìm thấy bài đăng.");

        if (post.UserId != userId && !isAdmin)
            return ApiResult<bool>.Fail("Bạn không có quyền xóa bài này.");

        post.IsDeleted = true;
        await _db.SaveChangesAsync();
        return ApiResult<bool>.Ok(true, "Đã xóa bài đăng.");
    }

    // ── Like / Unlike bài đăng ──────────────────────────────────────────────
    public async Task<ApiResult<bool>> ToggleLikeAsync(int postId, string userId)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return ApiResult<bool>.Fail("Không tìm thấy bài đăng.");

        var existing = await _db.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
        if (existing != null)
        {
            _db.Likes.Remove(existing);
            await _db.SaveChangesAsync();
            return ApiResult<bool>.Ok(false, "Đã bỏ like.");
        }

        _db.Likes.Add(new Like { PostId = postId, UserId = userId });
        await _db.SaveChangesAsync();

        // Gửi notification nếu không phải tự like bài của mình
        if (post.UserId != userId)
            await _notifService.CreateAsync(post.UserId, userId, "like", "đã thích bài viết của bạn.", postId);

        return ApiResult<bool>.Ok(true, "Đã like.");
    }

    // ── Helper: chuyển query → PagedResult ──────────────────────────────────
    private async Task<PagedResult<PostResponseDTO>> ToPagedResultAsync(
        IQueryable<Post> query, string currentUserId, int page, int pageSize)
    {
        var total = await query.CountAsync();

        var posts = await query
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeleted))
            .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<PostResponseDTO>
        {
            Items      = posts.Select(p => MapToDTO(p, currentUserId)).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };
    }

    // ── Helper: map Post entity → PostResponseDTO ────────────────────────────
    private static PostResponseDTO MapToDTO(Post p, string currentUserId) => new()
    {
        Id                   = p.Id,
        Content              = p.Content,
        ImageUrl             = p.ImageUrl,
        Visibility           = p.Visibility,
        CreatedAt            = p.CreatedAt,
        UpdatedAt            = p.UpdatedAt,
        Author               = new UserSummaryDTO { Id = p.User.Id, UserName = p.User.UserName!, FullName = p.User.FullName, AvatarUrl = p.User.AvatarUrl },
        LikeCount            = p.Likes.Count,
        CommentCount         = p.Comments.Count(c => !c.IsDeleted),
        IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == currentUserId),
        Hashtags             = p.PostHashtags.Select(ph => ph.Hashtag.Name).ToList(),
    };
}
