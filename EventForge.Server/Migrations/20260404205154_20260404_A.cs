using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventForge.Server.Migrations
{
    /// <inheritdoc />
    public partial class _20260404_A : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "LogEntries",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MachineName",
                table: "LogEntries",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "LogEntries",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "OldValue",
                table: "EntityChangeLogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewValue",
                table: "EntityChangeLogs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "EntityChangeLogs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWSEQUENTIALID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "EntityChangeLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EntityChangeLogs_EntityName_TenantId_ChangedAt",
                table: "EntityChangeLogs",
                columns: new[] { "EntityName", "TenantId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityChangeLogs_TenantId_ChangedAt",
                table: "EntityChangeLogs",
                columns: new[] { "TenantId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityChangeLogs_TenantId_EntityId_ChangedAt",
                table: "EntityChangeLogs",
                columns: new[] { "TenantId", "EntityId", "ChangedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EntityChangeLogs_EntityName_TenantId_ChangedAt",
                table: "EntityChangeLogs");

            migrationBuilder.DropIndex(
                name: "IX_EntityChangeLogs_TenantId_ChangedAt",
                table: "EntityChangeLogs");

            migrationBuilder.DropIndex(
                name: "IX_EntityChangeLogs_TenantId_EntityId_ChangedAt",
                table: "EntityChangeLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "EntityChangeLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserName",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MachineName",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Level",
                table: "LogEntries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "OldValue",
                table: "EntityChangeLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NewValue",
                table: "EntityChangeLogs",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "EntityChangeLogs",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValueSql: "NEWSEQUENTIALID()");
        }
    }
}
