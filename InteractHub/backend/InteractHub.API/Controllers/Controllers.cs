using InteractHub.API.DTOs;
using InteractHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InteractHub.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _auth.RegisterAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _auth.LoginAsync(dto);
        return result.Success ? Ok(result) : Unauthorized(result);
    }


    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var userName = User.FindFirstValue(ClaimTypes.Name)!;
        var fullName = User.FindFirstValue("fullName")!;
        return Ok(ApiResponse<object>.Ok(new { userId, userName, fullName }));
    }
}

// ─── POSTS CONTROLLER ────────────────────────────────────────────────────────
[ApiController]
[Route("api/posts")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IPostsService _posts;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public PostsController(IPostsService posts) => _posts = posts;

    /// <summary>Get paginated news feed for current user</summary>
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _posts.GetFeedAsync(UserId, page, pageSize));

    /// <summary>Search posts by content or hashtag</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _posts.SearchAsync(q, UserId, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _posts.GetByIdAsync(id, UserId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPosts(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _posts.GetUserPostsAsync(userId, UserId, page, pageSize));

    /// <summary>Create a new post</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
    {
        var result = await _posts.CreateAsync(UserId, dto);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update own post</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostDto dto)
    {
        var result = await _posts.UpdateAsync(id, UserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete own post</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _posts.DeleteAsync(id, UserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Toggle like on a post</summary>
    [HttpPost("{id:int}/like")]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var result = await _posts.ToggleLikeAsync(id, UserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// ─── COMMENTS CONTROLLER ─────────────────────────────────────────────────────
[ApiController]
[Route("api/posts/{postId:int}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentsService _comments;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public CommentsController(ICommentsService comments) => _comments = comments;

    /// <summary>Get all comments for a post</summary>
    [HttpGet]
    public async Task<IActionResult> GetByPost(int postId)
        => Ok(ApiResponse<List<CommentResponseDto>>.Ok(await _comments.GetByPostAsync(postId)));

    /// <summary>Add a comment to a post</summary>
    [HttpPost]
    public async Task<IActionResult> Create(int postId, [FromBody] CreateCommentDto dto)
    {
        var result = await _comments.CreateAsync(postId, UserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete own comment</summary>
    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int postId, int commentId)
    {
        var result = await _comments.DeleteAsync(commentId, UserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// ─── FRIENDS CONTROLLER ──────────────────────────────────────────────────────
[ApiController]
[Route("api/friends")]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly IFriendsService _friends;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public FriendsController(IFriendsService friends) => _friends = friends;

    /// <summary>Get accepted friends list</summary>
    [HttpGet]
    public async Task<IActionResult> GetFriends()
        => Ok(ApiResponse<List<FriendshipResponseDto>>.Ok(await _friends.GetFriendsAsync(UserId)));

    /// <summary>Get pending incoming friend requests</summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetPendingRequests()
        => Ok(ApiResponse<List<FriendshipResponseDto>>.Ok(await _friends.GetPendingRequestsAsync(UserId)));

    /// <summary>Send a friend request</summary>
    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] FriendRequestDto dto)
    {
        var result = await _friends.SendRequestAsync(UserId, dto.ReceiverId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Accept or reject a friend request</summary>
    [HttpPut("request/{id:int}")]
    public async Task<IActionResult> RespondToRequest(int id, [FromQuery] bool accept)
    {
        var result = await _friends.RespondToRequestAsync(id, UserId, accept);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Remove a friend</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> RemoveFriend(int id)
    {
        var result = await _friends.RemoveFriendAsync(id, UserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// ─── STORIES CONTROLLER ──────────────────────────────────────────────────────
[ApiController]
[Route("api/stories")]
[Authorize]
public class StoriesController : ControllerBase
{
    private readonly IStoriesService _stories;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public StoriesController(IStoriesService stories) => _stories = stories;

    /// <summary>Get stories from friends (active, not expired)</summary>
    [HttpGet]
    public async Task<IActionResult> GetFeed()
        => Ok(ApiResponse<List<StoryResponseDto>>.Ok(await _stories.GetFeedAsync(UserId)));

    /// <summary>Create a new story</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoryDto dto)
    {
        var result = await _stories.CreateAsync(UserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete own story</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _stories.DeleteAsync(id, UserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// ─── NOTIFICATIONS CONTROLLER ────────────────────────────────────────────────
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationsService _notifications;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public NotificationsController(INotificationsService notifications) => _notifications = notifications;

    /// <summary>Get all notifications for current user</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(ApiResponse<List<NotificationResponseDto>>.Ok(await _notifications.GetUserNotificationsAsync(UserId)));

    /// <summary>Mark a notification as read</summary>
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var result = await _notifications.MarkAsReadAsync(id, UserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Mark all notifications as read</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notifications.MarkAllAsReadAsync(UserId);
        return Ok(ApiResponse<bool>.Ok(true));
    }
}

// ─── USERS CONTROLLER ────────────────────────────────────────────────────────
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly Microsoft.AspNetCore.Identity.UserManager<Models.AppUser> _userManager;
    private readonly AppDbContext _db;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public UsersController(
        Microsoft.AspNetCore.Identity.UserManager<Models.AppUser> userManager,
        AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    /// <summary>Get a user's public profile</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound(ApiResponse<UserProfileDto>.Fail("User not found."));

        var postCount = await _db.Posts.CountAsync(p => p.UserId == id && !p.IsDeleted);
        var friendCount = await _db.Friendships.CountAsync(f =>
            (f.SenderId == id || f.ReceiverId == id) && f.Status == "accepted");

        var friendship = await _db.Friendships.FirstOrDefaultAsync(f =>
            (f.SenderId == UserId && f.ReceiverId == id) ||
            (f.SenderId == id && f.ReceiverId == UserId));

        return Ok(ApiResponse<UserProfileDto>.Ok(new UserProfileDto
        {
            Id = user.Id,
            UserName = user.UserName!,
            FullName = user.FullName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            CoverUrl = user.CoverUrl,
            CreatedAt = user.CreatedAt,
            PostCount = postCount,
            FriendCount = friendCount,
            FriendshipStatus = friendship?.Status ?? "none"
        }));
    }

    /// <summary>Update own profile</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null) return NotFound();

        if (dto.FullName != null) user.FullName = dto.FullName;
        if (dto.Bio != null) user.Bio = dto.Bio;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded ? Ok(ApiResponse<bool>.Ok(true)) : BadRequest(result.Errors);
    }

    /// <summary>Search users by username or full name</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        var users = await _db.Users
            .Where(u => u.UserName!.Contains(q) || u.FullName.Contains(q))
            .Take(20)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl
            })
            .ToListAsync();

        return Ok(ApiResponse<List<UserSummaryDto>>.Ok(users));
    }
}

// ─── ADMIN REPORTS CONTROLLER ────────────────────────────────────────────────
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public ReportsController(AppDbContext db) => _db = db;

    /// <summary>Report a post</summary>
    [HttpPost("{postId:int}")]
    public async Task<IActionResult> ReportPost(int postId, [FromBody] CreateReportDto dto)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return NotFound(ApiResponse<bool>.Fail("Post not found."));

        _db.PostReports.Add(new Models.PostReport { PostId = postId, UserId = UserId, Reason = dto.Reason });
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<bool>.Ok(true, "Report submitted."));
    }

    /// <summary>Admin: Get all pending reports</summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetReports()
    {
        var reports = await _db.PostReports
            .Include(r => r.User)
            .Include(r => r.Post)
            .Where(r => r.Status == "pending")
            .Select(r => new
            {
                r.Id,
                r.Reason,
                r.Status,
                r.CreatedAt,
                ReportedBy = r.User.UserName,
                PostId = r.PostId
            })
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(reports));
    }

    /// <summary>Admin: Update report status</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateReport(int id, [FromQuery] string status)
    {
        var report = await _db.PostReports.FindAsync(id);
        if (report == null) return NotFound();
        report.Status = status;
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<bool>.Ok(true));
    }
}
