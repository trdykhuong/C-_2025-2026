using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace InteractHub.API.Models;

// ─── USER ─────────────────────────────────────────────────────────────────────
public class AppUser : IdentityUser
{
    [MaxLength(100)] public string FullName  { get; set; } = string.Empty;
    [MaxLength(300)] public string? Bio      { get; set; }
    public string?   AvatarUrl  { get; set; }
    public string?   CoverUrl   { get; set; }
    public DateTime  CreatedAt  { get; set; } = DateTime.UtcNow;
    public bool      IsActive   { get; set; } = true;
}

// ─── POST ─────────────────────────────────────────────────────────────────────
public class Post
{
    public int      Id        { get; set; }
    [MaxLength(2000)] public string Content  { get; set; } = string.Empty;
    public string?  ImageUrl  { get; set; }
    [MaxLength(20)] public string Visibility { get; set; } = "public"; // public | friends | private
    public bool     IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt{ get; set; }

    // FK
    public string  UserId { get; set; } = string.Empty;
    public AppUser User   { get; set; } = null!;

    // Nav
    public List<Comment>     Comments    { get; set; } = new();
    public List<Like>        Likes       { get; set; } = new();
    public List<PostHashtag> PostHashtags{ get; set; } = new();
    public List<PostReport>  PostReports { get; set; } = new();
}

// ─── COMMENT ──────────────────────────────────────────────────────────────────
public class Comment
{
    public int      Id        { get; set; }
    [MaxLength(1000)] public string Content  { get; set; } = string.Empty;
    public bool     IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string  UserId { get; set; } = string.Empty;
    public AppUser User   { get; set; } = null!;
    public int     PostId { get; set; }
    public Post    Post   { get; set; } = null!;
}

// ─── LIKE ─────────────────────────────────────────────────────────────────────
public class Like
{
    public int      Id        { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string  UserId { get; set; } = string.Empty;
    public AppUser User   { get; set; } = null!;
    public int     PostId { get; set; }
    public Post    Post   { get; set; } = null!;
}

// ─── HASHTAG ──────────────────────────────────────────────────────────────────
public class Hashtag
{
    public int    Id   { get; set; }
    [MaxLength(100)] public string Name { get; set; } = string.Empty;
    public List<PostHashtag> PostHashtags { get; set; } = new();
}

public class PostHashtag
{
    public int     PostId    { get; set; }
    public Post    Post      { get; set; } = null!;
    public int     HashtagId { get; set; }
    public Hashtag Hashtag   { get; set; } = null!;
}

// ─── STORY ────────────────────────────────────────────────────────────────────
public class Story
{
    public int      Id        { get; set; }
    public string?  ImageUrl  { get; set; }
    [MaxLength(300)] public string? Caption { get; set; }
    [MaxLength(20)] public string Visibility { get; set; } = "public"; // public | friends | private
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

    public string  UserId { get; set; } = string.Empty;
    public AppUser User   { get; set; } = null!;
    public List<StoryView> StoryViews { get; set; } = new();
}

// ─── NOTIFICATION ─────────────────────────────────────────────────────────────
public class Notification
{
    public int      Id          { get; set; }
    [MaxLength(300)] public string Message { get; set; } = string.Empty;
    public string   Type        { get; set; } = "general"; // like | comment | friend_request | friend_accepted
    public bool     IsRead      { get; set; } = false;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
    public int?     RelatedPostId { get; set; }

    public string  UserId   { get; set; } = string.Empty; // người nhận
    public AppUser User     { get; set; } = null!;
    public string? ActorId  { get; set; }                 // người gây ra
}

// ─── FRIENDSHIP ───────────────────────────────────────────────────────────────
public class Friendship
{
    public int      Id         { get; set; }
    public string   Status     { get; set; } = "pending"; // pending | accepted | rejected
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string  SenderId   { get; set; } = string.Empty;
    public AppUser Sender     { get; set; } = null!;
    public string  ReceiverId { get; set; } = string.Empty;
    public AppUser Receiver   { get; set; } = null!;
}

// ─── STORY VIEW ───────────────────────────────────────────────────────────────
public class StoryView
{
    public int      Id       { get; set; }
    public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

    public int     StoryId { get; set; }
    public Story   Story   { get; set; } = null!;
    public string  UserId  { get; set; } = string.Empty;
    public AppUser User    { get; set; } = null!;
}

// ─── POST REPORT ──────────────────────────────────────────────────────────────
public class PostReport
{
    public int      Id        { get; set; }
    [MaxLength(500)] public string Reason { get; set; } = string.Empty;
    public string   Status    { get; set; } = "pending"; // pending | reviewed | dismissed
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string  UserId { get; set; } = string.Empty;
    public AppUser User   { get; set; } = null!;
    public int     PostId { get; set; }
    public Post    Post   { get; set; } = null!;
}
