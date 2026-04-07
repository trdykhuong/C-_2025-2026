using InteractHub.API.DTOs;
using InteractHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize]
public class UploadController : ControllerBase
{
    private readonly IFileUploadService _fileUpload;
    // Allow common image and video content types
    private static readonly string[] AllowedTypes = new[]
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "video/mp4", "video/webm", "video/quicktime"
    };

    // 50 MB max for media uploads in dev
    private const long MaxFileSize = 50 * 1024 * 1024;

    public UploadController(IFileUploadService fileUpload) => _fileUpload = fileUpload;

    /// <summary>Upload an image or video file, returns URL</summary>
    [HttpPost("media")]
    [RequestSizeLimit(52_428_800)] // ~50MB
    public async Task<IActionResult> UploadMedia(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("No file provided."));

        if (file.Length > MaxFileSize)
            return BadRequest(ApiResponse<string>.Fail("File too large. Max 50MB."));

        var contentType = (file.ContentType ?? string.Empty).ToLower();
        if (!AllowedTypes.Contains(contentType))
            return BadRequest(ApiResponse<string>.Fail("Unsupported file type."));

        using var stream = file.OpenReadStream();
        var url = await _fileUpload.UploadAsync(stream, file.FileName, file.ContentType ?? "application/octet-stream");

        return Ok(ApiResponse<string>.Ok(url, "Media uploaded successfully."));
    }
}
