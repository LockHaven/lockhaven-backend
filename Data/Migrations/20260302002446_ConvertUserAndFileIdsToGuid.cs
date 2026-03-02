using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lockhaven_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertUserAndFileIdsToGuid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_UserId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_OwnerUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_Users_UserId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_SecretAuditEvents_Users_UserId",
                table: "SecretAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_SecretVersions_Users_CreatedByUserId",
                table: "SecretVersions");

            migrationBuilder.Sql("""ALTER TABLE "Users" ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;""");
            migrationBuilder.Sql("""ALTER TABLE "Files" ALTER COLUMN "Id" TYPE uuid USING "Id"::uuid;""");
            migrationBuilder.Sql("""ALTER TABLE "Files" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;""");
            migrationBuilder.Sql("""ALTER TABLE "Projects" ALTER COLUMN "OwnerUserId" TYPE uuid USING "OwnerUserId"::uuid;""");
            migrationBuilder.Sql("""ALTER TABLE "ProjectMembers" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;""");
            migrationBuilder.Sql("""ALTER TABLE "SecretAuditEvents" ALTER COLUMN "UserId" TYPE uuid USING "UserId"::uuid;""");
            migrationBuilder.Sql("""ALTER TABLE "SecretVersions" ALTER COLUMN "CreatedByUserId" TYPE uuid USING "CreatedByUserId"::uuid;""");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Users_UserId",
                table: "Files",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_OwnerUserId",
                table: "Projects",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_Users_UserId",
                table: "ProjectMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SecretAuditEvents_Users_UserId",
                table: "SecretAuditEvents",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SecretVersions_Users_CreatedByUserId",
                table: "SecretVersions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_UserId",
                table: "Files");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Users_OwnerUserId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMembers_Users_UserId",
                table: "ProjectMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_SecretAuditEvents_Users_UserId",
                table: "SecretAuditEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_SecretVersions_Users_CreatedByUserId",
                table: "SecretVersions");

            migrationBuilder.Sql("""ALTER TABLE "SecretVersions" ALTER COLUMN "CreatedByUserId" TYPE text USING "CreatedByUserId"::text;""");
            migrationBuilder.Sql("""ALTER TABLE "SecretAuditEvents" ALTER COLUMN "UserId" TYPE text USING "UserId"::text;""");
            migrationBuilder.Sql("""ALTER TABLE "ProjectMembers" ALTER COLUMN "UserId" TYPE text USING "UserId"::text;""");
            migrationBuilder.Sql("""ALTER TABLE "Projects" ALTER COLUMN "OwnerUserId" TYPE text USING "OwnerUserId"::text;""");
            migrationBuilder.Sql("""ALTER TABLE "Files" ALTER COLUMN "UserId" TYPE text USING "UserId"::text;""");
            migrationBuilder.Sql("""ALTER TABLE "Files" ALTER COLUMN "Id" TYPE text USING "Id"::text;""");
            migrationBuilder.Sql("""ALTER TABLE "Users" ALTER COLUMN "Id" TYPE text USING "Id"::text;""");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Users_UserId",
                table: "Files",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Users_OwnerUserId",
                table: "Projects",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMembers_Users_UserId",
                table: "ProjectMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SecretAuditEvents_Users_UserId",
                table: "SecretAuditEvents",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SecretVersions_Users_CreatedByUserId",
                table: "SecretVersions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
