using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Models;

namespace TelegramScheduler.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Post> Posts => Set<Post>();
}