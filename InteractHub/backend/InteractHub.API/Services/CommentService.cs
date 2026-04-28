using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;

namespace InteractHub.API.Services;

public class CommentService
{
    private readonly ICommentRepository  _repo;
    private readonly NotificationService _notifService;

    public CommentService(ICommentRepository repo, NotificationService notifService)
    {
        _repo         = repo;
        _notifService = notifService;
    }

    public async Task<List<CommentResponseDTO>> GetByPostAsync(int postId)
    {
        var comments = await _repo.GetByPostWithUserAsync(postId);
        return comments.Select(c => new CommentResponseDTO
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
        }).ToList();
    }

    public async Task<ApiResult<CommentResponseDTO>> CreateAsync(int postId, string userId, CreateCommentDTO dto)
    {
        var post = await _repo.FindPostAsync(postId);
        if (post == null) return ApiResult<CommentResponseDTO>.Fail("Bài đăng không tồn tại.");

        var comment = new Comment { PostId = postId, UserId = userId, Content = dto.Content };
        _repo.Add(comment);
        await _repo.SaveChangesAsync();
        await _repo.LoadUserAsync(comment);

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
        var comment = await _repo.FindActiveWithUserAsync(commentId);
        if (comment == null)          return ApiResult<CommentResponseDTO>.Fail("Không tìm thấy comment.");
        if (comment.UserId != userId) return ApiResult<CommentResponseDTO>.Fail("Không có quyền sửa.");

        comment.Content = dto.Content;
        await _repo.SaveChangesAsync();

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
        var comment = await _repo.FindActiveAsync(commentId);
        if (comment == null)                            return ApiResult<bool>.Fail("Không tìm thấy comment.");
        if (comment.UserId != userId && !isAdmin)       return ApiResult<bool>.Fail("Không có quyền xóa.");

        comment.IsDeleted = true;
        await _repo.SaveChangesAsync();
        return ApiResult<bool>.Ok(true, "Đã xóa comment.");
    }
}
