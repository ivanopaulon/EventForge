using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260406_4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedPrinterId",
                table: "Stations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedPrinterId1",
                table: "Stations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrintsReceiptCopy",
                table: "Stations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StationType",
                table: "Stations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "AgentId",
                table: "Printers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Printers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ConnectionType",
                table: "Printers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsThermal",
                table: "Printers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaperWidth",
                table: "Printers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrintLanguage",
                table: "Printers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrinterWidth",
                table: "Printers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsbDeviceId",
                table: "Printers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DailyClosureRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrinterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZReportNumber = table.Column<int>(type: "int", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReceiptCount = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    CashAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    CardAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    Operator = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HasPdf = table.Column<bool>(type: "bit", nullable: false),
                    PdfBytes = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PrinterResponse = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyClosureRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyClosureRecords_Printers_PrinterId",
                        column: x => x.PrinterId,
                        principalTable: "Printers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stations_AssignedPrinterId1",
                table: "Stations",
                column: "AssignedPrinterId1");

            migrationBuilder.CreateIndex(
                name: "IX_DailyClosureRecords_ClosedAt",
                table: "DailyClosureRecords",
                column: "ClosedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DailyClosureRecords_PrinterId",
                table: "DailyClosureRecords",
                column: "PrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyClosureRecords_PrinterId_ClosedAt",
                table: "DailyClosureRecords",
                columns: new[] { "PrinterId", "ClosedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Stations_Printers_AssignedPrinterId1",
                table: "Stations",
                column: "AssignedPrinterId1",
                principalTable: "Printers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stations_Printers_AssignedPrinterId1",
                table: "Stations");

            migrationBuilder.DropTable(
                name: "DailyClosureRecords");

            migrationBuilder.DropIndex(
                name: "IX_Stations_AssignedPrinterId1",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "AssignedPrinterId",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "AssignedPrinterId1",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "PrintsReceiptCopy",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "StationType",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "AgentId",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "ConnectionType",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "IsThermal",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "PaperWidth",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "PrintLanguage",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "PrinterWidth",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "UsbDeviceId",
                table: "Printers");
        }
    }
}
