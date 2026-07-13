using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260710_B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FidelityCardId",
                table: "SaleSessions",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FidelityCardId",
                table: "SaleSessions");
        }
    }
}
