using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportController : BaseController
{
    private readonly AppDbContext _db;
    public ReportController(AppDbContext db) => _db = db;

    // POST /api/reports/{postId}  – user báo cáo bài đăng
    [HttpPost("{postId:int}")]
    public async Task<IActionResult> Report(int postId, [FromBody] CreateReportDTO dto)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post == null) return NotFound(new { message = "Bài đăng không tồn tại." });

        _db.PostReports.Add(new PostReport
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
