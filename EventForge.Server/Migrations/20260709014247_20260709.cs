using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260709 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppliedPromotionsJSON",
                table: "SaleItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ManualDiscountPercent",
                table: "SaleItems",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "PriceListId",
                table: "SaleItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriceListName",
                table: "SaleItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PromotionDiscountPercent",
                table: "SaleItems",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppliedPromotionsJSON",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "ManualDiscountPercent",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "PriceListId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "PriceListName",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "PromotionDiscountPercent",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Promotions");
        }
    }
}
