using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lockhaven_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class SlugUrlSafeCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_Projects_Slug_UrlSafe",
                table: "Projects",
                sql: "\"Slug\" ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Environments_Slug_UrlSafe",
                table: "Environments",
                sql: "\"Slug\" ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Projects_Slug_UrlSafe",
                table: "Projects");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Environments_Slug_UrlSafe",
                table: "Environments");
        }
    }
}
