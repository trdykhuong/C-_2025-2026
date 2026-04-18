using InteractHub.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/hashtags")]
[Authorize]
public class HashtagController : ControllerBase
{
    private readonly AppDbContext _db;
    public HashtagController(AppDbContext db) => _db = db;

    // GET /api/hashtags/trending
    [HttpGet("trending")]
    public async Task<IActionResult> GetTrending([FromQuery] int top = 10)
    {
        var trending = await _db.PostHashtags
            .Where(ph => !ph.Post.IsDeleted)
            .GroupBy(ph => ph.Hashtag.Name)
            .Select(g => new { name = g.Key, count = g.Count() })
            .OrderByDescending(g => g.count)
            .Take(top)
            .ToListAsync();

        return Ok(trending);
    }
}
