using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260401 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PriceLists_Events_EventId",
                table: "PriceLists");

            migrationBuilder.AddColumn<int>(
                name: "FiscalCode",
                table: "VatRates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomApplicationName",
                table: "Tenants",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomFaviconUrl",
                table: "Tenants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomLogoUrl",
                table: "Tenants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "SystemConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultFiscalPrinterId",
                table: "StorePoses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MergeReason",
                table: "SaleSessions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentSessionId",
                table: "SaleSessions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SplitPercentage",
                table: "SaleSessions",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SplitType",
                table: "SaleSessions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessPartyGroupIds",
                table: "PromotionRules",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BaudRate",
                table: "Printers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConnectionString",
                table: "Printers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFiscalPrinter",
                table: "Printers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "Printers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtocolType",
                table: "Printers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SerialPortName",
                table: "Printers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "EventId",
                table: "PriceLists",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "PriceLists",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Direction",
                table: "PriceLists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GenerationMetadata",
                table: "PriceLists",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGeneratedFromDocuments",
                table: "PriceLists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncedAt",
                table: "PriceLists",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSyncedBy",
                table: "PriceLists",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "PriceLists",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                table: "PriceListEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumOrderQuantity",
                table: "PriceListEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityIncrement",
                table: "PriceListEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierDescription",
                table: "PriceListEntries",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierProductCode",
                table: "PriceListEntries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UnitOfMeasureId",
                table: "PriceListEntries",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalCode",
                table: "PaymentMethods",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInventoryDocument",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "AppliedPriceListId",
                table: "DocumentRows",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPriceManual",
                table: "DocumentRows",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPriceFromPriceList",
                table: "DocumentRows",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceNotes",
                table: "DocumentRows",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockConnectionId",
                table: "DocumentHeaders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "DocumentHeaders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedBy",
                table: "DocumentHeaders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriceApplicationModeOverride",
                table: "DocumentHeaders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceListId",
                table: "DocumentHeaders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultPriceApplicationMode",
                table: "BusinessParties",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultPurchasePriceListId",
                table: "BusinessParties",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultSalesPriceListId",
                table: "BusinessParties",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ForcedPriceListId",
                table: "BusinessParties",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessPartyGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GroupType = table.Column<int>(type: "int", nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_BusinessPartyGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentHeaderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_DocumentStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentStatusHistories_DocumentHeaders_DocumentHeaderId",
                        column: x => x.DocumentHeaderId,
                        principalTable: "DocumentHeaders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JwtKeyHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KeyIdentifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EncryptedKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JwtKeyHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestsPerMinute = table.Column<double>(type: "float", nullable: false),
                    AvgResponseTimeMs = table.Column<double>(type: "float", nullable: false),
                    MemoryUsageMB = table.Column<long>(type: "bigint", nullable: false),
                    CpuUsagePercent = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_PerformanceLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceListBusinessParties",
                columns: table => new
                {
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessPartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    OverridePriority = table.Column<int>(type: "int", nullable: true),
                    SpecificValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SpecificValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GlobalDiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PriceListBusinessParties", x => new { x.PriceListId, x.BusinessPartyId });
                    table.ForeignKey(
                        name: "FK_PriceListBusinessParties_BusinessParties_BusinessPartyId",
                        column: x => x.BusinessPartyId,
                        principalTable: "BusinessParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PriceListBusinessParties_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SetupHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConfigurationSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_SetupHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemOperationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Operation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DurationMs = table.Column<double>(type: "float", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExecutedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemOperationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessPartyGroupMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessPartyGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessPartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberSince = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MemberUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OverridePriority = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_BusinessPartyGroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessPartyGroupMembers_BusinessParties_BusinessPartyId",
                        column: x => x.BusinessPartyId,
                        principalTable: "BusinessParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessPartyGroupMembers_BusinessPartyGroups_BusinessPartyGroupId",
                        column: x => x.BusinessPartyGroupId,
                        principalTable: "BusinessPartyGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId1",
                table: "Users",
                column: "TenantId1");

            migrationBuilder.CreateIndex(
                name: "IX_StorePoses_DefaultFiscalPrinterId",
                table: "StorePoses",
                column: "DefaultFiscalPrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleSessions_ParentSessionId",
                table: "SaleSessions",
                column: "ParentSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_Type_Direction",
                table: "PriceLists",
                columns: new[] { "Type", "Direction" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListEntries_UnitOfMeasureId",
                table: "PriceListEntries",
                column: "UnitOfMeasureId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentRows_AppliedPriceListId",
                table: "DocumentRows",
                column: "AppliedPriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentHeaders_PriceListId",
                table: "DocumentHeaders",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessParties_DefaultPurchasePriceListId",
                table: "BusinessParties",
                column: "DefaultPurchasePriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessParties_DefaultSalesPriceListId",
                table: "BusinessParties",
                column: "DefaultSalesPriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessParties_ForcedPriceListId",
                table: "BusinessParties",
                column: "ForcedPriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartyGroupMembers_BusinessPartyGroupId",
                table: "BusinessPartyGroupMembers",
                column: "BusinessPartyGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartyGroupMembers_BusinessPartyId",
                table: "BusinessPartyGroupMembers",
                column: "BusinessPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentStatusHistories_DocumentHeaderId",
                table: "DocumentStatusHistories",
                column: "DocumentHeaderId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListBusinessParties_BusinessPartyId_Status",
                table: "PriceListBusinessParties",
                columns: new[] { "BusinessPartyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListBusinessParties_IsPrimary",
                table: "PriceListBusinessParties",
                column: "IsPrimary");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListBusinessParties_Status",
                table: "PriceListBusinessParties",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessParties_PriceLists_DefaultPurchasePriceListId",
                table: "BusinessParties",
                column: "DefaultPurchasePriceListId",
                principalTable: "PriceLists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessParties_PriceLists_DefaultSalesPriceListId",
                table: "BusinessParties",
                column: "DefaultSalesPriceListId",
                principalTable: "PriceLists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessParties_PriceLists_ForcedPriceListId",
                table: "BusinessParties",
                column: "ForcedPriceListId",
                principalTable: "PriceLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentHeaders_PriceLists_PriceListId",
                table: "DocumentHeaders",
                column: "PriceListId",
                principalTable: "PriceLists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentRows_PriceLists_AppliedPriceListId",
                table: "DocumentRows",
                column: "AppliedPriceListId",
                principalTable: "PriceLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceListEntries_UMs_UnitOfMeasureId",
                table: "PriceListEntries",
                column: "UnitOfMeasureId",
                principalTable: "UMs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceLists_Events_EventId",
                table: "PriceLists",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SaleSessions_SaleSessions_ParentSessionId",
                table: "SaleSessions",
                column: "ParentSessionId",
                principalTable: "SaleSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StorePoses_Printers_DefaultFiscalPrinterId",
                table: "StorePoses",
                column: "DefaultFiscalPrinterId",
                principalTable: "Printers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId1",
                table: "Users",
                column: "TenantId1",
                principalTable: "Tenants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessParties_PriceLists_DefaultPurchasePriceListId",
                table: "BusinessParties");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessParties_PriceLists_DefaultSalesPriceListId",
                table: "BusinessParties");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessParties_PriceLists_ForcedPriceListId",
                table: "BusinessParties");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentHeaders_PriceLists_PriceListId",
                table: "DocumentHeaders");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentRows_PriceLists_AppliedPriceListId",
                table: "DocumentRows");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceListEntries_UMs_UnitOfMeasureId",
                table: "PriceListEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceLists_Events_EventId",
                table: "PriceLists");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleSessions_SaleSessions_ParentSessionId",
                table: "SaleSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_StorePoses_Printers_DefaultFiscalPrinterId",
                table: "StorePoses");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId1",
                table: "Users");

            migrationBuilder.DropTable(
                name: "BusinessPartyGroupMembers");

            migrationBuilder.DropTable(
                name: "DocumentStatusHistories");

            migrationBuilder.DropTable(
                name: "JwtKeyHistories");

            migrationBuilder.DropTable(
                name: "PerformanceLogs");

            migrationBuilder.DropTable(
                name: "PriceListBusinessParties");

            migrationBuilder.DropTable(
                name: "SetupHistories");

            migrationBuilder.DropTable(
                name: "SystemAlerts");

            migrationBuilder.DropTable(
                name: "SystemOperationLogs");

            migrationBuilder.DropTable(
                name: "BusinessPartyGroups");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_StorePoses_DefaultFiscalPrinterId",
                table: "StorePoses");

            migrationBuilder.DropIndex(
                name: "IX_SaleSessions_ParentSessionId",
                table: "SaleSessions");

            migrationBuilder.DropIndex(
                name: "IX_PriceLists_Type_Direction",
                table: "PriceLists");

            migrationBuilder.DropIndex(
                name: "IX_PriceListEntries_UnitOfMeasureId",
                table: "PriceListEntries");

            migrationBuilder.DropIndex(
                name: "IX_DocumentRows_AppliedPriceListId",
                table: "DocumentRows");

            migrationBuilder.DropIndex(
                name: "IX_DocumentHeaders_PriceListId",
                table: "DocumentHeaders");

            migrationBuilder.DropIndex(
                name: "IX_BusinessParties_DefaultPurchasePriceListId",
                table: "BusinessParties");

            migrationBuilder.DropIndex(
                name: "IX_BusinessParties_DefaultSalesPriceListId",
                table: "BusinessParties");

            migrationBuilder.DropIndex(
                name: "IX_BusinessParties_ForcedPriceListId",
                table: "BusinessParties");

            migrationBuilder.DropColumn(
                name: "FiscalCode",
                table: "VatRates");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CustomApplicationName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CustomFaviconUrl",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CustomLogoUrl",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "SystemConfigurations");

            migrationBuilder.DropColumn(
                name: "DefaultFiscalPrinterId",
                table: "StorePoses");

            migrationBuilder.DropColumn(
                name: "MergeReason",
                table: "SaleSessions");

            migrationBuilder.DropColumn(
                name: "ParentSessionId",
                table: "SaleSessions");

            migrationBuilder.DropColumn(
                name: "SplitPercentage",
                table: "SaleSessions");

            migrationBuilder.DropColumn(
                name: "SplitType",
                table: "SaleSessions");

            migrationBuilder.DropColumn(
                name: "BusinessPartyGroupIds",
                table: "PromotionRules");

            migrationBuilder.DropColumn(
                name: "BaudRate",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "ConnectionString",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "IsFiscalPrinter",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "ProtocolType",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "SerialPortName",
                table: "Printers");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "GenerationMetadata",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "IsGeneratedFromDocuments",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "LastSyncedAt",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "LastSyncedBy",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                table: "PriceListEntries");

            migrationBuilder.DropColumn(
                name: "MinimumOrderQuantity",
                table: "PriceListEntries");

            migrationBuilder.DropColumn(
                name: "QuantityIncrement",
                table: "PriceListEntries");

            migrationBuilder.DropColumn(
                name: "SupplierDescription",
                table: "PriceListEntries");

            migrationBuilder.DropColumn(
                name: "SupplierProductCode",
                table: "PriceListEntries");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasureId",
                table: "PriceListEntries");

            migrationBuilder.DropColumn(
                name: "FiscalCode",
                table: "PaymentMethods");

            migrationBuilder.DropColumn(
                name: "IsInventoryDocument",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "AppliedPriceListId",
                table: "DocumentRows");

            migrationBuilder.DropColumn(
                name: "IsPriceManual",
                table: "DocumentRows");

            migrationBuilder.DropColumn(
                name: "OriginalPriceFromPriceList",
                table: "DocumentRows");

            migrationBuilder.DropColumn(
                name: "PriceNotes",
                table: "DocumentRows");

            migrationBuilder.DropColumn(
                name: "LockConnectionId",
                table: "DocumentHeaders");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "DocumentHeaders");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "DocumentHeaders");

            migrationBuilder.DropColumn(
                name: "PriceApplicationModeOverride",
                table: "DocumentHeaders");

            migrationBuilder.DropColumn(
                name: "PriceListId",
                table: "DocumentHeaders");

            migrationBuilder.DropColumn(
                name: "DefaultPriceApplicationMode",
                table: "BusinessParties");

            migrationBuilder.DropColumn(
                name: "DefaultPurchasePriceListId",
                table: "BusinessParties");

            migrationBuilder.DropColumn(
                name: "DefaultSalesPriceListId",
                table: "BusinessParties");

            migrationBuilder.DropColumn(
                name: "ForcedPriceListId",
                table: "BusinessParties");

            migrationBuilder.AlterColumn<Guid>(
                name: "EventId",
                table: "PriceLists",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceLists_Events_EventId",
                table: "PriceLists",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
