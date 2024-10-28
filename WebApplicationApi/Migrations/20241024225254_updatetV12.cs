using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplicationApi.Migrations
{
    /// <inheritdoc />
    public partial class updatetV12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoPaths",
                table: "Inspections");

            migrationBuilder.CreateTable(
                name: "Photo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PhotoPath = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    InspectionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photo_Inspections_InspectionId",
                        column: x => x.InspectionId,
                        principalTable: "Inspections",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Photo_InspectionId",
                table: "Photo",
                column: "InspectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Photo");

            migrationBuilder.AddColumn<string>(
                name: "PhotoPaths",
                table: "Inspections",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
