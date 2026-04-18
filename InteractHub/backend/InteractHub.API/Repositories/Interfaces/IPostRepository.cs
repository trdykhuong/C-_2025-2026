using InteractHub.API.Models;

namespace InteractHub.API.Repositories.Interfaces;

public interface IPostRepository
{
    Task<List<string>>              GetFriendIdsAsync(string userId);
    Task<(int Total, List<Post> Items)> GetFeedPagedAsync(List<string> authorIds, string currentUserId, int page, int pageSize);
    Task<(int Total, List<Post> Items)> GetAllPostsPagedAsync(int page, int pageSize);
    Task<(int Total, List<Post> Items)> GetByUserPagedAsync(string userId, int page, int pageSize);
    Task<(int Total, List<Post> Items)> SearchPagedAsync(string keyword, int page, int pageSize);
    Task<(int Total, List<Post> Items)> GetByHashtagPagedAsync(string normalizedTag, int page, int pageSize);
    Task<Post?>                     FindByIdAsync(int id);
    Task<Post?>                     FindActiveWithUserAsync(int id);
    Task<Like?>                     FindLikeAsync(int postId, string userId);
    Task<Hashtag?>                  FindHashtagByNameAsync(string name);
    void                            Add(Post post);
    void                            AddLike(Like like);
    void                            RemoveLike(Like like);
    Task                            LoadUserAsync(Post post);
    Task<int>                       SaveChangesAsync();
}
