using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260507_B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductBundleItems_BundleProductId",
                table: "ProductBundleItems");

            migrationBuilder.AddColumn<bool>(
                name: "IsReconciliationAdjustment",
                table: "StockMovements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ReconciliationRunId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_ProductBundleItem_BundleProduct_ComponentProduct",
                table: "ProductBundleItems",
                columns: new[] { "BundleProductId", "ComponentProductId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ProductBundleItem_BundleProduct_ComponentProduct",
                table: "ProductBundleItems");

            migrationBuilder.DropColumn(
                name: "IsReconciliationAdjustment",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ReconciliationRunId",
                table: "StockMovements");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBundleItems_BundleProductId",
                table: "ProductBundleItems",
                column: "BundleProductId");
        }
    }
}
