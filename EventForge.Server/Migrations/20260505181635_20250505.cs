using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20250505 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CreatesStockMovements",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ClosureType",
                table: "DailyClosureRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "FiscalClosurePending",
                table: "DailyClosureRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PrinterErrors",
                table: "DailyClosureRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlagReason",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FlaggedAt",
                table: "ChatMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FlaggedBy",
                table: "ChatMessages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFlagged",
                table: "ChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CashierShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PosId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ShiftStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CashierShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashierShifts_StorePoses_PosId",
                        column: x => x.PosId,
                        principalTable: "StorePoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CashierShifts_StoreUsers_StoreUserId",
                        column: x => x.StoreUserId,
                        principalTable: "StoreUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashierShift_PosId",
                table: "CashierShifts",
                column: "PosId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierShift_ShiftEnd",
                table: "CashierShifts",
                column: "ShiftEnd");

            migrationBuilder.CreateIndex(
                name: "IX_CashierShift_ShiftStart",
                table: "CashierShifts",
                column: "ShiftStart");

            migrationBuilder.CreateIndex(
                name: "IX_CashierShift_StoreUserId",
                table: "CashierShifts",
                column: "StoreUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierShift_TenantId_ShiftStart_ShiftEnd",
                table: "CashierShifts",
                columns: new[] { "TenantId", "ShiftStart", "ShiftEnd" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashierShifts");

            migrationBuilder.DropColumn(
                name: "CreatesStockMovements",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "ClosureType",
                table: "DailyClosureRecords");

            migrationBuilder.DropColumn(
                name: "FiscalClosurePending",
                table: "DailyClosureRecords");

            migrationBuilder.DropColumn(
                name: "PrinterErrors",
                table: "DailyClosureRecords");

            migrationBuilder.DropColumn(
                name: "FlagReason",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "FlaggedAt",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "FlaggedBy",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "IsFlagged",
                table: "ChatMessages");
        }
    }
}
