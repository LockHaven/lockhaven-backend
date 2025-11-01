using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lockhaven_backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEncryptionFormatVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EncryptionFormatVersion",
                table: "Files",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptionFormatVersion",
                table: "Files");
        }
    }
}
