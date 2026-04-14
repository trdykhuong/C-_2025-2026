using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace InteractHub.API.Models;

public class AppUser : IdentityUser
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Bio { get; set; }

    public string? AvatarUrl { get; set; }
    public string? CoverUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<Story> Stories { get; set; } = new List<Story>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
    public ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
    public ICollection<PostReport> PostReports { get; set; } = new List<PostReport>();
}
