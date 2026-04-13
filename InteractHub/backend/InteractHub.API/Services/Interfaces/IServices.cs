using InteractHub.API.DTOs;

namespace InteractHub.API.Services.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterDto dto);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
}

public interface IPostsService
{
    Task<PagedResultDto<PostResponseDto>> GetFeedAsync(string currentUserId, int page, int pageSize);
    Task<PagedResultDto<PostResponseDto>> GetUserPostsAsync(string userId, string currentUserId, int page, int pageSize);
    Task<ApiResponse<PostResponseDto>> GetByIdAsync(int id, string currentUserId);
    Task<ApiResponse<PostResponseDto>> CreateAsync(string userId, CreatePostDto dto);
    Task<ApiResponse<PostResponseDto>> UpdateAsync(int id, string userId, UpdatePostDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id, string userId);
    Task<ApiResponse<bool>> ToggleLikeAsync(int postId, string userId);
    Task<PagedResultDto<PostResponseDto>> SearchAsync(string query, string currentUserId, int page, int pageSize);
}

public interface ICommentsService
{
    Task<List<CommentResponseDto>> GetByPostAsync(int postId);
    Task<ApiResponse<CommentResponseDto>> CreateAsync(int postId, string userId, CreateCommentDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int commentId, string userId);
}

public interface IFriendsService
{
    Task<ApiResponse<FriendshipResponseDto>> SendRequestAsync(string senderId, string receiverId);
    Task<ApiResponse<FriendshipResponseDto>> RespondToRequestAsync(int friendshipId, string userId, bool accept);
    Task<List<FriendshipResponseDto>> GetFriendsAsync(string userId);
    Task<List<FriendshipResponseDto>> GetPendingRequestsAsync(string userId);
    Task<ApiResponse<bool>> RemoveFriendAsync(int friendshipId, string userId);
}

public interface IStoriesService
{
    Task<List<StoryResponseDto>> GetFeedAsync(string userId);
    Task<ApiResponse<StoryResponseDto>> CreateAsync(string userId, CreateStoryDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id, string userId);
}

public interface INotificationsService
{
    Task<List<NotificationResponseDto>> GetUserNotificationsAsync(string userId);
    Task<ApiResponse<bool>> MarkAsReadAsync(int id, string userId);
    Task MarkAllAsReadAsync(string userId);
    Task CreateNotificationAsync(string userId, string actorId, string type, string message, int? relatedPostId = null);
}

public interface IFileUploadService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteAsync(string fileUrl);
}
