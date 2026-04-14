using InteractHub.API.Data;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext          _db;
    private readonly UserManager<AppUser>  _userManager;

    public UserRepository(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    public async Task<AppUser?> FindByIdAsync(string userId)
        => await _userManager.FindByIdAsync(userId);

    public async Task<int> CountPostsAsync(string userId)
        => await _db.Posts.CountAsync(p => p.UserId == userId && !p.IsDeleted);

    public async Task<int> CountFriendsAsync(string userId)
        => await _db.Friendships.CountAsync(f =>
            (f.SenderId == userId || f.ReceiverId == userId) && f.Status == "accepted");

    public async Task<Friendship?> FindFriendshipAsync(string userId1, string userId2)
        => await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.SenderId == userId1 && f.ReceiverId == userId2) ||
            (f.SenderId == userId2 && f.ReceiverId == userId1));

    public async Task<List<AppUser>> SearchAsync(string keyword)
        => await _db.Users
            .Where(u => u.UserName!.Contains(keyword) || u.FullName.Contains(keyword))
            .Take(20)
            .ToListAsync();

    public async Task<IdentityResult> UpdateAsync(AppUser user)
        => await _userManager.UpdateAsync(user);
}
