using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;

namespace InteractHub.API.Services;

public class NotificationService
{
    private readonly INotificationRepository _repo;
    private readonly IUserRepository         _userRepo;
    private readonly IRealtimePusher         _pusher;

    public NotificationService(
        INotificationRepository repo,
        IUserRepository         userRepo,
        IRealtimePusher         pusher)
    {
        _repo     = repo;
        _userRepo = userRepo;
        _pusher   = pusher;
    }

    // Tạo thông báo, tự động ghép tên actor vào message, rồi push real-time
    public async Task CreateAsync(
        string toUserId, string fromUserId, string type, string message, int? postId = null)
    {
        var actor     = await _userRepo.FindByIdAsync(fromUserId);
        var actorName = actor?.FullName ?? actor?.UserName ?? "Ai đó";
        var fullMsg   = $"{actorName} {message}";

        var notif = new Notification
        {
            UserId        = toUserId,
            ActorId       = fromUserId,
            Type          = type,
            Message       = fullMsg,
            RelatedPostId = postId,
        };
        _repo.Add(notif);
        await _repo.SaveChangesAsync();

        await _pusher.PushNotificationAsync(toUserId, ToDTO(notif, actor));
    }

    public async Task<List<NotificationResponseDTO>> GetAllAsync(string userId)
    {
        var list = await _repo.GetByUserAsync(userId);
        return list.Select(n => ToDTO(n, n.Actor)).ToList();
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

    // ── helper ───────────────────────────────────────────────────────────────
    private static NotificationResponseDTO ToDTO(Notification n, AppUser? actor) => new()
    {
        Id            = n.Id,
        Message       = n.Message,
        Type          = n.Type,
        IsRead        = n.IsRead,
        CreatedAt     = n.CreatedAt,
        RelatedPostId = n.RelatedPostId,
        ActorId       = n.ActorId,
        ActorName     = actor?.FullName,
        ActorAvatar   = actor?.AvatarUrl,
    };
}
