using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;
using InteractHub.API.Services;
using Moq;
using Xunit;

namespace InteractHub.Tests;

// ─── STORIES SERVICE TESTS ───────────────────────────────────────────────────
public class StoriesServiceTests
{
    private static (StoryService service, Mock<IStoryRepository> repo) MakeService()
    {
        var repo = new Mock<IStoryRepository>();
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        return (new StoryService(repo.Object), repo);
    }

    [Fact]
    public async Task CreateStory_WithCaption_ReturnsStory()
    {
        var (service, repo) = MakeService();
        var user = TestHelpers.CreateUser("u1");
        repo.Setup(r => r.LoadUserAsync(It.IsAny<Story>()))
            .Callback<Story>(s => s.User = user)
            .Returns(Task.CompletedTask);

        var result = await service.CreateAsync(user.Id, new CreateStoryDTO
        {
            Caption  = "Hello from story!",
            ImageUrl = "https://example.com/img.jpg"
        });

        Assert.True(result.Success);
        Assert.Equal("Hello from story!", result.Data!.Caption);
        Assert.Equal("https://example.com/img.jpg", result.Data!.ImageUrl);
    }

    [Fact]
    public async Task CreateStory_ExpiresIn24Hours()
    {
        var (service, repo) = MakeService();
        var user = TestHelpers.CreateUser("u1");
        repo.Setup(r => r.LoadUserAsync(It.IsAny<Story>()))
            .Callback<Story>(s => s.User = user)
            .Returns(Task.CompletedTask);

        var result = await service.CreateAsync(user.Id, new CreateStoryDTO { Caption = "Test" });

        Assert.True(result.Success);
        var diff = result.Data!.ExpiresAt - result.Data!.CreatedAt;
        Assert.True(diff.TotalHours >= 23.9 && diff.TotalHours <= 24.1);
    }

    [Fact]
    public async Task GetFeed_ReturnsActiveStoriesFromRepo()
    {
        var (service, repo) = MakeService();
        var user = TestHelpers.CreateUser("u1");
        var activeStory = new Story
        {
            UserId    = user.Id,
            User      = user,
            Caption   = "Active",
            ExpiresAt = DateTime.UtcNow.AddHours(23)
        };

        repo.Setup(r => r.GetFriendIdsAsync(user.Id)).ReturnsAsync([]);
        repo.Setup(r => r.GetFeedAsync(It.IsAny<List<string>>(), user.Id))
            .ReturnsAsync([activeStory]);

        var result = await service.GetFeedAsync(user.Id);

        Assert.Single(result);
        Assert.Equal("Active", result[0].Caption);
    }

    [Fact]
    public async Task DeleteStory_ByNonOwner_ReturnsFailure()
    {
        var (service, repo) = MakeService();
        var story = new Story { Id = 1, UserId = "owner", ExpiresAt = DateTime.UtcNow.AddHours(24) };
        repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(story);

        var result = await service.DeleteAsync(1, "other");

        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }
}

// ─── COMMENTS SERVICE TESTS ───────────────────────────────────────────────────
public class CommentsServiceTests
{
    private static (CommentService service, Mock<ICommentRepository> repo) MakeService()
    {
        var repo      = new Mock<ICommentRepository>();
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var notifRepo = new Mock<INotificationRepository>();
        notifRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        return (new CommentService(repo.Object, TestHelpers.CreateNotifService(notifRepo)), repo);
    }

    [Fact]
    public async Task CreateComment_OnExistingPost_ReturnsComment()
    {
        var (service, repo) = MakeService();
        var author    = TestHelpers.CreateUser("author");
        var commenter = TestHelpers.CreateUser("commenter");
        var post      = new Post { Id = 1, Content = "Test post", UserId = author.Id, User = author };

        repo.Setup(r => r.FindPostAsync(1)).ReturnsAsync(post);
        repo.Setup(r => r.LoadUserAsync(It.IsAny<Comment>()))
            .Callback<Comment>(c => c.User = commenter)
            .Returns(Task.CompletedTask);

        var result = await service.CreateAsync(1, commenter.Id, new CreateCommentDTO { Content = "Great post!" });

        Assert.True(result.Success);
        Assert.Equal("Great post!", result.Data!.Content);
        Assert.Equal("commenter", result.Data!.Author.UserName);
    }

    [Fact]
    public async Task CreateComment_OnNonExistentPost_ReturnsFailure()
    {
        var (service, repo) = MakeService();
        repo.Setup(r => r.FindPostAsync(999)).ReturnsAsync((Post?)null);

        var result = await service.CreateAsync(999, "user1", new CreateCommentDTO { Content = "Hi" });

        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public async Task GetByPost_ReturnsCommentsFromRepo()
    {
        var (service, repo) = MakeService();
        var user    = TestHelpers.CreateUser("u1");
        var visible = new Comment { Id = 1, UserId = user.Id, User = user, Content = "Visible", IsDeleted = false };

        repo.Setup(r => r.GetByPostWithUserAsync(1)).ReturnsAsync([visible]);

        var comments = await service.GetByPostAsync(1);

        Assert.Single(comments);
        Assert.Equal("Visible", comments[0].Content);
    }

    [Fact]
    public async Task DeleteComment_ByOwner_SoftDeletes()
    {
        var (service, repo) = MakeService();
        var user    = TestHelpers.CreateUser("u1");
        var comment = new Comment { Id = 1, UserId = user.Id, User = user, Content = "Hi", IsDeleted = false };

        repo.Setup(r => r.FindActiveAsync(1)).ReturnsAsync(comment);

        var result = await service.DeleteAsync(1, user.Id);

        Assert.True(result.Success);
        Assert.True(comment.IsDeleted);
    }
}

// ─── NOTIFICATIONS SERVICE EXTENDED TESTS ───────────────────────────────────
public class NotificationsServiceExtendedTests
{
    private static (NotificationService service, Mock<INotificationRepository> repo) MakeService()
    {
        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        return (TestHelpers.CreateNotifService(repo), repo);
    }

    [Fact]
    public async Task CreateNotification_PersistsCorrectly()
    {
        var (service, repo) = MakeService();
        Notification? saved = null;
        repo.Setup(r => r.Add(It.IsAny<Notification>()))
            .Callback<Notification>(n => saved = n);

        await service.CreateAsync("u1", "actor1", "like", "đã thích bài viết của bạn.", 42);

        Assert.NotNull(saved);
        Assert.Equal("u1",   saved.UserId);
        Assert.Equal("like", saved.Type);
        Assert.Equal(42,     saved.RelatedPostId);
        Assert.False(saved.IsRead);
        // Message phải chứa verb (actor name được prepend bởi service)
        Assert.Contains("đã thích bài viết của bạn.", saved.Message);
    }

    [Fact]
    public async Task MarkAsRead_ByWrongUser_ReturnsFailure()
    {
        var (service, repo) = MakeService();
        var notif = new Notification { Id = 1, UserId = "u1", Message = "Test", Type = "like" };
        repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(notif);

        var result = await service.MarkReadAsync(1, "wrong_user");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetNotifications_ReturnsMappedResults()
    {
        var (service, repo) = MakeService();
        var notifs = Enumerable.Range(0, 50)
            .Select(i => new Notification { Id = i, UserId = "u1", Message = $"Notif {i}", Type = "like" })
            .ToList();
        repo.Setup(r => r.GetByUserAsync("u1")).ReturnsAsync(notifs);

        var result = await service.GetAllAsync("u1");

        Assert.Equal(50, result.Count);
    }
}

// ─── POSTS SERVICE EXTENDED TESTS ───────────────────────────────────────────
public class PostsServiceExtendedTests
{
    private static (PostService service, Mock<IPostRepository> repo) MakeService()
    {
        var repo      = new Mock<IPostRepository>();
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var notifRepo = new Mock<INotificationRepository>();
        notifRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        return (new PostService(repo.Object, TestHelpers.CreateNotifService(notifRepo)), repo);
    }

    [Fact]
    public async Task UpdatePost_ByOwner_UpdatesContent()
    {
        var (service, repo) = MakeService();
        var user = TestHelpers.CreateUser("u1");
        var post = new Post { Id = 1, Content = "Original", UserId = user.Id, User = user };

        repo.Setup(r => r.FindActiveWithUserAsync(1)).ReturnsAsync(post);

        var result = await service.UpdateAsync(1, user.Id, new UpdatePostDTO { Content = "Updated" });

        Assert.True(result.Success);
        Assert.Equal("Updated", result.Data!.Content);
        Assert.NotNull(post.UpdatedAt);
    }

    [Fact]
    public async Task GetFeed_CallsRepoWithFriendAndOwnIds()
    {
        var (service, repo) = MakeService();
        var user   = TestHelpers.CreateUser("me");
        var friend = TestHelpers.CreateUser("friend");
        var posts  = new List<Post>
        {
            new() { Id = 1, Content = "My post",    UserId = user.Id,   User = user,   Visibility = "public" },
            new() { Id = 2, Content = "Friend post", UserId = friend.Id, User = friend, Visibility = "public" },
        };

        repo.Setup(r => r.GetFriendIdsAsync("me")).ReturnsAsync(["friend"]);
        repo.Setup(r => r.GetFeedPagedAsync(
                It.Is<List<string>>(l => l.Contains("me") && l.Contains("friend")),
                "me", 1, 10))
            .ReturnsAsync((2, posts));

        var result = await service.GetFeedAsync("me", 1, 10);

        Assert.Equal(2, result.Items.Count);
        Assert.DoesNotContain(result.Items, p => p.Content == "Stranger post");
    }

    [Fact]
    public async Task ToggleLike_SecondTime_RemovesLike()
    {
        var (service, repo) = MakeService();
        var owner = TestHelpers.CreateUser("owner");
        var liker = TestHelpers.CreateUser("liker");
        var post  = new Post { Id = 1, Content = "Post", UserId = owner.Id, User = owner };
        var like  = new Like { PostId = 1, UserId = liker.Id };

        repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(post);
        repo.Setup(r => r.FindLikeAsync(1, liker.Id)).ReturnsAsync(like);

        var result = await service.ToggleLikeAsync(1, liker.Id);

        Assert.True(result.Success);
        Assert.False(result.Data);
        repo.Verify(r => r.RemoveLike(like), Times.Once);
    }

    [Fact]
    public async Task CreatePost_WithHashtags_SavesHashtags()
    {
        var (service, repo) = MakeService();
        var user = TestHelpers.CreateUser("u1");

        repo.Setup(r => r.FindHashtagByNameAsync(It.IsAny<string>())).ReturnsAsync((Hashtag?)null);
        repo.Setup(r => r.LoadUserAsync(It.IsAny<Post>()))
            .Callback<Post>(p => p.User = user)
            .Returns(Task.CompletedTask);

        var result = await service.CreateAsync(user.Id, new CreatePostDTO
        {
            Content  = "Post with tags",
            Hashtags = ["dotnet", "csharp"]
        });

        Assert.True(result.Success);
        Assert.Contains("dotnet", result.Data!.Hashtags);
        Assert.Contains("csharp", result.Data!.Hashtags);
    }
}
