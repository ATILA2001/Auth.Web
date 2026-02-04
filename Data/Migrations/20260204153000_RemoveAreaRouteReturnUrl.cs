using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Web.Data.Migrations
{
    [DbContext(typeof(AuthDbContext))]
    [Migration("20260204153000_RemoveAreaRouteReturnUrl")]
    public partial class RemoveAreaRouteReturnUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AreaRoutes_AreaId_ClientId_ReturnUrl' AND object_id = OBJECT_ID(N'[AreaRoutes]'))
    DROP INDEX [IX_AreaRoutes_AreaId_ClientId_ReturnUrl] ON [AreaRoutes];
");

            migrationBuilder.DropColumn(
                name: "ReturnUrl",
                table: "AreaRoutes");

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_AreaId_ClientId",
                table: "AreaRoutes",
                columns: new[] { "AreaId", "ClientId" },
                unique: true,
                filter: "[AreaId] IS NOT NULL AND [ClientId] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AreaRoutes_AreaId_ClientId",
                table: "AreaRoutes");

            migrationBuilder.AddColumn<string>(
                name: "ReturnUrl",
                table: "AreaRoutes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_AreaId_ClientId_ReturnUrl",
                table: "AreaRoutes",
                columns: new[] { "AreaId", "ClientId", "ReturnUrl" },
                unique: true);
        }
    }
}
