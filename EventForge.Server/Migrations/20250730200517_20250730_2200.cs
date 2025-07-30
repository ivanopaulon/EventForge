using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20250730_2200 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "UMs");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PaymentTerms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "UMs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PaymentTerms",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
