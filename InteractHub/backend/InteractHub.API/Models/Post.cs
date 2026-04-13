using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InteractHub.API.Models;

public class Post
{
    public int Id { get; set; }

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Like> Likes { get; set; } = new List<Like>();
    public ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
    public ICollection<PostReport> PostReports { get; set; } = new List<PostReport>();
}

public class Comment
{
    public int Id { get; set; }

    [Required, MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
}

public class Like
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
}

public class Hashtag
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public ICollection<PostHashtag> PostHashtags { get; set; } = new List<PostHashtag>();
}

public class PostHashtag
{
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int HashtagId { get; set; }
    public Hashtag Hashtag { get; set; } = null!;
}

public class Story
{
    public int Id { get; set; }
    public string? ImageUrl { get; set; }

    [MaxLength(300)]
    public string? Caption { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
}

public class Notification
{
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = "general"; // like, comment, friend_request, etc.
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? RelatedPostId { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public string? ActorId { get; set; } // who triggered
}

public class Friendship
{
    public int Id { get; set; }
    public string Status { get; set; } = "pending"; // pending, accepted, rejected

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;
    public AppUser Sender { get; set; } = null!;

    [Required]
    public string ReceiverId { get; set; } = string.Empty;
    public AppUser Receiver { get; set; } = null!;
}

public class PostReport
{
    public int Id { get; set; }

    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;

    public string Status { get; set; } = "pending"; // pending, reviewed, dismissed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
}
