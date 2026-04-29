using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.SignalR;

namespace InteractHub.API.Hubs;

public class SignalRPusher : IRealtimePusher
{
    private readonly IHubContext<NotificationHub> _hub;
    public SignalRPusher(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task PushNotificationAsync(string userId, NotificationResponseDTO notification)
        => _hub.Clients.Group(userId).SendAsync("ReceiveNotification", notification);
}
