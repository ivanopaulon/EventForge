using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260625_0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_ExpiresAt",
                table: "UserRoles",
                columns: new[] { "UserId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemOperationLogs_Severity_ExecutedAt",
                table: "SystemOperationLogs",
                columns: new[] { "Severity", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Printers_IsDeleted_IsFiscalPrinter",
                table: "Printers",
                columns: new[] { "IsDeleted", "IsFiscalPrinter" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessParties_TenantId_IsDeleted_Name",
                table: "BusinessParties",
                columns: new[] { "TenantId", "IsDeleted", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRoles_UserId_ExpiresAt",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_SystemOperationLogs_Severity_ExecutedAt",
                table: "SystemOperationLogs");

            migrationBuilder.DropIndex(
                name: "IX_Printers_IsDeleted_IsFiscalPrinter",
                table: "Printers");

            migrationBuilder.DropIndex(
                name: "IX_BusinessParties_TenantId_IsDeleted_Name",
                table: "BusinessParties");
        }
    }
}
