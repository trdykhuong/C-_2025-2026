using InteractHub.API.Data;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Repositories;

public class PostRepository : IPostRepository
{
    private readonly AppDbContext _db;
    public PostRepository(AppDbContext db) => _db = db;

    public async Task<List<string>> GetFriendIdsAsync(string userId)
        => await _db.Friendships
            .Where(f => (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted")
            .Select(f => f.SenderId == userId ? f.ReceiverId : f.SenderId)
            .ToListAsync();

    public async Task<(int Total, List<Post> Items)> GetFeedPagedAsync(
        List<string> authorIds, string currentUserId, int page, int pageSize)
    {
        var query = _db.Posts
            .Where(p => !p.IsDeleted && authorIds.Contains(p.UserId))
            .Where(p => p.Visibility == "public"
                     || p.Visibility == "friends"
                     || (p.Visibility == "private" && p.UserId == currentUserId))
            .OrderByDescending(p => p.CreatedAt);

        return await ExecutePagedAsync(query, page, pageSize);
    }

    public async Task<(int Total, List<Post> Items)> GetByUserPagedAsync(string userId, int page, int pageSize)
    {
        var query = _db.Posts
            .Where(p => !p.IsDeleted && p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt);

        return await ExecutePagedAsync(query, page, pageSize);
    }

    public async Task<(int Total, List<Post> Items)> SearchPagedAsync(string keyword, int page, int pageSize)
    {
        var query = _db.Posts
            .Where(p => !p.IsDeleted && p.Content.Contains(keyword))
            .OrderByDescending(p => p.CreatedAt);

        return await ExecutePagedAsync(query, page, pageSize);
    }

    public async Task<(int Total, List<Post> Items)> GetByHashtagPagedAsync(string normalizedTag, int page, int pageSize)
    {
        var query = _db.Posts
            .Where(p => !p.IsDeleted && p.PostHashtags.Any(ph => ph.Hashtag.Name == normalizedTag))
            .OrderByDescending(p => p.CreatedAt);

        return await ExecutePagedAsync(query, page, pageSize);
    }

    public async Task<Post?> FindByIdAsync(int id)
        => await _db.Posts.FindAsync(id);

    public async Task<Post?> FindActiveWithUserAsync(int id)
        => await _db.Posts.Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

    public async Task<Like?> FindLikeAsync(int postId, string userId)
        => await _db.Likes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

    public async Task<Hashtag?> FindHashtagByNameAsync(string name)
        => await _db.Hashtags.FirstOrDefaultAsync(h => h.Name == name);

    public void Add(Post post)         => _db.Posts.Add(post);
    public void AddLike(Like like)     => _db.Likes.Add(like);
    public void RemoveLike(Like like)  => _db.Likes.Remove(like);

    public async Task LoadUserAsync(Post post)
        => await _db.Entry(post).Reference(p => p.User).LoadAsync();

    public async Task<int> SaveChangesAsync()
        => await _db.SaveChangesAsync();

    // ── private helper ───────────────────────────────────────────────────────
    private static async Task<(int Total, List<Post> Items)> ExecutePagedAsync(
        IQueryable<Post> query, int page, int pageSize)
    {
        var total = await query.CountAsync();
        var items = await query
            .Include(p => p.User)
            .Include(p => p.Likes)
            .Include(p => p.Comments.Where(c => !c.IsDeleted))
            .Include(p => p.PostHashtags).ThenInclude(ph => ph.Hashtag)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (total, items);
    }
}
