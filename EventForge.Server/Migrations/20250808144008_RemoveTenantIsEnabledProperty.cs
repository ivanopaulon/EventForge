using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantIsEnabledProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "Tenants",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }
    }
}
