using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lockhaven_backend.Migrations
{
    /// <inheritdoc />
    public partial class Remove_EncryptionFormatVersion_From_File : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptionFormatVersion",
                table: "Files");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EncryptionFormatVersion",
                table: "Files",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }
    }
}
