using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class _20260204103000_SyncModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RolePagePermissions_RoleId_PageId_ActionPermissionId' AND object_id = OBJECT_ID(N'[RolePagePermissions]'))
    DROP INDEX [IX_RolePagePermissions_RoleId_PageId_ActionPermissionId] ON [RolePagePermissions];
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AreaRoutes_AreaId' AND object_id = OBJECT_ID(N'[AreaRoutes]'))
    DROP INDEX [IX_AreaRoutes_AreaId] ON [AreaRoutes];
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AreaRoutes_AreaId_ClientId_ReturnUrl' AND object_id = OBJECT_ID(N'[AreaRoutes]'))
    DROP INDEX [IX_AreaRoutes_AreaId_ClientId_ReturnUrl] ON [AreaRoutes];
");

            migrationBuilder.CreateIndex(
                name: "IX_RolePagePermissions_RoleId_PageId_ActionPermissionId",
                table: "RolePagePermissions",
                columns: new[] { "RoleId", "PageId", "ActionPermissionId" },
                unique: true,
                filter: "[PageId] IS NOT NULL AND [ActionPermissionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_AreaId_ClientId_ReturnUrl",
                table: "AreaRoutes",
                columns: new[] { "AreaId", "ClientId", "ReturnUrl" },
                unique: true,
                filter: "[AreaId] IS NOT NULL AND [ClientId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RolePagePermissions_RoleId_PageId_ActionPermissionId",
                table: "RolePagePermissions");

            migrationBuilder.DropIndex(
                name: "IX_AreaRoutes_AreaId_ClientId_ReturnUrl",
                table: "AreaRoutes");

            migrationBuilder.CreateIndex(
                name: "IX_RolePagePermissions_RoleId_PageId_ActionPermissionId",
                table: "RolePagePermissions",
                columns: new[] { "RoleId", "PageId", "ActionPermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_AreaId",
                table: "AreaRoutes",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_AreaId_ClientId_ReturnUrl",
                table: "AreaRoutes",
                columns: new[] { "AreaId", "ClientId", "ReturnUrl" },
                unique: true);
        }
    }
}
