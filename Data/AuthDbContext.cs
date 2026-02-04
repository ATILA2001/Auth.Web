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

        builder.Entity<RolePagePermission>()
            .HasOne(x => x.Page)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<RolePagePermission>()
            .HasOne(x => x.ActionPermission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.ActionPermissionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<UserArea>()
            .HasIndex(x => new { x.UserId, x.AreaId })
            .IsUnique();

        builder.Entity<AreaRoute>()
            .HasIndex(x => new { x.AreaId, x.ClientId })
            .IsUnique();

        builder.Entity<AreaRoute>()
            .HasOne(x => x.Area)
            .WithMany()
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<AreaRoute>()
            .HasOne(x => x.Client)
            .WithMany()
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.SetNull);

    }
}
