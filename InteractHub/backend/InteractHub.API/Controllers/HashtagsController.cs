using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/hashtags")]
[Authorize]
public class HashtagsController : ControllerBase
{
    private readonly IHashtagsService _hashtags;
    public HashtagsController(IHashtagsService hashtags) => _hashtags = hashtags;

    /// <summary>Get trending hashtags</summary>
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int count = 10)
    {
        var trends = await _hashtags.GetTrendingAsync(count);
        return Ok(ApiResponse<List<HashtagTrendDto>>.Ok(trends));
    }
}
