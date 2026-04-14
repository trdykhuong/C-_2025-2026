using InteractHub.API.Models;
using Microsoft.AspNetCore.Identity;

namespace InteractHub.API.Repositories.Interfaces;

public interface IUserRepository
{
    Task<AppUser?>          FindByIdAsync(string userId);
    Task<int>               CountPostsAsync(string userId);
    Task<int>               CountFriendsAsync(string userId);
    Task<Friendship?>       FindFriendshipAsync(string userId1, string userId2);
    Task<List<AppUser>>     SearchAsync(string keyword);
    Task<IdentityResult>    UpdateAsync(AppUser user);
}
