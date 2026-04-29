using InteractHub.API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Post>         Posts         => Set<Post>();
    public DbSet<Comment>      Comments      => Set<Comment>();
    public DbSet<Like>         Likes         => Set<Like>();
    public DbSet<Hashtag>      Hashtags      => Set<Hashtag>();
    public DbSet<PostHashtag>  PostHashtags  => Set<PostHashtag>();
    public DbSet<Story>        Stories       => Set<Story>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Friendship>   Friendships   => Set<Friendship>();
    public DbSet<PostReport>   PostReports   => Set<PostReport>();
    public DbSet<StoryView>    StoryViews    => Set<StoryView>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // PostHashtag – composite PK
        builder.Entity<PostHashtag>()
            .HasKey(x => new { x.PostId, x.HashtagId });

        // Like – 1 user chỉ like 1 post 1 lần
        builder.Entity<Like>()
            .HasIndex(x => new { x.UserId, x.PostId }).IsUnique();

        // Friendship – không trùng cặp
        builder.Entity<Friendship>()
            .HasIndex(x => new { x.SenderId, x.ReceiverId }).IsUnique();

        // Friendship FK – dùng Restrict để tránh multiple cascade paths
        builder.Entity<Friendship>()
            .HasOne(f => f.Sender)
            .WithMany()
            .HasForeignKey(f => f.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Friendship>()
            .HasOne(f => f.Receiver)
            .WithMany()
            .HasForeignKey(f => f.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // Post self-reference FK – SharedPost
        builder.Entity<Post>()
            .HasOne(p => p.SharedPost)
            .WithMany()
            .HasForeignKey(p => p.SharedPostId)
            .OnDelete(DeleteBehavior.Restrict);

        // Comment FK – Restrict để tránh multiple cascade
        builder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Like FK – Restrict
        builder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Hashtag name unique
        builder.Entity<Hashtag>()
            .HasIndex(h => h.Name).IsUnique();

        // Notification.Actor FK – Restrict để tránh multiple cascade paths
        builder.Entity<Notification>()
            .HasOne(n => n.Actor)
            .WithMany()
            .HasForeignKey(n => n.ActorId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed roles
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().HasData(
            new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Id = "admin-role-id", Name = "Admin",
                NormalizedName = "ADMIN", ConcurrencyStamp = "1"
            },
            new Microsoft.AspNetCore.Identity.IdentityRole
            {
                Id = "user-role-id", Name = "User",
                NormalizedName = "USER", ConcurrencyStamp = "2"
            }
        );
    }
}
