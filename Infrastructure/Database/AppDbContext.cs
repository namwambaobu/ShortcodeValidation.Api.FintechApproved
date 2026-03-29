using Microsoft.EntityFrameworkCore;
using ShortcodeValidation.Api.Entities;

namespace ShortcodeValidation.Api.Infrastructure.Database;

public class AppDbContext : DbContext
{
    public DbSet<Shortcode> Shortcodes => Set<Shortcode>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
}