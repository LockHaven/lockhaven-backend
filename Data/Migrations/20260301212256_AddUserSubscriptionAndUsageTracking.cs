using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lockhaven_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSubscriptionAndUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CurrentStorageUsedBytes",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadsCountDateUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UploadsTodayCount",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentStorageUsedBytes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UploadsCountDateUtc",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "UploadsTodayCount",
                table: "Users");
        }
    }
}
