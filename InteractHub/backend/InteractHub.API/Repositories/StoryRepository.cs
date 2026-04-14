using InteractHub.API.Data;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Repositories;

public class StoryRepository : IStoryRepository
{
    private readonly AppDbContext _db;
    public StoryRepository(AppDbContext db) => _db = db;

    public async Task<List<string>> GetFriendIdsAsync(string userId)
        => await _db.Friendships
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted")
            .Select(f => f.SenderId == userId ? f.ReceiverId : f.SenderId)
            .ToListAsync();

    public async Task<List<Story>> GetFeedAsync(List<string> authorIds, string userId)
        => await _db.Stories
            .Include(s => s.User)
            .Include(s => s.StoryViews)
            .Where(s => authorIds.Contains(s.UserId) && s.ExpiresAt > DateTime.UtcNow)
            .Where(s => s.Visibility == "public"
                     || s.Visibility == "friends"
                     || (s.Visibility == "private" && s.UserId == userId))
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public async Task<Story?> FindByIdAsync(int id)
        => await _db.Stories.FindAsync(id);

    public async Task<bool> HasViewedAsync(int storyId, string userId)
        => await _db.StoryViews.AnyAsync(v => v.StoryId == storyId && v.UserId == userId);

    public void Add(Story story)        => _db.Stories.Add(story);
    public void AddView(StoryView view) => _db.StoryViews.Add(view);
    public void Remove(Story story)     => _db.Stories.Remove(story);

    public async Task LoadUserAsync(Story story)
        => await _db.Entry(story).Reference(s => s.User).LoadAsync();

    public async Task<int> SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
