using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace InteractHub.API.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    // Mỗi user kết nối sẽ tự join vào group theo userId của họ.
    // NotificationService sẽ dùng group này để push thông báo đúng người.
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }
}
