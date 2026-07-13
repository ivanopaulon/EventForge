using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260624 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StorePoses_DocumentReferences_ImageDocumentId",
                table: "StorePoses");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreUserGroups_DocumentReferences_LogoDocumentId",
                table: "StoreUserGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreUsers_DocumentReferences_PhotoDocumentId",
                table: "StoreUsers");

            migrationBuilder.RenameColumn(
                name: "ClosedAt",
                table: "DocumentHeaders",
                newName: "ArchivedAt");

            migrationBuilder.AddColumn<Guid>(
                name: "ImageDocumentId",
                table: "StoreUserPrivileges",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessPartyClassifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessPartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassificationNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_BusinessPartyClassifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessPartyClassifications_BusinessParties_BusinessPartyId",
                        column: x => x.BusinessPartyId,
                        principalTable: "BusinessParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BusinessPartyClassifications_ClassificationNodes_ClassificationNodeId",
                        column: x => x.ClassificationNodeId,
                        principalTable: "ClassificationNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FidelityCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CurrentPoints = table.Column<int>(type: "int", nullable: false),
                    TotalPointsEarned = table.Column<int>(type: "int", nullable: false),
                    TotalPointsRedeemed = table.Column<int>(type: "int", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HasPriorityAccess = table.Column<bool>(type: "bit", nullable: false),
                    HasBirthdayBonus = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BusinessPartyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_FidelityCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FidelityCards_BusinessParties_BusinessPartyId",
                        column: x => x.BusinessPartyId,
                        principalTable: "BusinessParties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FidelityPointsTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FidelityCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_FidelityPointsTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FidelityPointsTransactions_FidelityCards_FidelityCardId",
                        column: x => x.FidelityCardId,
                        principalTable: "FidelityCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoreUserPrivilege_ImageDocumentId",
                table: "StoreUserPrivileges",
                column: "ImageDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartyClassifications_BusinessPartyId",
                table: "BusinessPartyClassifications",
                column: "BusinessPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPartyClassifications_ClassificationNodeId",
                table: "BusinessPartyClassifications",
                column: "ClassificationNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FidelityCards_BusinessPartyId",
                table: "FidelityCards",
                column: "BusinessPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_FidelityCards_TenantId_CardNumber",
                table: "FidelityCards",
                columns: new[] { "TenantId", "CardNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FidelityCards_TenantId_Status",
                table: "FidelityCards",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FidelityPointsTransactions_FidelityCardId_TransactionDate",
                table: "FidelityPointsTransactions",
                columns: new[] { "FidelityCardId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FidelityPointsTransactions_TenantId",
                table: "FidelityPointsTransactions",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_StorePoses_DocumentReferences_ImageDocumentId",
                table: "StorePoses",
                column: "ImageDocumentId",
                principalTable: "DocumentReferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreUserGroups_DocumentReferences_LogoDocumentId",
                table: "StoreUserGroups",
                column: "LogoDocumentId",
                principalTable: "DocumentReferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreUserPrivileges_DocumentReferences_ImageDocumentId",
                table: "StoreUserPrivileges",
                column: "ImageDocumentId",
                principalTable: "DocumentReferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreUsers_DocumentReferences_PhotoDocumentId",
                table: "StoreUsers",
                column: "PhotoDocumentId",
                principalTable: "DocumentReferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StorePoses_DocumentReferences_ImageDocumentId",
                table: "StorePoses");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreUserGroups_DocumentReferences_LogoDocumentId",
                table: "StoreUserGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreUserPrivileges_DocumentReferences_ImageDocumentId",
                table: "StoreUserPrivileges");

            migrationBuilder.DropForeignKey(
                name: "FK_StoreUsers_DocumentReferences_PhotoDocumentId",
                table: "StoreUsers");

            migrationBuilder.DropTable(
                name: "BusinessPartyClassifications");

            migrationBuilder.DropTable(
                name: "FidelityPointsTransactions");

            migrationBuilder.DropTable(
                name: "FidelityCards");

            migrationBuilder.DropIndex(
                name: "IX_StoreUserPrivilege_ImageDocumentId",
                table: "StoreUserPrivileges");

            migrationBuilder.DropColumn(
                name: "ImageDocumentId",
                table: "StoreUserPrivileges");

            migrationBuilder.RenameColumn(
                name: "ArchivedAt",
                table: "DocumentHeaders",
                newName: "ClosedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_StorePoses_DocumentReferences_ImageDocumentId",
                table: "StorePoses",
                column: "ImageDocumentId",
                principalTable: "DocumentReferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreUserGroups_DocumentReferences_LogoDocumentId",
                table: "StoreUserGroups",
                column: "LogoDocumentId",
                principalTable: "DocumentReferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StoreUsers_DocumentReferences_PhotoDocumentId",
                table: "StoreUsers",
                column: "PhotoDocumentId",
                principalTable: "DocumentReferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
