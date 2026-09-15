using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Models;

namespace TelegramScheduler.Data;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    public DbSet<TelegramAccount> TelegramAccounts => Set<TelegramAccount>();
    public DbSet<Channel> Channels => Set<Channel>();

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostAttachment> PostAttachments => Set<PostAttachment>();

    public DbSet<SlotTemplate> SlotTemplates => Set<SlotTemplate>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>()
            .HasOne(x => x.Settings)
            .WithOne(x => x.User)
            .HasForeignKey<UserSettings>(x => x.UserId);

        builder.Entity<User>()
            .HasIndex(x => x.NormalizedLogin)
            .IsUnique();

        builder.Entity<User>()
            .Property(x => x.Login)
            .HasMaxLength(64);

        builder.Entity<User>()
            .Property(x => x.NormalizedLogin)
            .HasMaxLength(64);

        builder.Entity<User>()
            .Property(x => x.PasswordHash)
            .HasMaxLength(512);
        
        builder.Entity<Channel>()
            .HasOne(x => x.TelegramAccount)
            .WithMany(x => x.Channels)
            .HasForeignKey(x => x.TelegramAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Post>()
            .HasIndex(x => x.ScheduledAtUtc);

        builder.Entity<Post>()
            .HasIndex(x => new
            {
                x.ChannelId,
                x.ScheduledAtUtc
            });
        
        builder.Entity<TelegramAccount>()
            .HasOne(x => x.User)
            .WithMany(x => x.TelegramAccounts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostAttachment>()
            .HasIndex(x => x.PostId);

        builder.Entity<SlotTemplate>()
            .HasIndex(x => new
            {
                x.ChannelId,
                x.TimeOfDay
            })
            .IsUnique();
    }
}
