using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplicationApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoToInspection12dfds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoPath",
                table: "Photo");

            migrationBuilder.AddColumn<byte[]>(
                name: "PhotoData",
                table: "Photo",
                type: "BLOB",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoData",
                table: "Photo");

            migrationBuilder.AddColumn<string>(
                name: "PhotoPath",
                table: "Photo",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
