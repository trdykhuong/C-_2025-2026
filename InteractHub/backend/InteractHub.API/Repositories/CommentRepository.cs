using InteractHub.API.Data;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Repositories;

public class CommentRepository : ICommentRepository
{
    private readonly AppDbContext _db;
    public CommentRepository(AppDbContext db) => _db = db;

    public async Task<List<Comment>> GetByPostWithUserAsync(int postId)
        => await _db.Comments
            .Include(c => c.User)
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

    public async Task<Post?> FindPostAsync(int postId)
        => await _db.Posts.FindAsync(postId);

    public async Task<Comment?> FindActiveAsync(int id)
        => await _db.Comments.FindAsync(id) is { IsDeleted: false } c ? c : null;

    public async Task<Comment?> FindActiveWithUserAsync(int id)
        => await _db.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

    public void Add(Comment comment) => _db.Comments.Add(comment);

    public async Task LoadUserAsync(Comment comment)
        => await _db.Entry(comment).Reference(c => c.User).LoadAsync();

    public async Task<int> SaveChangesAsync()
        => await _db.SaveChangesAsync();
}
