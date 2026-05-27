using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Web.Data.Migrations
{
    [DbContext(typeof(AuthDbContext))]
    [Migration("20260526093000_UserPageOverrideIsAllowed")]
    public partial class UserPageOverrideIsAllowed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserPageOverride_GrantRequiresAction",
                table: "UserPageOverrides");

            migrationBuilder.AddColumn<bool>(
                name: "IsAllowed",
                table: "UserPageOverrides",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE [UserPageOverrides] SET [IsAllowed] = CASE WHEN [Type] = N'GRANT' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "UserPageOverrides");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserPageOverride_AllowedRequiresAction",
                table: "UserPageOverrides",
                sql: "[IsAllowed] = 0 OR [ActionPermissionId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserPageOverride_AllowedRequiresAction",
                table: "UserPageOverrides");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "UserPageOverrides",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "DENY");

            migrationBuilder.Sql(
                "UPDATE [UserPageOverrides] SET [Type] = CASE WHEN [IsAllowed] = CAST(1 AS bit) THEN N'GRANT' ELSE N'DENY' END");

            migrationBuilder.DropColumn(
                name: "IsAllowed",
                table: "UserPageOverrides");

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserPageOverride_GrantRequiresAction",
                table: "UserPageOverrides",
                sql: "Type <> 'GRANT' OR ActionPermissionId IS NOT NULL");
        }
    }
}
