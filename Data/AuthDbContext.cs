using Auth.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Auth.Web.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<Page> Pages => Set<Page>();

    public DbSet<ActionPermission> ActionPermissions => Set<ActionPermission>();

    public DbSet<RolePagePermission> RolePagePermissions => Set<RolePagePermission>();

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<UserArea> UserAreas => Set<UserArea>();

    public DbSet<ApplicationClient> ApplicationClients => Set<ApplicationClient>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RolePagePermission>()
            .HasIndex(x => new { x.RoleId, x.PageId, x.ActionPermissionId })
            .IsUnique();

        builder.Entity<UserArea>()
            .HasIndex(x => new { x.UserId, x.AreaId })
            .IsUnique();
    }
}
