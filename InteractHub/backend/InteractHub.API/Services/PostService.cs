using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;

namespace InteractHub.API.Services;

public class PostService
{
    private readonly IPostRepository      _repo;
    private readonly NotificationService  _notifService;

    public PostService(IPostRepository repo, NotificationService notifService)
    {
        _repo         = repo;
        _notifService = notifService;
    }

    // ── Feed (bài đăng của bạn bè + bản thân; admin thấy tất cả) ────────────
    public async Task<PagedResult<PostResponseDTO>> GetFeedAsync(string userId, int page, int pageSize, bool isAdmin = false)
    {
        if (isAdmin)
        {
            var (allTotal, allPosts) = await _repo.GetAllPostsPagedAsync(page, pageSize);
            return BuildPaged(allPosts, userId, allTotal, page, pageSize);
        }

        var friendIds = await _repo.GetFriendIdsAsync(userId);
        friendIds.Add(userId);

        var (total, posts) = await _repo.GetFeedPagedAsync(friendIds, userId, page, pageSize);
        return BuildPaged(posts, userId, total, page, pageSize);
    }

    // ── Bài đăng của 1 user cụ thể ──────────────────────────────────────────
    public async Task<PagedResult<PostResponseDTO>> GetByUserAsync(string userId, string currentUserId, int page, int pageSize)
    {
        var (total, posts) = await _repo.GetByUserPagedAsync(userId, page, pageSize);
        return BuildPaged(posts, currentUserId, total, page, pageSize);
    }

    // ── Tìm kiếm bài đăng ───────────────────────────────────────────────────
    public async Task<PagedResult<PostResponseDTO>> SearchAsync(string keyword, string currentUserId, int page, int pageSize)
    {
        var (total, posts) = await _repo.SearchPagedAsync(keyword, page, pageSize);
        return BuildPaged(posts, currentUserId, total, page, pageSize);
    }

    // ── Bài đăng theo hashtag ────────────────────────────────────────────────
    public async Task<PagedResult<PostResponseDTO>> GetByHashtagAsync(string tag, string currentUserId, int page, int pageSize)
    {
        var normalizedTag = tag.ToLower().Trim().TrimStart('#');
        var (total, posts) = await _repo.GetByHashtagPagedAsync(normalizedTag, page, pageSize);
        return BuildPaged(posts, currentUserId, total, page, pageSize);
    }

    // ── Tạo bài đăng ────────────────────────────────────────────────────────
    public async Task<ApiResult<PostResponseDTO>> CreateAsync(string userId, CreatePostDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Content) && dto.SharedPostId == null && string.IsNullOrWhiteSpace(dto.ImageUrl))
            return ApiResult<PostResponseDTO>.Fail("Nội dung bài viết không được để trống.");

        var post = new Post
        {
            Content      = dto.Content,
            ImageUrl     = dto.ImageUrl,
            Visibility   = dto.Visibility,
            UserId       = userId,
            SharedPostId = dto.SharedPostId,
        };

        foreach (var tagName in dto.Hashtags.Distinct())
        {
            var name    = tagName.ToLower().Trim().TrimStart('#');
            var hashtag = await _repo.FindHashtagByNameAsync(name) ?? new Hashtag { Name = name };
            post.PostHashtags.Add(new PostHashtag { Hashtag = hashtag });
        }

        _repo.Add(post);
        await _repo.SaveChangesAsync();
        await _repo.LoadUserAsync(post);

        if (dto.SharedPostId.HasValue)
            post.SharedPost = await _repo.FindActiveWithUserAsync(dto.SharedPostId.Value);

        return ApiResult<PostResponseDTO>.Ok(MapToDTO(post, userId), "Đăng bài thành công!");
    }

    // ── Cập nhật bài đăng ───────────────────────────────────────────────────
    public async Task<ApiResult<PostResponseDTO>> UpdateAsync(int postId, string userId, UpdatePostDTO dto)
    {
        var post = await _repo.FindActiveWithUserAsync(postId);
        if (post == null)           return ApiResult<PostResponseDTO>.Fail("Không tìm thấy bài đăng.");
        if (post.UserId != userId)  return ApiResult<PostResponseDTO>.Fail("Bạn không có quyền sửa bài này.");

        post.Content   = dto.Content;
        post.UpdatedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync();

        return ApiResult<PostResponseDTO>.Ok(MapToDTO(post, userId));
    }

    // ── Xóa bài đăng (soft delete) ──────────────────────────────────────────
    public async Task<ApiResult<bool>> DeleteAsync(int postId, string userId, bool isAdmin = false)
    {
        var post = await _repo.FindByIdAsync(postId);
        if (post == null) return ApiResult<bool>.Fail("Không tìm thấy bài đăng.");

        if (post.UserId != userId && !isAdmin)
            return ApiResult<bool>.Fail("Bạn không có quyền xóa bài này.");

        post.IsDeleted = true;
        await _repo.SaveChangesAsync();
        return ApiResult<bool>.Ok(true, "Đã xóa bài đăng.");
    }

    // ── Like / Unlike bài đăng ──────────────────────────────────────────────
    public async Task<ApiResult<bool>> ToggleLikeAsync(int postId, string userId)
    {
        var post = await _repo.FindByIdAsync(postId);
        if (post == null) return ApiResult<bool>.Fail("Không tìm thấy bài đăng.");

        var existing = await _repo.FindLikeAsync(postId, userId);
        if (existing != null)
        {
            _repo.RemoveLike(existing);
            await _repo.SaveChangesAsync();
            return ApiResult<bool>.Ok(false, "Đã bỏ like.");
        }

        _repo.AddLike(new Like { PostId = postId, UserId = userId });
        await _repo.SaveChangesAsync();

        if (post.UserId != userId)
            await _notifService.CreateAsync(post.UserId, userId, "like", "đã thích bài viết của bạn.", postId);

        return ApiResult<bool>.Ok(true, "Đã like.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static PagedResult<PostResponseDTO> BuildPaged(
        List<Post> posts, string currentUserId, int total, int page, int pageSize)
        => new()
        {
            Items      = posts.Select(p => MapToDTO(p, currentUserId)).ToList(),
            TotalCount = total,
            Page       = page,
            PageSize   = pageSize,
        };

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
        SharedPostId         = p.SharedPostId,
        SharedPost           = p.SharedPost == null ? null : new SharedPostSummaryDTO
        {
            Id        = p.SharedPost.Id,
            Content   = p.SharedPost.Content,
            ImageUrl  = p.SharedPost.ImageUrl,
            CreatedAt = p.SharedPost.CreatedAt,
            Author    = new UserSummaryDTO { Id = p.SharedPost.User.Id, UserName = p.SharedPost.User.UserName!, FullName = p.SharedPost.User.FullName, AvatarUrl = p.SharedPost.User.AvatarUrl },
        },
    };
}
