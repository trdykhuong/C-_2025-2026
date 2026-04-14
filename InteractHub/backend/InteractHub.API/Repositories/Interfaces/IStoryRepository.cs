using InteractHub.API.Models;

namespace InteractHub.API.Repositories.Interfaces;

public interface IStoryRepository
{
    Task<List<string>> GetFriendIdsAsync(string userId);
    Task<List<Story>>  GetFeedAsync(List<string> authorIds, string userId);
    Task<Story?>       FindByIdAsync(int id);
    Task<bool>         HasViewedAsync(int storyId, string userId);
    void               Add(Story story);
    void               AddView(StoryView view);
    void               Remove(Story story);
    Task               LoadUserAsync(Story story);
    Task<int>          SaveChangesAsync();
}
