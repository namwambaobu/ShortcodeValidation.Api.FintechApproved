using Microsoft.EntityFrameworkCore;

namespace ShortcodeValidation.Api.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public DbSet<Shortcode> Shortcodes => Set<Shortcode>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}