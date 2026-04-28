using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;

namespace InteractHub.API.Services;

public class NotificationService
{
    private readonly INotificationRepository _repo;
    public NotificationService(INotificationRepository repo) => _repo = repo;

    public async Task CreateAsync(string toUserId, string fromUserId, string type, string message, int? postId = null)
    {
        _repo.Add(new Notification
        {
            UserId        = toUserId,
            ActorId       = fromUserId,
            Type          = type,
            Message       = message,
            RelatedPostId = postId,
        });
        await _repo.SaveChangesAsync();
    }

    public async Task<List<NotificationResponseDTO>> GetAllAsync(string userId)
    {
        var list = await _repo.GetByUserAsync(userId);
        return list.Select(n => new NotificationResponseDTO
        {
            Id            = n.Id,
            Message       = n.Message,
            Type          = n.Type,
            IsRead        = n.IsRead,
            CreatedAt     = n.CreatedAt,
            RelatedPostId = n.RelatedPostId,
        }).ToList();
    }

    public async Task<ApiResult<bool>> MarkReadAsync(int id, string userId)
    {
        var n = await _repo.FindByIdAsync(id);
        if (n == null || n.UserId != userId) return ApiResult<bool>.Fail("Không tìm thấy.");
        n.IsRead = true;
        await _repo.SaveChangesAsync();
        return ApiResult<bool>.Ok(true);
    }

    public async Task MarkAllReadAsync(string userId)
        => await _repo.MarkAllReadAsync(userId);
}
