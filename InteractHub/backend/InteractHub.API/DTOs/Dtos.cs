using System.ComponentModel.DataAnnotations;

namespace InteractHub.API.DTOs;

// ════════════════════════════════════════════════════════════
// SHARED - dùng ở nhiều nơi
// ════════════════════════════════════════════════════════════
public class ApiResult<T>
{
    public bool   Success { get; set; }
    public T?     Data    { get; set; }
    public string Message { get; set; } = string.Empty;

    public static ApiResult<T> Ok(T data, string msg = "")
        => new() { Success = true, Data = data, Message = msg };

    public static ApiResult<T> Fail(string msg)
        => new() { Success = false, Message = msg };
}

public class PagedResult<T>
{
    public List<T> Items      { get; set; } = new();
    public int     TotalCount { get; set; }
    public int     Page       { get; set; }
    public int     PageSize   { get; set; }
    public bool    HasNext    => Page * PageSize < TotalCount;
}

public class UserSummaryDTO
{
    public string  Id        { get; set; } = string.Empty;
    public string  UserName  { get; set; } = string.Empty;
    public string  FullName  { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

// ════════════════════════════════════════════════════════════
// AUTH DTO
// ════════════════════════════════════════════════════════════
public class RegisterDTO
{
    [Required, MaxLength(100)]  public string FullName { get; set; } = string.Empty;
    [Required, MaxLength(50)]   public string UserName { get; set; } = string.Empty;
    [Required, EmailAddress]    public string Email    { get; set; } = string.Empty;
    [Required, MinLength(8)]    public string Password { get; set; } = string.Empty;
}

public class LoginDTO
{
    [Required, EmailAddress] public string Email    { get; set; } = string.Empty;
    [Required]               public string Password { get; set; } = string.Empty;
}

public class AuthResponseDTO
{
    public string        Token    { get; set; } = string.Empty;
    public string        UserId   { get; set; } = string.Empty;
    public string        UserName { get; set; } = string.Empty;
    public string        FullName { get; set; } = string.Empty;
    public string?       AvatarUrl{ get; set; }
    public IList<string> Roles    { get; set; } = new List<string>();
    public DateTime      ExpiredAt{ get; set; }
}

// ════════════════════════════════════════════════════════════
// USER DTO
// ════════════════════════════════════════════════════════════
public class UserProfileDTO
{
    public string  Id               { get; set; } = string.Empty;
    public string  UserName         { get; set; } = string.Empty;
    public string  FullName         { get; set; } = string.Empty;
    public string? Bio              { get; set; }
    public string? AvatarUrl        { get; set; }
    public string? CoverUrl         { get; set; }
    public DateTime CreatedAt       { get; set; }
    public int     PostCount        { get; set; }
    public int     FriendCount      { get; set; }
    public string  FriendshipStatus { get; set; } = "none"; // none | pending_sent | pending_received | accepted
    public int?    FriendshipId     { get; set; }
}

public class UpdateProfileDTO
{
    [MaxLength(100)] public string? FullName { get; set; }
    [MaxLength(300)] public string? Bio      { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverUrl  { get; set; }
}

// ════════════════════════════════════════════════════════════
// POST DTO
// ════════════════════════════════════════════════════════════
public class CreatePostDTO
{
    [Required, MinLength(1), MaxLength(2000)]
    public string       Content    { get; set; } = string.Empty;
    public string?      ImageUrl   { get; set; }
    [MaxLength(20)] public string Visibility { get; set; } = "public";
    public List<string> Hashtags   { get; set; } = new();
}

public class UpdatePostDTO
{
    [Required, MinLength(1), MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
}

public class PostResponseDTO
{
    public int           Id                    { get; set; }
    public string        Content               { get; set; } = string.Empty;
    public string?       ImageUrl              { get; set; }
    public string        Visibility            { get; set; } = "public";
    public DateTime      CreatedAt             { get; set; }
    public DateTime?     UpdatedAt             { get; set; }
    public UserSummaryDTO Author               { get; set; } = null!;
    public int           LikeCount             { get; set; }
    public int           CommentCount          { get; set; }
    public bool          IsLikedByCurrentUser  { get; set; }
    public List<string>  Hashtags              { get; set; } = new();
}

// ════════════════════════════════════════════════════════════
// COMMENT DTO
// ════════════════════════════════════════════════════════════
public class CreateCommentDTO
{
    [Required, MinLength(1), MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
}

public class UpdateCommentDTO
{
    [Required, MinLength(1), MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
}

public class CommentResponseDTO
{
    public int            Id        { get; set; }
    public string         Content   { get; set; } = string.Empty;
    public DateTime       CreatedAt { get; set; }
    public UserSummaryDTO Author    { get; set; } = null!;
}

// ════════════════════════════════════════════════════════════
// FRIEND DTO
// ════════════════════════════════════════════════════════════
public class SendFriendRequestDTO
{
    [Required] public string ReceiverId { get; set; } = string.Empty;
}

public class FriendshipResponseDTO
{
    public int            Id        { get; set; }
    public string         Status    { get; set; } = string.Empty;
    public DateTime       CreatedAt { get; set; }
    public UserSummaryDTO OtherUser { get; set; } = null!;
}

// ════════════════════════════════════════════════════════════
// STORY DTO
// ════════════════════════════════════════════════════════════
public class CreateStoryDTO
{
    public string?                  ImageUrl   { get; set; }
    [MaxLength(300)] public string? Caption    { get; set; }
    [MaxLength(20)] public string   Visibility { get; set; } = "public";
}

public class StoryResponseDTO
{
    public int            Id         { get; set; }
    public string?        ImageUrl   { get; set; }
    public string?        Caption    { get; set; }
    public string         Visibility { get; set; } = "public";
    public DateTime       CreatedAt  { get; set; }
    public DateTime       ExpiresAt  { get; set; }
    public int            ViewCount  { get; set; }
    public UserSummaryDTO Author     { get; set; } = null!;
}

// ════════════════════════════════════════════════════════════
// NOTIFICATION DTO
// ════════════════════════════════════════════════════════════
public class NotificationResponseDTO
{
    public int       Id            { get; set; }
    public string    Message       { get; set; } = string.Empty;
    public string    Type          { get; set; } = string.Empty;
    public bool      IsRead        { get; set; }
    public DateTime  CreatedAt     { get; set; }
    public int?      RelatedPostId { get; set; }
}

// ════════════════════════════════════════════════════════════
// REPORT DTO
// ════════════════════════════════════════════════════════════
public class CreateReportDTO
{
    [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
}
