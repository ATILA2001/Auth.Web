using Auth.Web.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace Auth.Web.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options), IDataProtectionKeyContext
{
    public DbSet<Page> Pages => Set<Page>();

    public DbSet<ActionPermission> ActionPermissions => Set<ActionPermission>();

    public DbSet<RolePagePermission> RolePagePermissions => Set<RolePagePermission>();

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<UserArea> UserAreas => Set<UserArea>();

    public DbSet<ApplicationClient> ApplicationClients => Set<ApplicationClient>();

    public DbSet<AreaRoute> AreaRoutes => Set<AreaRoute>();

    // Data Protection key storage for shared cookie SSO
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RolePagePermission>()
            .HasIndex(x => new { x.RoleId, x.PageId, x.ActionPermissionId })
            .IsUnique();

        builder.Entity<UserArea>()
            .HasIndex(x => new { x.UserId, x.AreaId })
            .IsUnique();

        builder.Entity<AreaRoute>()
            .HasIndex(x => new { x.AreaId, x.ClientId, x.ReturnUrl })
            .IsUnique();

        // Longitudes para permitir indexar
        builder.Entity<AreaRoute>()
            .Property(r => r.ClientId)
            .HasMaxLength(100);

        builder.Entity<AreaRoute>()
            .Property(r => r.ReturnUrl)
            .HasMaxLength(450);
    }
}
