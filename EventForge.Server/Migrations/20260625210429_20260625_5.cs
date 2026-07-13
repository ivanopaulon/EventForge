using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260625_5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultMovementReason",
                table: "DocumentTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTransferDocument",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultMovementReason",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "IsTransferDocument",
                table: "DocumentTypes");
        }
    }
}
