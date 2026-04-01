using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Services;
using InteractHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace InteractHub.Tests;

// ─── STORIES SERVICE TESTS ───────────────────────────────────────────────────
public class StoriesServiceTests
{
    [Fact]
    public async Task CreateStory_WithCaption_ReturnsStory()
    {
        var db = TestHelpers.CreateInMemoryDb("stories_create_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new StoriesService(db);
        var result = await service.CreateAsync(user.Id, new CreateStoryDto
        {
            Caption = "Hello from story!",
            ImageUrl = "https://example.com/img.jpg"
        });

        Assert.True(result.Success);
        Assert.Equal("Hello from story!", result.Data!.Caption);
        Assert.Equal("https://example.com/img.jpg", result.Data!.ImageUrl);
    }

    [Fact]
    public async Task CreateStory_ExpiresIn24Hours()
    {
        var db = TestHelpers.CreateInMemoryDb("stories_expiry_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new StoriesService(db);
        var result = await service.CreateAsync(user.Id, new CreateStoryDto { Caption = "Test" });

        Assert.True(result.Success);
        var diff = result.Data!.ExpiresAt - result.Data!.CreatedAt;
        Assert.True(diff.TotalHours >= 23.9 && diff.TotalHours <= 24.1);
    }

    [Fact]
    public async Task GetFeed_ExcludesExpiredStories()
    {
        var db = TestHelpers.CreateInMemoryDb("stories_expired_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);

        // Active story
        db.Stories.Add(new Story
        {
            UserId = user.Id,
            User = user,
            Caption = "Active",
            ExpiresAt = DateTime.UtcNow.AddHours(23)
        });
        // Expired story
        db.Stories.Add(new Story
        {
            UserId = user.Id,
            User = user,
            Caption = "Expired",
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        });
        await db.SaveChangesAsync();

        var service = new StoriesService(db);
        var result = await service.GetFeedAsync(user.Id);

        Assert.Single(result);
        Assert.Equal("Active", result[0].Caption);
    }

    [Fact]
    public async Task DeleteStory_ByNonOwner_ReturnsUnauthorized()
    {
        var db = TestHelpers.CreateInMemoryDb("stories_delete_" + Guid.NewGuid());
        var owner = TestHelpers.CreateUser("owner");
        var other = TestHelpers.CreateUser("other");
        db.Users.AddRange(owner, other);
        db.Stories.Add(new Story
        {
            UserId = owner.Id,
            User = owner,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        });
        await db.SaveChangesAsync();

        var story = db.Stories.First();
        var service = new StoriesService(db);
        var result = await service.DeleteAsync(story.Id, other.Id);

        Assert.False(result.Success);
        Assert.Contains("Unauthorized", result.Errors.First());
    }
}

// ─── COMMENTS SERVICE TESTS ───────────────────────────────────────────────────
public class CommentsServiceTests
{
    private readonly Mock<INotificationsService> _notifMock = new();

    public CommentsServiceTests()
    {
        _notifMock.Setup(n => n.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateComment_OnExistingPost_ReturnsComment()
    {
        var db = TestHelpers.CreateInMemoryDb("comments_create_" + Guid.NewGuid());
        var author = TestHelpers.CreateUser("author");
        var commenter = TestHelpers.CreateUser("commenter");
        db.Users.AddRange(author, commenter);
        var post = new Post { Content = "Test post", UserId = author.Id, User = author };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new CommentsService(db, _notifMock.Object);
        var result = await service.CreateAsync(post.Id, commenter.Id, new CreateCommentDto
        {
            Content = "Great post!"
        });

        Assert.True(result.Success);
        Assert.Equal("Great post!", result.Data!.Content);
        Assert.Equal("commenter", result.Data!.Author.UserName);
    }

    [Fact]
    public async Task CreateComment_OnNonExistentPost_ReturnsFailure()
    {
        var db = TestHelpers.CreateInMemoryDb("comments_nopost_" + Guid.NewGuid());
        var service = new CommentsService(db, _notifMock.Object);

        var result = await service.CreateAsync(999, "user1", new CreateCommentDto { Content = "Hi" });

        Assert.False(result.Success);
        Assert.Contains("not found", result.Errors.First().ToLower());
    }

    [Fact]
    public async Task GetByPost_ReturnsOnlyNonDeletedComments()
    {
        var db = TestHelpers.CreateInMemoryDb("comments_get_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);
        var post = new Post { Content = "Post", UserId = user.Id, User = user };
        db.Posts.Add(post);
        db.Comments.AddRange(
            new Comment { PostId = 1, UserId = user.Id, User = user, Content = "Visible", IsDeleted = false },
            new Comment { PostId = 1, UserId = user.Id, User = user, Content = "Deleted", IsDeleted = true }
        );
        await db.SaveChangesAsync();

        var postId = db.Posts.First().Id;
        var service = new CommentsService(db, _notifMock.Object);
        var comments = await service.GetByPostAsync(postId);

        Assert.Single(comments);
        Assert.Equal("Visible", comments[0].Content);
    }

    [Fact]
    public async Task DeleteComment_ByOwner_SoftDeletes()
    {
        var db = TestHelpers.CreateInMemoryDb("comments_softdelete_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);
        var post = new Post { Content = "P", UserId = user.Id, User = user };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var comment = new Comment { PostId = db.Posts.First().Id, UserId = user.Id, User = user, Content = "Hi" };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var service = new CommentsService(db, _notifMock.Object);
        var result = await service.DeleteAsync(comment.Id, user.Id);

        Assert.True(result.Success);
        Assert.True(db.Comments.First().IsDeleted);
    }
}

// ─── NOTIFICATIONS SERVICE EXTENDED TESTS ───────────────────────────────────
public class NotificationsServiceExtendedTests
{
    [Fact]
    public async Task CreateNotification_PersistsCorrectly()
    {
        var db = TestHelpers.CreateInMemoryDb("notif_create_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new NotificationsService(db);
        await service.CreateNotificationAsync(user.Id, "actor1", "like", "liked your post", 42);

        var saved = db.Notifications.First();
        Assert.Equal(user.Id, saved.UserId);
        Assert.Equal("actor1", saved.ActorId);
        Assert.Equal("like", saved.Type);
        Assert.Equal(42, saved.RelatedPostId);
        Assert.False(saved.IsRead);
    }

    [Fact]
    public async Task MarkAsRead_ByWrongUser_ReturnsFailure()
    {
        var db = TestHelpers.CreateInMemoryDb("notif_wronguser_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);
        db.Notifications.Add(new Notification { UserId = user.Id, Message = "Test", Type = "like" });
        await db.SaveChangesAsync();

        var notif = db.Notifications.First();
        var service = new NotificationsService(db);
        var result = await service.MarkAsReadAsync(notif.Id, "wrong_user");

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GetNotifications_LimitedTo50()
    {
        var db = TestHelpers.CreateInMemoryDb("notif_limit_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);

        for (int i = 0; i < 60; i++)
            db.Notifications.Add(new Notification { UserId = user.Id, Message = $"Notif {i}", Type = "like" });
        await db.SaveChangesAsync();

        var service = new NotificationsService(db);
        var result = await service.GetUserNotificationsAsync(user.Id);

        Assert.Equal(50, result.Count);
    }
}

// ─── POSTS SERVICE EXTENDED TESTS ───────────────────────────────────────────
public class PostsServiceExtendedTests
{
    private readonly Mock<INotificationsService> _notifMock = new();

    public PostsServiceExtendedTests()
    {
        _notifMock.Setup(n => n.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task UpdatePost_ByOwner_UpdatesContent()
    {
        var db = TestHelpers.CreateInMemoryDb("posts_update_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);
        var post = new Post { Content = "Original", UserId = user.Id, User = user };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new PostsService(db, _notifMock.Object);
        var result = await service.UpdateAsync(post.Id, user.Id, new UpdatePostDto { Content = "Updated" });

        Assert.True(result.Success);
        Assert.Equal("Updated", result.Data!.Content);
        Assert.NotNull(db.Posts.First().UpdatedAt);
    }

    [Fact]
    public async Task GetFeed_ShowsOnlyFriendsAndOwnPosts()
    {
        var db = TestHelpers.CreateInMemoryDb("posts_feed_" + Guid.NewGuid());
        var me = TestHelpers.CreateUser("me");
        var friend = TestHelpers.CreateUser("friend");
        var stranger = TestHelpers.CreateUser("stranger");
        db.Users.AddRange(me, friend, stranger);

        db.Friendships.Add(new Friendship
        {
            SenderId = me.Id, ReceiverId = friend.Id,
            Sender = me, Receiver = friend,
            Status = "accepted"
        });

        db.Posts.AddRange(
            new Post { Content = "My post", UserId = me.Id, User = me },
            new Post { Content = "Friend post", UserId = friend.Id, User = friend },
            new Post { Content = "Stranger post", UserId = stranger.Id, User = stranger }
        );
        await db.SaveChangesAsync();

        var service = new PostsService(db, _notifMock.Object);
        var result = await service.GetFeedAsync(me.Id, 1, 10);

        Assert.Equal(2, result.Items.Count);
        Assert.DoesNotContain(result.Items, p => p.Content == "Stranger post");
    }

    [Fact]
    public async Task ToggleLike_SecondTime_RemovesLike()
    {
        var db = TestHelpers.CreateInMemoryDb("posts_unlike_" + Guid.NewGuid());
        var owner = TestHelpers.CreateUser("owner");
        var liker = TestHelpers.CreateUser("liker");
        db.Users.AddRange(owner, liker);
        var post = new Post { Content = "Post", UserId = owner.Id, User = owner };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        _notifMock.Setup(n => n.CreateNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);

        var service = new PostsService(db, _notifMock.Object);
        await service.ToggleLikeAsync(post.Id, liker.Id); // like
        var result = await service.ToggleLikeAsync(post.Id, liker.Id); // unlike

        Assert.True(result.Success);
        Assert.False(result.Data); // unliked
        Assert.Equal(0, db.Likes.Count());
    }

    [Fact]
    public async Task CreatePost_WithHashtags_SavesHashtags()
    {
        var db = TestHelpers.CreateInMemoryDb("posts_hashtags_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new PostsService(db, _notifMock.Object);
        var result = await service.CreateAsync(user.Id, new CreatePostDto
        {
            Content = "Post with tags",
            Hashtags = new List<string> { "dotnet", "csharp" }
        });

        Assert.True(result.Success);
        Assert.Contains("dotnet", result.Data!.Hashtags);
        Assert.Contains("csharp", result.Data!.Hashtags);
    }
}
