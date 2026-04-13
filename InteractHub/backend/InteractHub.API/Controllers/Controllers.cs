using System.Security.Claims;
using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Controllers;

// Helper để lấy userId từ JWT claims
public class BaseController : ControllerBase
{
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    protected bool   IsAdmin       => User.IsInRole("Admin");
}

// ════════════════════════════════════════════════════════════
// AUTH CONTROLLER
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/auth")]
public class AuthController : BaseController
{
    private readonly AuthService _authService;
    public AuthController(AuthService authService) => _authService = authService;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        var result = await _authService.LoginAsync(dto);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new { userId = CurrentUserId, userName = User.Identity?.Name });
    }
}

// ════════════════════════════════════════════════════════════
// POST CONTROLLER
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/posts")]
[Authorize]
public class PostController : BaseController
{
    private readonly PostService _postService;
    public PostController(PostService postService) => _postService = postService;

    // GET /api/posts/feed?page=1&pageSize=10
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _postService.GetFeedAsync(CurrentUserId, page, pageSize));

    // GET /api/posts/user/{userId}
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId, [FromQuery] int page = 1)
        => Ok(await _postService.GetByUserAsync(userId, CurrentUserId, page, 10));

    // GET /api/posts/search?keyword=abc
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword, [FromQuery] int page = 1)
        => Ok(await _postService.SearchAsync(keyword, CurrentUserId, page, 10));

    // GET /api/posts/hashtag/{tag}
    [HttpGet("hashtag/{tag}")]
    public async Task<IActionResult> GetByHashtag(string tag, [FromQuery] int page = 1)
        => Ok(await _postService.GetByHashtagAsync(tag, CurrentUserId, page, 10));

    // POST /api/posts
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostDTO dto)
    {
        var result = await _postService.CreateAsync(CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/posts/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostDTO dto)
    {
        var result = await _postService.UpdateAsync(id, CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/posts/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _postService.DeleteAsync(id, CurrentUserId, IsAdmin);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/posts/{id}/like
    [HttpPost("{id:int}/like")]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var result = await _postService.ToggleLikeAsync(id, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// ════════════════════════════════════════════════════════════
// COMMENT CONTROLLER
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/posts/{postId:int}/comments")]
[Authorize]
public class CommentController : BaseController
{
    private readonly CommentService _commentService;
    public CommentController(CommentService commentService) => _commentService = commentService;

    // GET /api/posts/{postId}/comments
    [HttpGet]
    public async Task<IActionResult> GetAll(int postId)
        => Ok(await _commentService.GetByPostAsync(postId));

    // POST /api/posts/{postId}/comments
    [HttpPost]
    public async Task<IActionResult> Create(int postId, [FromBody] CreateCommentDTO dto)
    {
        var result = await _commentService.CreateAsync(postId, CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/posts/{postId}/comments/{commentId}
    [HttpPut("{commentId:int}")]
    public async Task<IActionResult> Update(int postId, int commentId, [FromBody] UpdateCommentDTO dto)
    {
        var result = await _commentService.UpdateAsync(commentId, CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/posts/{postId}/comments/{commentId}
    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int postId, int commentId)
    {
        var result = await _commentService.DeleteAsync(commentId, CurrentUserId, IsAdmin);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// ════════════════════════════════════════════════════════════
// FRIEND CONTROLLER
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/friends")]
[Authorize]
public class FriendController : BaseController
{
    private readonly FriendService _friendService;
    public FriendController(FriendService friendService) => _friendService = friendService;

    // GET /api/friends  – danh sách bạn bè
    [HttpGet]
    public async Task<IActionResult> GetFriends()
        => Ok(await _friendService.GetFriendsAsync(CurrentUserId));

    // GET /api/friends/requests  – lời mời đang chờ
    [HttpGet("requests")]
    public async Task<IActionResult> GetPending()
        => Ok(await _friendService.GetPendingAsync(CurrentUserId));

    // POST /api/friends/request
    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDTO dto)
    {
        var result = await _friendService.SendRequestAsync(CurrentUserId, dto.ReceiverId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/friends/request/{id}?accept=true
    [HttpPut("request/{id:int}")]
    public async Task<IActionResult> Respond(int id, [FromQuery] bool accept)
    {
        var result = await _friendService.RespondAsync(id, CurrentUserId, accept);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/friends/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var result = await _friendService.RemoveAsync(id, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/friends/with/{userId}  – hủy kết bạn / hủy lời mời bằng userId
    [HttpDelete("with/{userId}")]
    public async Task<IActionResult> RemoveByUser(string userId)
    {
        var result = await _friendService.RemoveByUserIdAsync(CurrentUserId, userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// ════════════════════════════════════════════════════════════
// STORY CONTROLLER
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/stories")]
[Authorize]
public class StoryController : BaseController
{
    private readonly StoryService _storyService;
    public StoryController(StoryService storyService) => _storyService = storyService;

    // GET /api/stories
    [HttpGet]
    public async Task<IActionResult> GetFeed()
        => Ok(await _storyService.GetFeedAsync(CurrentUserId));

    // POST /api/stories
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoryDTO dto)
    {
        var result = await _storyService.CreateAsync(CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/stories/{id}/view  – ghi nhận lượt xem
    [HttpPost("{id:int}/view")]
    public async Task<IActionResult> RecordView(int id)
    {
        await _storyService.RecordViewAsync(id, CurrentUserId);
        return Ok();
    }

    // DELETE /api/stories/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _storyService.DeleteAsync(id, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// ════════════════════════════════════════════════════════════
// NOTIFICATION CONTROLLER
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : BaseController
{
    private readonly NotificationService _notifService;
    public NotificationController(NotificationService notifService) => _notifService = notifService;

    // GET /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _notifService.GetAllAsync(CurrentUserId));

    // PUT /api/notifications/{id}/read
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var result = await _notifService.MarkReadAsync(id, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifService.MarkAllReadAsync(CurrentUserId);
        return Ok(new { success = true });
    }
}

// ════════════════════════════════════════════════════════════
// USER CONTROLLER
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/users")]
[Authorize]
public class UserController : BaseController
{
    private readonly UserService _userService;
    public UserController(UserService userService) => _userService = userService;

    // GET /api/users/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetProfile(string id)
    {
        var result = await _userService.GetProfileAsync(id, CurrentUserId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    // PUT /api/users/me
    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
    {
        var result = await _userService.UpdateProfileAsync(CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/users/search?keyword=abc
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword)
        => Ok(await _userService.SearchAsync(keyword));
}

// ════════════════════════════════════════════════════════════
// UPLOAD CONTROLLER
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/upload")]
[Authorize]
public class UploadController : BaseController
{
    private static readonly string[] AllowedImages = [".jpg", ".jpeg", ".png", ".gif", ".webp"];
    private static readonly string[] AllowedVideos = [".mp4", ".webm", ".mov", ".avi"];

    // POST /api/upload
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Không có file." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedImages.Contains(ext) && !AllowedVideos.Contains(ext))
            return BadRequest(new { success = false, message = "Định dạng không được hỗ trợ (jpg/png/gif/mp4/webm/mov)." });

        if (file.Length > 50 * 1024 * 1024) // 50 MB
            return BadRequest(new { success = false, message = "File quá lớn (tối đa 50MB)." });

        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var url = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
        return Ok(new { success = true, url });
    }
}

// ════════════════════════════════════════════════════════════
// ADMIN CONTROLLER
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    private readonly InteractHub.API.Data.AppDbContext _db;
    private readonly Microsoft.AspNetCore.Identity.UserManager<InteractHub.API.Models.AppUser> _userManager;

    public AdminController(
        InteractHub.API.Data.AppDbContext db,
        Microsoft.AspNetCore.Identity.UserManager<InteractHub.API.Models.AppUser> userManager)
    {
        _db          = db;
        _userManager = userManager;
    }

    // GET /api/admin/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers   = await _db.Users.CountAsync();
        var totalPosts   = await _db.Posts.CountAsync(p => !p.IsDeleted);
        var totalReports = await _db.PostReports.CountAsync(r => r.Status == "pending");
        var totalStories = await _db.Stories.CountAsync(s => s.ExpiresAt > DateTime.UtcNow);
        return Ok(new { totalUsers, totalPosts, totalReports, totalStories });
    }

    // GET /api/admin/users?page=1&keyword=
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] string keyword = "")
    {
        var query = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(u => u.UserName!.Contains(keyword) || u.FullName.Contains(keyword) || u.Email!.Contains(keyword));

        var total = await query.CountAsync();
        var users = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * 20).Take(20)
            .Select(u => new { u.Id, u.UserName, u.FullName, u.Email, u.AvatarUrl, u.IsActive, u.CreatedAt })
            .ToListAsync();

        return Ok(new { items = users, totalCount = total, page, pageSize = 20 });
    }

    // PUT /api/admin/users/{id}/toggle-active
    [HttpPut("users/{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await _userManager.UpdateAsync(user);
        return Ok(new { success = true, isActive = user.IsActive });
    }

    // GET /api/admin/posts?page=1
    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] string keyword = "")
    {
        var query = _db.Posts
            .Include(p => p.User)
            .Include(p => p.PostReports)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(p => p.Content.Contains(keyword) || p.User.FullName.Contains(keyword));

        var total = await query.CountAsync();
        var posts = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * 20).Take(20)
            .Select(p => new
            {
                p.Id, p.Content, p.ImageUrl, p.Visibility, p.IsDeleted, p.CreatedAt,
                Author = new { p.User.Id, p.User.FullName, p.User.UserName, p.User.AvatarUrl },
                ReportCount = p.PostReports.Count(r => r.Status == "pending"),
            })
            .ToListAsync();

        return Ok(new { items = posts, totalCount = total, page, pageSize = 20 });
    }

    // DELETE /api/admin/posts/{id}  – xóa hẳn (hard delete)
    [HttpDelete("posts/{id:int}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null) return NotFound();
        post.IsDeleted = true;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // GET /api/admin/reports
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports([FromQuery] string status = "pending")
    {
        var reports = await _db.PostReports
            .Include(r => r.User)
            .Include(r => r.Post).ThenInclude(p => p.User)
            .Where(r => status == "all" || r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .Take(100)
            .Select(r => new
            {
                r.Id, r.Reason, r.Status, r.CreatedAt,
                Reporter = new { r.User.FullName, r.User.UserName },
                Post = new { r.Post.Id, r.Post.Content, r.Post.IsDeleted, Author = new { r.Post.User.FullName } },
            })
            .ToListAsync();
        return Ok(reports);
    }

    // PUT /api/admin/reports/{id}
    [HttpPut("reports/{id:int}")]
    public async Task<IActionResult> UpdateReport(int id, [FromQuery] string status)
    {
        var report = await _db.PostReports.FindAsync(id);
        if (report == null) return NotFound();
        report.Status = status;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}

// ════════════════════════════════════════════════════════════
// REPORT CONTROLLER (Admin moderation)
// ════════════════════════════════════════════════════════════
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportController : BaseController
{
    private readonly InteractHub.API.Data.AppDbContext _db;
    public ReportController(InteractHub.API.Data.AppDbContext db) => _db = db;

    // POST /api/reports/{postId}  – user báo cáo bài đăng
    [HttpPost("{postId:int}")]
    public async Task<IActionResult> Report(int postId, [FromBody] CreateReportDTO dto)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return NotFound(new { message = "Bài đăng không tồn tại." });

        _db.PostReports.Add(new InteractHub.API.Models.PostReport
        {
            PostId = postId,
            UserId = CurrentUserId,
            Reason = dto.Reason,
        });
        await _db.SaveChangesAsync();
        return Ok(new { success = true, message = "Đã gửi báo cáo." });
    }

    // GET /api/reports  – admin xem danh sách báo cáo
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var reports = await _db.PostReports
            .Select(r => new { r.Id, r.Reason, r.Status, r.CreatedAt, r.PostId, r.UserId })
            .ToListAsync();
        return Ok(reports);
    }

    // PUT /api/reports/{id}?status=reviewed  – admin cập nhật trạng thái
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
    {
        var report = await _db.PostReports.FindAsync(id);
        if (report == null) return NotFound();
        report.Status = status;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
