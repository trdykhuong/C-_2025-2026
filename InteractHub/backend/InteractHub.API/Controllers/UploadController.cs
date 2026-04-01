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
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/gif", "image/webp"];
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public UploadController(IFileUploadService fileUpload) => _fileUpload = fileUpload;

    /// <summary>Upload an image file, returns URL</summary>
    [HttpPost("image")]
    [RequestSizeLimit(5_242_880)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<string>.Fail("No file provided."));

        if (file.Length > MaxFileSize)
            return BadRequest(ApiResponse<string>.Fail("File too large. Max 5MB."));

        if (!AllowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(ApiResponse<string>.Fail("Only JPEG, PNG, GIF, WEBP images are allowed."));

        using var stream = file.OpenReadStream();
        var url = await _fileUpload.UploadAsync(stream, file.FileName, file.ContentType);

        return Ok(ApiResponse<string>.Ok(url, "Image uploaded successfully."));
    }
}
