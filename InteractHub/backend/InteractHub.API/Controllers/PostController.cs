using System.Security.Claims;
using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace InteractHub.API.Controllers;
public class PostController : ControllerBase{
     protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    protected bool   IsAdmin       => User.IsInRole("Admin");
}[ApiController]
[Route("api/posts")]
[Authorize]
public class PostController : BaseController{
    private readonly Postservice _postService;
    public PostController(PostService postService) => _postService = postService;
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _postService.GetFeedAsync(CurrentUserId, page, pageSize));
    [HttpGet("user/{userID}")]
    public async Task<IActionResult> GetbyUser(string userId,[FromQuery] int page = 1)
    => Ok(await _postService.GetByUserAsync(userId, CurrentUserId ,page,10));

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string keyword , [FromQuery] int page = 1)
    => Ok(await _postService.SearchAsync(keyword, CurrentUserId, page, 10));

}