using InteractHub.API.Models;

namespace InteractHub.API.Repositories.Interfaces;

public interface INotificationRepository
{
    void                        Add(Notification notification);
    Task<List<Notification>>    GetByUserAsync(string userId);
    Task<Notification?>         FindByIdAsync(int id);
    Task                        MarkAllReadAsync(string userId);
    Task<int>                   SaveChangesAsync();
}
