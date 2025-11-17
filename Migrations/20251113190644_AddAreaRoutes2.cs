using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Web.Migrations
{
    public partial class AddAreaRoutes2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Crear la tabla completa (no asumir que existe)
            migrationBuilder.CreateTable(
                name: "AreaRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AreaId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReturnUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaRoutes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AreaRoutes_AreaId_ClientId_ReturnUrl",
                table: "AreaRoutes",
                columns: new[] { "AreaId", "ClientId", "ReturnUrl" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AreaRoutes");
        }
    }
}
