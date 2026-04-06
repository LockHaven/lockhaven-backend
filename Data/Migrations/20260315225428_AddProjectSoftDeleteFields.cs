using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lockhaven_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectSoftDeleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerUserId_Name",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerUserId_Slug",
                table: "Projects");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Projects",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId_Name",
                table: "Projects",
                columns: new[] { "OwnerUserId", "Name" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId_Slug",
                table: "Projects",
                columns: new[] { "OwnerUserId", "Slug" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerUserId_Name",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwnerUserId_Slug",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Projects");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId_Name",
                table: "Projects",
                columns: new[] { "OwnerUserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId_Slug",
                table: "Projects",
                columns: new[] { "OwnerUserId", "Slug" },
                unique: true);
        }
    }
}
