using InteractHub.API.Models;

namespace InteractHub.API.Repositories.Interfaces;

public interface ICommentRepository
{
    Task<List<Comment>> GetByPostWithUserAsync(int postId);
    Task<Post?>         FindPostAsync(int postId);
    Task<Comment?>      FindActiveAsync(int id);
    Task<Comment?>      FindActiveWithUserAsync(int id);
    void                Add(Comment comment);
    Task                LoadUserAsync(Comment comment);
    Task<int>           SaveChangesAsync();
}
