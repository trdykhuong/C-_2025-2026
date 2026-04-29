using InteractHub.API.DTOs;

namespace InteractHub.API.Services;

public interface IRealtimePusher
{
    Task PushNotificationAsync(string userId, NotificationResponseDTO notification);
}
