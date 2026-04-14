using InteractHub.API.Data;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;
    public NotificationRepository(AppDbContext db) => _db = db;

    public void Add(Notification notification) => _db.Notifications.Add(notification);

    public async Task<List<Notification>> GetByUserAsync(string userId)
        => await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

    public async Task<Notification?> FindByIdAsync(int id)
        => await _db.Notifications.FindAsync(id);

    public async Task MarkAllReadAsync(string userId)
        => await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

    public async Task<int> SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
