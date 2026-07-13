using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260409 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stations_Printers_AssignedPrinterId1",
                table: "Stations");

            migrationBuilder.DropForeignKey(
                name: "FK_StorePoses_Printers_DefaultFiscalPrinterId",
                table: "StorePoses");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId1",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Stations_AssignedPrinterId1",
                table: "Stations");

            migrationBuilder.DropColumn(
                name: "TenantId1",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AssignedPrinterId1",
                table: "Stations");

            migrationBuilder.RenameIndex(
                name: "IX_StorePoses_DefaultFiscalPrinterId",
                table: "StorePoses",
                newName: "IX_StorePos_DefaultFiscalPrinterId");

            migrationBuilder.AddColumn<Guid>(
                name: "CashierGroupId",
                table: "StorePoses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultPaymentTerminalId",
                table: "StorePoses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessPartyId",
                table: "ChatThreads",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalPhoneNumber",
                table: "ChatThreads",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnrecognizedNumber",
                table: "ChatThreads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WhatsAppLastStatus",
                table: "ChatThreads",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                table: "ChatMessages",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "MessageDirection",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhatsAppDeliveryStatus",
                table: "ChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppMessageId",
                table: "ChatMessages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FiscalDrawers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AssignmentType = table.Column<int>(type: "int", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    PosId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_FiscalDrawers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalDrawers_StorePoses_PosId",
                        column: x => x.PosId,
                        principalTable: "StorePoses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FiscalDrawers_StoreUsers_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "StoreUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NumeriBloccati",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroDiTelefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    BloccatoAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_NumeriBloccati", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTerminals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ConnectionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Port = table.Column<int>(type: "int", nullable: false),
                    AgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TimeoutMs = table.Column<int>(type: "int", nullable: false),
                    AmountConfirmationRequired = table.Column<bool>(type: "bit", nullable: false),
                    TerminalId = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
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
                    table.PrimaryKey("PK_PaymentTerminals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CashDenominations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalDrawerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 6, nullable: false),
                    DenominationType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CashDenominations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashDenominations_FiscalDrawers_FiscalDrawerId",
                        column: x => x.FiscalDrawerId,
                        principalTable: "FiscalDrawers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiscalDrawerSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalDrawerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    TotalCashIn = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    TotalCashOut = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    TotalDeposits = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    TotalWithdrawals = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    TransactionCount = table.Column<int>(type: "int", nullable: false),
                    OpenedByOperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ClosedByOperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_FiscalDrawerSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalDrawerSessions_FiscalDrawers_FiscalDrawerId",
                        column: x => x.FiscalDrawerId,
                        principalTable: "FiscalDrawers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiscalDrawerSessions_StoreUsers_ClosedByOperatorId",
                        column: x => x.ClosedByOperatorId,
                        principalTable: "StoreUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalDrawerSessions_StoreUsers_OpenedByOperatorId",
                        column: x => x.OpenedByOperatorId,
                        principalTable: "StoreUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FiscalDrawerTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalDrawerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalDrawerSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 6, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SaleSessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperatorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_FiscalDrawerTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalDrawerTransactions_FiscalDrawerSessions_FiscalDrawerSessionId",
                        column: x => x.FiscalDrawerSessionId,
                        principalTable: "FiscalDrawerSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalDrawerTransactions_FiscalDrawers_FiscalDrawerId",
                        column: x => x.FiscalDrawerId,
                        principalTable: "FiscalDrawers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorePos_CashierGroupId",
                table: "StorePoses",
                column: "CashierGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StorePos_DefaultPaymentTerminalId",
                table: "StorePoses",
                column: "DefaultPaymentTerminalId");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_AssignedPrinterId",
                table: "Stations",
                column: "AssignedPrinterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatThreads_BusinessPartyId",
                table: "ChatThreads",
                column: "BusinessPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatThreads_ExternalPhoneNumber",
                table: "ChatThreads",
                column: "ExternalPhoneNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_WhatsAppMessageId",
                table: "ChatMessages",
                column: "WhatsAppMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CashDenomination_FiscalDrawerId",
                table: "CashDenominations",
                column: "FiscalDrawerId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDrawer_OperatorId",
                table: "FiscalDrawers",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDrawer_PosId",
                table: "FiscalDrawers",
                column: "PosId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDrawerSession_FiscalDrawerId",
                table: "FiscalDrawerSessions",
                column: "FiscalDrawerId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDrawerSession_SessionDate",
                table: "FiscalDrawerSessions",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDrawerSessions_ClosedByOperatorId",
                table: "FiscalDrawerSessions",
                column: "ClosedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDrawerSessions_OpenedByOperatorId",
                table: "FiscalDrawerSessions",
                column: "OpenedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDrawerTransaction_FiscalDrawerId",
                table: "FiscalDrawerTransactions",
                column: "FiscalDrawerId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDrawerTransaction_TransactionAt",
                table: "FiscalDrawerTransactions",
                column: "TransactionAt");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalDrawerTransactions_FiscalDrawerSessionId",
                table: "FiscalDrawerTransactions",
                column: "FiscalDrawerSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_NumeriBloccati_NumeroDiTelefono",
                table: "NumeriBloccati",
                column: "NumeroDiTelefono");

            migrationBuilder.CreateIndex(
                name: "UX_NumeriBloccati_TenantId_Numero",
                table: "NumeriBloccati",
                columns: new[] { "TenantId", "NumeroDiTelefono" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatThreads_BusinessParties_BusinessPartyId",
                table: "ChatThreads",
                column: "BusinessPartyId",
                principalTable: "BusinessParties",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Stations_Printers_AssignedPrinterId",
                table: "Stations",
                column: "AssignedPrinterId",
                principalTable: "Printers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StorePoses_PaymentTerminals_DefaultPaymentTerminalId",
                table: "StorePoses",
                column: "DefaultPaymentTerminalId",
                principalTable: "PaymentTerminals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StorePoses_Printers_DefaultFiscalPrinterId",
                table: "StorePoses",
                column: "DefaultFiscalPrinterId",
                principalTable: "Printers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StorePoses_StoreUserGroups_CashierGroupId",
                table: "StorePoses",
                column: "CashierGroupId",
                principalTable: "StoreUserGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatThreads_BusinessParties_BusinessPartyId",
                table: "ChatThreads");

            migrationBuilder.DropForeignKey(
                name: "FK_Stations_Printers_AssignedPrinterId",
                table: "Stations");

            migrationBuilder.DropForeignKey(
                name: "FK_StorePoses_PaymentTerminals_DefaultPaymentTerminalId",
                table: "StorePoses");

            migrationBuilder.DropForeignKey(
                name: "FK_StorePoses_Printers_DefaultFiscalPrinterId",
                table: "StorePoses");

            migrationBuilder.DropForeignKey(
                name: "FK_StorePoses_StoreUserGroups_CashierGroupId",
                table: "StorePoses");

            migrationBuilder.DropTable(
                name: "CashDenominations");

            migrationBuilder.DropTable(
                name: "FiscalDrawerTransactions");

            migrationBuilder.DropTable(
                name: "NumeriBloccati");

            migrationBuilder.DropTable(
                name: "PaymentTerminals");

            migrationBuilder.DropTable(
                name: "FiscalDrawerSessions");

            migrationBuilder.DropTable(
                name: "FiscalDrawers");

            migrationBuilder.DropIndex(
                name: "IX_StorePos_CashierGroupId",
                table: "StorePoses");

            migrationBuilder.DropIndex(
                name: "IX_StorePos_DefaultPaymentTerminalId",
                table: "StorePoses");

            migrationBuilder.DropIndex(
                name: "IX_Stations_AssignedPrinterId",
                table: "Stations");

            migrationBuilder.DropIndex(
                name: "IX_ChatThreads_BusinessPartyId",
                table: "ChatThreads");

            migrationBuilder.DropIndex(
                name: "IX_ChatThreads_ExternalPhoneNumber",
                table: "ChatThreads");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_WhatsAppMessageId",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "CashierGroupId",
                table: "StorePoses");

            migrationBuilder.DropColumn(
                name: "DefaultPaymentTerminalId",
                table: "StorePoses");

            migrationBuilder.DropColumn(
                name: "BusinessPartyId",
                table: "ChatThreads");

            migrationBuilder.DropColumn(
                name: "ExternalPhoneNumber",
                table: "ChatThreads");

            migrationBuilder.DropColumn(
                name: "IsUnrecognizedNumber",
                table: "ChatThreads");

            migrationBuilder.DropColumn(
                name: "WhatsAppLastStatus",
                table: "ChatThreads");

            migrationBuilder.DropColumn(
                name: "MessageDirection",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "WhatsAppDeliveryStatus",
                table: "ChatMessages");

            migrationBuilder.DropColumn(
                name: "WhatsAppMessageId",
                table: "ChatMessages");

            migrationBuilder.RenameIndex(
                name: "IX_StorePos_DefaultFiscalPrinterId",
                table: "StorePoses",
                newName: "IX_StorePoses_DefaultFiscalPrinterId");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId1",
                table: "Users",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedPrinterId1",
                table: "Stations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SenderId",
                table: "ChatMessages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId1",
                table: "Users",
                column: "TenantId1");

            migrationBuilder.CreateIndex(
                name: "IX_Stations_AssignedPrinterId1",
                table: "Stations",
                column: "AssignedPrinterId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Stations_Printers_AssignedPrinterId1",
                table: "Stations",
                column: "AssignedPrinterId1",
                principalTable: "Printers",
                principalColumn: "Id");

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
    }
}
