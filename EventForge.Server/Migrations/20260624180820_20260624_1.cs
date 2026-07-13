using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260624_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupplierProductPriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements");

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessPartyId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LineDiscountString",
                table: "DocumentRows",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AIOrderSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SystemPromptTemplate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    MaxItemsPerOrder = table.Column<int>(type: "int", nullable: false),
                    RequireConfirmation = table.Column<bool>(type: "bit", nullable: false),
                    AutoCreateDocument = table.Column<bool>(type: "bit", nullable: false),
                    WelcomeMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderConfirmationTemplate = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AmbiguousProductMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EnableAI = table.Column<bool>(type: "bit", nullable: false),
                    MaxTokensPerDay = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AIOrderSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIUsageLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModelUsed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    PromptTokens = table.Column<int>(type: "int", nullable: false),
                    CompletionTokens = table.Column<int>(type: "int", nullable: false),
                    EstimatedCostUsd = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    CallType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CallAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_AIUsageLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderConversationSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChatThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessPartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    State = table.Column<int>(type: "int", nullable: false),
                    DraftJson = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: true),
                    LastAiPromptAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDocumentHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AiRoundCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_OrderConversationSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderConversationSessions_ChatThreads_ChatThreadId",
                        column: x => x.ChatThreadId,
                        principalTable: "ChatThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_BusinessPartyId",
                table: "StockMovements",
                column: "BusinessPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_BusinessPartyId_MovementDate",
                table: "StockMovements",
                columns: new[] { "ProductId", "BusinessPartyId", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AIOrderSettings_TenantId",
                table: "AIOrderSettings",
                column: "TenantId",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_CallAt",
                table: "AIUsageLogs",
                column: "CallAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_TenantId",
                table: "AIUsageLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AIUsageLogs_TenantId_CallAt",
                table: "AIUsageLogs",
                columns: new[] { "TenantId", "CallAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderConversationSessions_ChatThreadId_TenantId",
                table: "OrderConversationSessions",
                columns: new[] { "ChatThreadId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderConversationSessions_TenantId",
                table: "OrderConversationSessions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_BusinessParties_BusinessPartyId",
                table: "StockMovements",
                column: "BusinessPartyId",
                principalTable: "BusinessParties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_BusinessParties_BusinessPartyId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "AIOrderSettings");

            migrationBuilder.DropTable(
                name: "AIUsageLogs");

            migrationBuilder.DropTable(
                name: "OrderConversationSessions");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_BusinessPartyId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductId_BusinessPartyId_MovementDate",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "BusinessPartyId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "LineDiscountString",
                table: "DocumentRows");

            migrationBuilder.CreateTable(
                name: "SupplierProductPriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductSupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangeSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NewLeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    NewUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OldLeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    OldUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PriceChange = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PriceChangePercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplierProductPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierProductPriceHistories_BusinessParties_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "BusinessParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierProductPriceHistories_ProductSuppliers_ProductSupplierId",
                        column: x => x.ProductSupplierId,
                        principalTable: "ProductSuppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SupplierProductPriceHistories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierProductPriceHistories_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistories_ChangedByUserId",
                table: "SupplierProductPriceHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistory_ChangedAt",
                table: "SupplierProductPriceHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistory_ProductId",
                table: "SupplierProductPriceHistories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistory_ProductId_ChangedAt",
                table: "SupplierProductPriceHistories",
                columns: new[] { "ProductId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistory_ProductSupplierId",
                table: "SupplierProductPriceHistories",
                column: "ProductSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistory_SupplierId",
                table: "SupplierProductPriceHistories",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierProductPriceHistory_SupplierId_ChangedAt",
                table: "SupplierProductPriceHistories",
                columns: new[] { "SupplierId", "ChangedAt" });
        }
    }
}
