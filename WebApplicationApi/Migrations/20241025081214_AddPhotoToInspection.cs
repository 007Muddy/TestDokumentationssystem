using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplicationApi.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoToInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photo_Inspections_InspectionId",
                table: "Photo");

            migrationBuilder.AlterColumn<int>(
                name: "InspectionId",
                table: "Photo",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Photo_Inspections_InspectionId",
                table: "Photo",
                column: "InspectionId",
                principalTable: "Inspections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photo_Inspections_InspectionId",
                table: "Photo");

            migrationBuilder.AlterColumn<int>(
                name: "InspectionId",
                table: "Photo",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddForeignKey(
                name: "FK_Photo_Inspections_InspectionId",
                table: "Photo",
                column: "InspectionId",
                principalTable: "Inspections",
                principalColumn: "Id");
        }
    }
}
