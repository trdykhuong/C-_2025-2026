using InteractHub.API.Data;
using InteractHub.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController : BaseController
{
    private readonly AppDbContext          _db;
    private readonly UserManager<AppUser>  _userManager;

    public AdminController(AppDbContext db, UserManager<AppUser> userManager)
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

    // GET /api/admin/posts?page=1&deleted=false
    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] string keyword = "", [FromQuery] bool deleted = false)
    {
        var query = _db.Posts
            .Include(p => p.User)
            .Include(p => p.PostReports)
            .Where(p => p.IsDeleted == deleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(p => p.Content.Contains(keyword) || p.User.FullName.Contains(keyword));

        var total = await query.CountAsync();
        var posts = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * 20).Take(20)
            .Select(p => new
            {
                p.Id, p.Content, p.ImageUrl, p.Visibility, p.IsDeleted, p.CreatedAt,
                Author      = new { p.User.Id, p.User.FullName, p.User.UserName, p.User.AvatarUrl },
                ReportCount = p.PostReports.Count(r => r.Status == "pending"),
            })
            .ToListAsync();

        return Ok(new { items = posts, totalCount = total, page, pageSize = 20, hasNext = page * 20 < total });
    }

    // DELETE /api/admin/posts/{id}
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
                Post     = new { r.Post.Id, r.Post.Content, r.Post.IsDeleted, Author = new { r.Post.User.FullName } },
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
