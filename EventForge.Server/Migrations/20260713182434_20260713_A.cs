using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260713_A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FidelityTierMultipliers_CampaignId",
                table: "FidelityTierMultipliers");

            migrationBuilder.DropIndex(
                name: "IX_FidelityTierMultipliers_TenantId_CardType",
                table: "FidelityTierMultipliers");

            migrationBuilder.DropColumn(
                name: "CardType",
                table: "FidelityTierMultipliers");

            migrationBuilder.AddColumn<Guid>(
                name: "TierId",
                table: "FidelityTierMultipliers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "TierEnteredAt",
                table: "FidelityCards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TierId",
                table: "FidelityCards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FidelityTiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_FidelityTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FidelityTierRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinimumSpendThreshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EvaluationPeriodMonths = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_FidelityTierRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FidelityTierRules_FidelityTiers_TierId",
                        column: x => x.TierId,
                        principalTable: "FidelityTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FidelityTierMultipliers_CampaignId_TierId",
                table: "FidelityTierMultipliers",
                columns: new[] { "CampaignId", "TierId" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [CampaignId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FidelityTierMultipliers_TierId",
                table: "FidelityTierMultipliers",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_FidelityCards_TierId",
                table: "FidelityCards",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_FidelityTierRules_TenantId_TierId",
                table: "FidelityTierRules",
                columns: new[] { "TenantId", "TierId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FidelityTierRules_TierId",
                table: "FidelityTierRules",
                column: "TierId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FidelityTiers_TenantId_SortOrder",
                table: "FidelityTiers",
                columns: new[] { "TenantId", "SortOrder" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_FidelityCards_FidelityTiers_TierId",
                table: "FidelityCards",
                column: "TierId",
                principalTable: "FidelityTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FidelityTierMultipliers_FidelityTiers_TierId",
                table: "FidelityTierMultipliers",
                column: "TierId",
                principalTable: "FidelityTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FidelityCards_FidelityTiers_TierId",
                table: "FidelityCards");

            migrationBuilder.DropForeignKey(
                name: "FK_FidelityTierMultipliers_FidelityTiers_TierId",
                table: "FidelityTierMultipliers");

            migrationBuilder.DropTable(
                name: "FidelityTierRules");

            migrationBuilder.DropTable(
                name: "FidelityTiers");

            migrationBuilder.DropIndex(
                name: "IX_FidelityTierMultipliers_CampaignId_TierId",
                table: "FidelityTierMultipliers");

            migrationBuilder.DropIndex(
                name: "IX_FidelityTierMultipliers_TierId",
                table: "FidelityTierMultipliers");

            migrationBuilder.DropIndex(
                name: "IX_FidelityCards_TierId",
                table: "FidelityCards");

            migrationBuilder.DropColumn(
                name: "TierId",
                table: "FidelityTierMultipliers");

            migrationBuilder.DropColumn(
                name: "TierEnteredAt",
                table: "FidelityCards");

            migrationBuilder.DropColumn(
                name: "TierId",
                table: "FidelityCards");

            migrationBuilder.AddColumn<int>(
                name: "CardType",
                table: "FidelityTierMultipliers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FidelityTierMultipliers_CampaignId",
                table: "FidelityTierMultipliers",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_FidelityTierMultipliers_TenantId_CardType",
                table: "FidelityTierMultipliers",
                columns: new[] { "TenantId", "CardType" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
