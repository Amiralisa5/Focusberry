using Focusberry.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Focusberry.Api.Data;

public sealed class FocusberryDbContext(DbContextOptions<FocusberryDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserState> States => Set<UserState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<UserState>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.Data).HasColumnType("jsonb").IsRequired();
            e.HasOne<User>().WithOne().HasForeignKey<UserState>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
