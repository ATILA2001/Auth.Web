using Microsoft.EntityFrameworkCore;
using Auth.Web.Data;
using Auth.Web.Domain.Entities;

namespace Auth.Web.Services.Admin;

public interface IUserAreaAdminService
{
    Task<UserAreaData> GetAsync(CancellationToken ct = default);
    Task<bool> AssignAsync(string userId, int areaId, CancellationToken ct = default);
    Task<bool> RemoveAsync(int userAreaId, CancellationToken ct = default);
}

public sealed record UserListItem(string Id, string? Email, string? UserName)
{
    public override string ToString() => string.IsNullOrWhiteSpace(Email) ? UserName ?? Id : Email!;
}

public sealed record UserAreaData(List<Area> Areas, List<UserArea> UserAreas, List<UserListItem> Users);

public sealed class UserAreaAdminService : IUserAreaAdminService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public UserAreaAdminService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public async Task<UserAreaData> GetAsync(CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var areas = await db.Areas.AsNoTracking().OrderBy(a => a.Name).ToListAsync(ct);
        var userAreas = await db.UserAreas.AsNoTracking().OrderBy(ua => ua.UserId).ThenBy(ua => ua.AreaId).ToListAsync(ct);
        var users = await db.Users.AsNoTracking()
            .OrderBy(u => u.Email).Select(u => new UserListItem(u.Id, u.Email, u.UserName)).ToListAsync(ct);
        return new UserAreaData(areas, userAreas, users);
    }

    public async Task<bool> AssignAsync(string userId, int areaId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        if (await db.UserAreas.AnyAsync(ua => ua.UserId == userId && ua.AreaId == areaId, ct)) return true;
        db.UserAreas.Add(new UserArea { UserId = userId, AreaId = areaId });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveAsync(int userAreaId, CancellationToken ct = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var entity = await db.UserAreas.FindAsync(new object[] { userAreaId }, ct);
        if (entity is null) return false;
        db.UserAreas.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
