using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers;

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

        if (file.Length > 50 * 1024 * 1024)
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
