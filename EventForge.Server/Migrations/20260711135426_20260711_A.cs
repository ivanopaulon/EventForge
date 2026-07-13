using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260711_A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnoreTierMultiplier",
                table: "FidelityPointsCampaigns");

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "FidelityTierMultipliers",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FidelityTierMultipliers_CampaignId",
                table: "FidelityTierMultipliers",
                column: "CampaignId");

            migrationBuilder.AddForeignKey(
                name: "FK_FidelityTierMultipliers_FidelityPointsCampaigns_CampaignId",
                table: "FidelityTierMultipliers",
                column: "CampaignId",
                principalTable: "FidelityPointsCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FidelityTierMultipliers_FidelityPointsCampaigns_CampaignId",
                table: "FidelityTierMultipliers");

            migrationBuilder.DropIndex(
                name: "IX_FidelityTierMultipliers_CampaignId",
                table: "FidelityTierMultipliers");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "FidelityTierMultipliers");

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreTierMultiplier",
                table: "FidelityPointsCampaigns",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
