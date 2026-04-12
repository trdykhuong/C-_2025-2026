using InteractHub.API.DTOs;
using InteractHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InteractHub.API.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace InteractHub.API.Controllers;

[ApiController]

// ─── POSTS CONTROLLER ────────────────────────────────────────────────────────
[ApiController]
[Route("api/posts")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IPostsService _posts;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public PostsController(IPostsService posts) => _posts = posts;

    /// <summary>Get paginated news feed for current user</summary>
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _posts.GetFeedAsync(UserId, page, pageSize));

    /// <summary>Search posts by content or hashtag</summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _posts.SearchAsync(q, UserId, page, pageSize));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _posts.GetByIdAsync(id, UserId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserPosts(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _posts.GetUserPostsAsync(userId, UserId, page, pageSize));

    /// <summary>Create a new post</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostDto dto)
    {
        var result = await _posts.CreateAsync(UserId, dto);
        return result.Success ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    /// <summary>Update own post</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePostDto dto)
    {
        var result = await _posts.UpdateAsync(id, UserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete own post</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _posts.DeleteAsync(id, UserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Toggle like on a post</summary>
    [HttpPost("{id:int}/like")]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var result = await _posts.ToggleLikeAsync(id, UserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

// ─── COMMENTS CONTROLLER ─────────────────────────────────────────────────────
[ApiController]
[Route("api/posts/{postId:int}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentsService _comments;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public CommentsController(ICommentsService comments) => _comments = comments;

    /// <summary>Get all comments for a post</summary>
    [HttpGet]
    public async Task<IActionResult> GetByPost(int postId)
        => Ok(ApiResponse<List<CommentResponseDto>>.Ok(await _comments.GetByPostAsync(postId)));

    /// <summary>Add a comment to a post</summary>
    [HttpPost]
    public async Task<IActionResult> Create(int postId, [FromBody] CreateCommentDto dto)
    {
        var result = await _comments.CreateAsync(postId, UserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Delete own comment</summary>
    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int postId, int commentId)
    {
        var result = await _comments.DeleteAsync(commentId, UserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
    

