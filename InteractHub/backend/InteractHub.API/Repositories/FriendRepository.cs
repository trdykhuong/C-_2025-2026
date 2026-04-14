using InteractHub.API.Data;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Repositories;

public class FriendRepository : IFriendRepository
{
    private readonly AppDbContext _db;
    public FriendRepository(AppDbContext db) => _db = db;

    public async Task<bool> ExistsAsync(string senderId, string receiverId)
        => await _db.Friendships.AnyAsync(f =>
            (f.SenderId == senderId && f.ReceiverId == receiverId) ||
            (f.SenderId == receiverId && f.ReceiverId == senderId));

    public async Task<AppUser?> FindUserAsync(string userId)
        => await _db.Users.FindAsync(userId);

    public async Task<Friendship?> FindByIdWithUsersAsync(int id)
        => await _db.Friendships
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .FirstOrDefaultAsync(f => f.Id == id);

    public async Task<Friendship?> FindByIdAsync(int id)
        => await _db.Friendships.FindAsync(id);

    public async Task<List<Friendship>> GetAcceptedAsync(string userId)
        => await _db.Friendships
            .Include(f => f.Sender)
            .Include(f => f.Receiver)
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted")
            .ToListAsync();

    public async Task<List<Friendship>> GetPendingAsync(string userId)
        => await _db.Friendships
            .Include(f => f.Sender)
            .Where(f => f.ReceiverId == userId && f.Status == "pending")
            .ToListAsync();

    public async Task<Friendship?> FindByUsersAsync(string userId1, string userId2)
        => await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.SenderId == userId1 && f.ReceiverId == userId2) ||
            (f.SenderId == userId2 && f.ReceiverId == userId1));

    public void Add(Friendship friendship)    => _db.Friendships.Add(friendship);
    public void Remove(Friendship friendship) => _db.Friendships.Remove(friendship);

    public async Task<int> SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
