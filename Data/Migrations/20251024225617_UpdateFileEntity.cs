using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lockhaven_backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFileEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GroupId",
                table: "Files",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClientEncrypted",
                table: "Files",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsShared",
                table: "Files",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "IsClientEncrypted",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "IsShared",
                table: "Files");
        }
    }
}
