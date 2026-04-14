using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/posts/{postId:int}/comments")]
[Authorize]
public class CommentController : BaseController
{
    private readonly CommentService _commentService;
    public CommentController(CommentService commentService) => _commentService = commentService;

    // GET /api/posts/{postId}/comments
    [HttpGet]
    public async Task<IActionResult> GetAll(int postId)
        => Ok(await _commentService.GetByPostAsync(postId));

    // POST /api/posts/{postId}/comments
    [HttpPost]
    public async Task<IActionResult> Create(int postId, [FromBody] CreateCommentDTO dto)
    {
        var result = await _commentService.CreateAsync(postId, CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/posts/{postId}/comments/{commentId}
    [HttpPut("{commentId:int}")]
    public async Task<IActionResult> Update(int postId, int commentId, [FromBody] UpdateCommentDTO dto)
    {
        var result = await _commentService.UpdateAsync(commentId, CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/posts/{postId}/comments/{commentId}
    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> Delete(int postId, int commentId)
    {
        var result = await _commentService.DeleteAsync(commentId, CurrentUserId, IsAdmin);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
