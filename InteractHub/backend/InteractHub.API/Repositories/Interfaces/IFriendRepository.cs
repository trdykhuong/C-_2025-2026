using InteractHub.API.Models;

namespace InteractHub.API.Repositories.Interfaces;

public interface IFriendRepository
{
    Task<bool>         ExistsAsync(string senderId, string receiverId);
    Task<AppUser?>     FindUserAsync(string userId);
    Task<Friendship?>  FindByIdWithUsersAsync(int id);
    Task<Friendship?>  FindByIdAsync(int id);
    Task<List<Friendship>> GetAcceptedAsync(string userId);
    Task<List<Friendship>> GetPendingAsync(string userId);
    Task<Friendship?>  FindByUsersAsync(string userId1, string userId2);
    void               Add(Friendship friendship);
    void               Remove(Friendship friendship);
    Task<int>          SaveChangesAsync();
}
