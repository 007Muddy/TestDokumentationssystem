using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplicationApi.Migrations
{
    /// <inheritdoc />
    public partial class TestLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Inspections_AspNetUsers_LoginId",
                table: "Inspections");

            migrationBuilder.DropForeignKey(
                name: "FK_Inspections_LoginModels_LoginModelId",
                table: "Inspections");

            migrationBuilder.DropIndex(
                name: "IX_Inspections_LoginId",
                table: "Inspections");

            migrationBuilder.DropIndex(
                name: "IX_Inspections_LoginModelId",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "LoginId",
                table: "Inspections");

            migrationBuilder.DropColumn(
                name: "LoginModelId",
                table: "Inspections");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Inspections",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Inspections");

            migrationBuilder.AddColumn<string>(
                name: "LoginId",
                table: "Inspections",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "LoginModelId",
                table: "Inspections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_LoginId",
                table: "Inspections",
                column: "LoginId");

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_LoginModelId",
                table: "Inspections",
                column: "LoginModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Inspections_AspNetUsers_LoginId",
                table: "Inspections",
                column: "LoginId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inspections_LoginModels_LoginModelId",
                table: "Inspections",
                column: "LoginModelId",
                principalTable: "LoginModels",
                principalColumn: "Id");
        }
    }
}
