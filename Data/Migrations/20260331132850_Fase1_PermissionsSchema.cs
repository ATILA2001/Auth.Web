using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Fase1_PermissionsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClientId",
                table: "Pages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PermissionVersion",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 1);
            
            migrationBuilder.Sql("UPDATE AspNetUsers SET PermissionVersion = 1 WHERE PermissionVersion = 0");

            migrationBuilder.CreateTable(
                name: "AreaPagePermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    PageId = table.Column<int>(type: "int", nullable: true),
                    ActionPermissionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaPagePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaPagePermissions_ActionPermissions_ActionPermissionId",
                        column: x => x.ActionPermissionId,
                        principalTable: "ActionPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AreaPagePermissions_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AreaPagePermissions_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PermissionAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TargetAreaId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPageOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PageId = table.Column<int>(type: "int", nullable: true),
                    ActionPermissionId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPageOverrides", x => x.Id);
                    table.CheckConstraint("CK_UserPageOverride_GrantRequiresAction", "Type <> 'GRANT' OR ActionPermissionId IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_UserPageOverrides_ActionPermissions_ActionPermissionId",
                        column: x => x.ActionPermissionId,
                        principalTable: "ActionPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserPageOverrides_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_ClientId",
                table: "Pages",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaPagePermissions_ActionPermissionId",
                table: "AreaPagePermissions",
                column: "ActionPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AreaPagePermissions_AreaId_PageId_ActionPermissionId",
                table: "AreaPagePermissions",
                columns: new[] { "AreaId", "PageId", "ActionPermissionId" },
                unique: true,
                filter: "[PageId] IS NOT NULL AND [ActionPermissionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AreaPagePermissions_PageId",
                table: "AreaPagePermissions",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPageOverrides_ActionPermissionId",
                table: "UserPageOverrides",
                column: "ActionPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPageOverrides_PageId",
                table: "UserPageOverrides",
                column: "PageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pages_ApplicationClients_ClientId",
                table: "Pages",
                column: "ClientId",
                principalTable: "ApplicationClients",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pages_ApplicationClients_ClientId",
                table: "Pages");

            migrationBuilder.DropTable(
                name: "AreaPagePermissions");

            migrationBuilder.DropTable(
                name: "PermissionAuditLogs");

            migrationBuilder.DropTable(
                name: "UserPageOverrides");

            migrationBuilder.DropIndex(
                name: "IX_Pages_ClientId",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "ClientId",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "PermissionVersion",
                table: "AspNetUsers");
        }
    }
}
