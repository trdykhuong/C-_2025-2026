using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers;

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
