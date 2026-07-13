using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260619_2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "DocumentHeaders");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "DocumentHeaders");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "DocumentHeaders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "DocumentVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "DocumentVersions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "DocumentVersions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "DocumentHeaders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "DocumentHeaders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "DocumentHeaders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
