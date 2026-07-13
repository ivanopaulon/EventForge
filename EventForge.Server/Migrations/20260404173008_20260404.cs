using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260404 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "StoreUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "References",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxTotalDiscountPercentage",
                table: "Promotions",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUsesPerCustomer",
                table: "Promotions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "Events",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Events",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "Events",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "BusinessParties",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CalendarReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsAllDay = table.Column<bool>(type: "bit", nullable: false),
                    ItemType = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    RecurrencePattern = table.Column<int>(type: "int", nullable: true),
                    RecurrenceInterval = table.Column<int>(type: "int", nullable: true),
                    RecurrenceEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Visibility = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CalendarReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalendarReminders_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EventTimeSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTimeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTimeSlots_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TenantId_IsActive",
                table: "Promotions",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_TenantId_StartDate_EndDate",
                table: "Promotions",
                columns: new[] { "TenantId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_TenantId_Priority",
                table: "PriceLists",
                columns: new[] { "TenantId", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRows_AppliedPromotionsJSON_NotNull",
                table: "DocumentRows",
                columns: new[] { "TenantId", "DocumentHeaderId" },
                filter: "[AppliedPromotionsJSON] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRows_IsPriceManual",
                table: "DocumentRows",
                column: "IsPriceManual");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRows_TenantId_ProductId",
                table: "DocumentRows",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHeaders_TenantId_Date",
                table: "DocumentHeaders",
                columns: new[] { "TenantId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarReminders_DueDate",
                table: "CalendarReminders",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarReminders_EventId",
                table: "CalendarReminders",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarReminders_TenantId",
                table: "CalendarReminders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CalendarReminders_TenantId_Status",
                table: "CalendarReminders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EventTimeSlots_EventId",
                table: "EventTimeSlots",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTimeSlots_EventId_SortOrder",
                table: "EventTimeSlots",
                columns: new[] { "EventId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarReminders");

            migrationBuilder.DropTable(
                name: "EventTimeSlots");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_TenantId_IsActive",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_Promotions_TenantId_StartDate_EndDate",
                table: "Promotions");

            migrationBuilder.DropIndex(
                name: "IX_PriceLists_TenantId_Priority",
                table: "PriceLists");

            migrationBuilder.DropIndex(
                name: "IX_DocumentRows_AppliedPromotionsJSON_NotNull",
                table: "DocumentRows");

            migrationBuilder.DropIndex(
                name: "IX_DocumentRows_IsPriceManual",
                table: "DocumentRows");

            migrationBuilder.DropIndex(
                name: "IX_DocumentRows_TenantId_ProductId",
                table: "DocumentRows");

            migrationBuilder.DropIndex(
                name: "IX_DocumentHeaders_TenantId_Date",
                table: "DocumentHeaders");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "StoreUsers");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "References");

            migrationBuilder.DropColumn(
                name: "MaxTotalDiscountPercentage",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "MaxUsesPerCustomer",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "BusinessParties");

        }
    }
}
