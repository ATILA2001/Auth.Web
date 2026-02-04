using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Web.Data.Migrations
{
    [DbContext(typeof(AuthDbContext))]
    [Migration("20260203120000_RefactorRoutesAndPermissions")]
    public partial class RefactorRoutesAndPermissions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Limpiar rutas existentes para evitar conflictos al cambiar el tipo de ClientId
            migrationBuilder.Sql("DELETE FROM AreaRoutes");

            migrationBuilder.DropIndex(
                name: "IX_AreaRoutes_AreaId_ClientId_ReturnUrl",
                table: "AreaRoutes");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "AreaRoutes");

            migrationBuilder.AlterColumn<int>(
                name: "AreaId",
                table: "AreaRoutes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "AreaRoutes",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AreaRoutes_AreaId_ClientId_ReturnUrl' AND object_id = OBJECT_ID(N'[AreaRoutes]'))
    DROP INDEX [IX_AreaRoutes_AreaId_ClientId_ReturnUrl] ON [AreaRoutes];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AreaRoutes_AreaId' AND object_id = OBJECT_ID(N'[AreaRoutes]'))
    DROP INDEX [IX_AreaRoutes_AreaId] ON [AreaRoutes];
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AreaRoutes_ClientId' AND object_id = OBJECT_ID(N'[AreaRoutes]'))
    DROP INDEX [IX_AreaRoutes_ClientId] ON [AreaRoutes];
");

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_AreaId_ClientId_ReturnUrl",
                table: "AreaRoutes",
                columns: new[] { "AreaId", "ClientId", "ReturnUrl" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_AreaId",
                table: "AreaRoutes",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_ClientId",
                table: "AreaRoutes",
                column: "ClientId");

            migrationBuilder.AddForeignKey(
                name: "FK_AreaRoutes_Areas_AreaId",
                table: "AreaRoutes",
                column: "AreaId",
                principalTable: "Areas",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AreaRoutes_ApplicationClients_ClientId",
                table: "AreaRoutes",
                column: "ClientId",
                principalTable: "ApplicationClients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropForeignKey(
                name: "FK_RolePagePermissions_ActionPermissions_ActionPermissionId",
                table: "RolePagePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePagePermissions_Pages_PageId",
                table: "RolePagePermissions");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RolePagePermissions_RoleId_PageId_ActionPermissionId' AND object_id = OBJECT_ID(N'[RolePagePermissions]'))
    DROP INDEX [IX_RolePagePermissions_RoleId_PageId_ActionPermissionId] ON [RolePagePermissions];
");

            migrationBuilder.AlterColumn<int>(
                name: "ActionPermissionId",
                table: "RolePagePermissions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PageId",
                table: "RolePagePermissions",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePagePermissions_ActionPermissions_ActionPermissionId",
                table: "RolePagePermissions",
                column: "ActionPermissionId",
                principalTable: "ActionPermissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePagePermissions_Pages_PageId",
                table: "RolePagePermissions",
                column: "PageId",
                principalTable: "Pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AreaRoutes_Areas_AreaId",
                table: "AreaRoutes");

            migrationBuilder.DropForeignKey(
                name: "FK_AreaRoutes_ApplicationClients_ClientId",
                table: "AreaRoutes");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePagePermissions_ActionPermissions_ActionPermissionId",
                table: "RolePagePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePagePermissions_Pages_PageId",
                table: "RolePagePermissions");

            migrationBuilder.DropIndex(
                name: "IX_AreaRoutes_AreaId",
                table: "AreaRoutes");

            migrationBuilder.DropIndex(
                name: "IX_AreaRoutes_ClientId",
                table: "AreaRoutes");

            migrationBuilder.DropIndex(
                name: "IX_AreaRoutes_AreaId_ClientId_ReturnUrl",
                table: "AreaRoutes");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "AreaRoutes");

            migrationBuilder.AlterColumn<int>(
                name: "AreaId",
                table: "AreaRoutes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                table: "AreaRoutes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_AreaId_ClientId_ReturnUrl",
                table: "AreaRoutes",
                columns: new[] { "AreaId", "ClientId", "ReturnUrl" },
                unique: true);

            migrationBuilder.AlterColumn<int>(
                name: "ActionPermissionId",
                table: "RolePagePermissions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PageId",
                table: "RolePagePermissions",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePagePermissions_ActionPermissions_ActionPermissionId",
                table: "RolePagePermissions",
                column: "ActionPermissionId",
                principalTable: "ActionPermissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePagePermissions_Pages_PageId",
                table: "RolePagePermissions",
                column: "PageId",
                principalTable: "Pages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
