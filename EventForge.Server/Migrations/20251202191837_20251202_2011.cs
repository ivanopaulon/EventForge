using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20251202_2011 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AvatarDocumentId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InAppNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredLanguage",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "PushNotificationsEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AlertConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PriceIncreaseThresholdPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PriceDecreaseThresholdPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    VolatilityThresholdPercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    DaysWithoutUpdateThreshold = table.Column<int>(type: "int", nullable: false),
                    EnableEmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    EnableBrowserNotifications = table.Column<bool>(type: "bit", nullable: false),
                    AlertOnPriceIncrease = table.Column<bool>(type: "bit", nullable: false),
                    AlertOnPriceDecrease = table.Column<bool>(type: "bit", nullable: false),
                    AlertOnBetterSupplier = table.Column<bool>(type: "bit", nullable: false),
                    AlertOnVolatility = table.Column<bool>(type: "bit", nullable: false),
                    NotificationFrequency = table.Column<int>(type: "int", nullable: false),
                    LastDigestSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_AlertConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DashboardConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_DashboardConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardConfigurations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplierPriceAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AlertType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OldPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    NewPrice = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    PriceChangePercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PotentialSavings = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    AlertTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AlertMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RecommendedAction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BetterSupplierSuggestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_SupplierPriceAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierPriceAlerts_BusinessParties_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "BusinessParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierPriceAlerts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierProductPriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductSupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NewUnitCost = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PriceChange = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PriceChangePercentage = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OldLeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    NewLeadTimeDays = table.Column<int>(type: "int", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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

            migrationBuilder.CreateTable(
                name: "TransferOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Series = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ShipmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpectedArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ShippingReference = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_TransferOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferOrders_StorageFacilities_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalTable: "StorageFacilities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrders_StorageFacilities_SourceWarehouseId",
                        column: x => x.SourceWarehouseId,
                        principalTable: "StorageFacilities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DashboardMetricConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DashboardConfigurationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FilterCondition = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DashboardMetricConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardMetricConfigs_DashboardConfigurations_DashboardConfigurationId",
                        column: x => x.DashboardConfigurationId,
                        principalTable: "DashboardConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferOrderRows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransferOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DestinationLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuantityOrdered = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    QuantityShipped = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_TransferOrderRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferOrderRows_Lots_LotId",
                        column: x => x.LotId,
                        principalTable: "Lots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrderRows_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransferOrderRows_StorageLocations_DestinationLocationId",
                        column: x => x.DestinationLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TransferOrderRows_StorageLocations_SourceLocationId",
                        column: x => x.SourceLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TransferOrderRows_TransferOrders_TransferOrderId",
                        column: x => x.TransferOrderId,
                        principalTable: "TransferOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_AvatarDocumentId",
                table: "Users",
                column: "AvatarDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertConfiguration_TenantId",
                table: "AlertConfigurations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertConfiguration_TenantId_UserId",
                table: "AlertConfigurations",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DashboardConfigurations_UserId",
                table: "DashboardConfigurations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardMetricConfigs_DashboardConfigurationId",
                table: "DashboardMetricConfigs",
                column: "DashboardConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceAlert_CreatedAt",
                table: "SupplierPriceAlerts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceAlert_ProductId",
                table: "SupplierPriceAlerts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceAlert_Status",
                table: "SupplierPriceAlerts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceAlert_SupplierId",
                table: "SupplierPriceAlerts",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceAlert_TenantId",
                table: "SupplierPriceAlerts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceAlert_TenantId_Status",
                table: "SupplierPriceAlerts",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPriceAlert_TenantId_Status_CreatedAt",
                table: "SupplierPriceAlerts",
                columns: new[] { "TenantId", "Status", "CreatedAt" });

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

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderRows_DestinationLocationId",
                table: "TransferOrderRows",
                column: "DestinationLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderRows_LotId",
                table: "TransferOrderRows",
                column: "LotId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderRows_ProductId",
                table: "TransferOrderRows",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderRows_SourceLocationId",
                table: "TransferOrderRows",
                column: "SourceLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrderRows_TransferOrderId",
                table: "TransferOrderRows",
                column: "TransferOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrders_DestinationWarehouseId",
                table: "TransferOrders",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferOrders_SourceWarehouseId",
                table: "TransferOrders",
                column: "SourceWarehouseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_DocumentReferences_AvatarDocumentId",
                table: "Users",
                column: "AvatarDocumentId",
                principalTable: "DocumentReferences",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_DocumentReferences_AvatarDocumentId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "AlertConfigurations");

            migrationBuilder.DropTable(
                name: "DashboardMetricConfigs");

            migrationBuilder.DropTable(
                name: "SupplierPriceAlerts");

            migrationBuilder.DropTable(
                name: "SupplierProductPriceHistories");

            migrationBuilder.DropTable(
                name: "TransferOrderRows");

            migrationBuilder.DropTable(
                name: "DashboardConfigurations");

            migrationBuilder.DropTable(
                name: "TransferOrders");

            migrationBuilder.DropIndex(
                name: "IX_Users_AvatarDocumentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AvatarDocumentId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EmailNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InAppNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PushNotificationsEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Users");
        }
    }
}
