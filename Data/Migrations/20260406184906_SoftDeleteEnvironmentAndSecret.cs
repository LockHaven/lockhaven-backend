using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lockhaven_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteEnvironmentAndSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Secrets_Environments_EnvironmentId",
                table: "Secrets");

            migrationBuilder.DropIndex(
                name: "IX_Secrets_EnvironmentId_Key",
                table: "Secrets");

            migrationBuilder.DropIndex(
                name: "IX_Environments_ProjectId_Slug",
                table: "Environments");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Secrets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Secrets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Environments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Environments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_EnvironmentId_Key",
                table: "Secrets",
                columns: new[] { "EnvironmentId", "Key" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Environments_ProjectId_Slug",
                table: "Environments",
                columns: new[] { "ProjectId", "Slug" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_Secrets_Environments_EnvironmentId",
                table: "Secrets",
                column: "EnvironmentId",
                principalTable: "Environments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Secrets_Environments_EnvironmentId",
                table: "Secrets");

            migrationBuilder.DropIndex(
                name: "IX_Secrets_EnvironmentId_Key",
                table: "Secrets");

            migrationBuilder.DropIndex(
                name: "IX_Environments_ProjectId_Slug",
                table: "Environments");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Secrets");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Secrets");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Environments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Environments");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_EnvironmentId_Key",
                table: "Secrets",
                columns: new[] { "EnvironmentId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Environments_ProjectId_Slug",
                table: "Environments",
                columns: new[] { "ProjectId", "Slug" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Secrets_Environments_EnvironmentId",
                table: "Secrets",
                column: "EnvironmentId",
                principalTable: "Environments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
