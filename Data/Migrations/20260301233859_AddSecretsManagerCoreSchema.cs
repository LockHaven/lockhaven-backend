using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace lockhaven_backend.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSecretsManagerCoreSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSecretCount",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecretsUpdatedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Environments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Environments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMembers",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMembers", x => new { x.ProjectId, x.UserId });
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Secrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastRotatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Secrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Secrets_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecretAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    SecretId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    Ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecretAuditEvents_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecretAuditEvents_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecretAuditEvents_Secrets_SecretId",
                        column: x => x.SecretId,
                        principalTable: "Secrets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecretAuditEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SecretVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecretId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    EncryptedPayload = table.Column<string>(type: "text", nullable: false),
                    EncryptedDek = table.Column<string>(type: "text", nullable: false),
                    Iv = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "text", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecretVersions_Secrets_SecretId",
                        column: x => x.SecretId,
                        principalTable: "Secrets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecretVersions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Environments_ProjectId_Slug",
                table: "Environments",
                columns: new[] { "ProjectId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMembers_UserId",
                table: "ProjectMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId_Name",
                table: "Projects",
                columns: new[] { "OwnerUserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwnerUserId_Slug",
                table: "Projects",
                columns: new[] { "OwnerUserId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecretAuditEvents_EnvironmentId",
                table: "SecretAuditEvents",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretAuditEvents_ProjectId_OccurredAtUtc",
                table: "SecretAuditEvents",
                columns: new[] { "ProjectId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecretAuditEvents_SecretId_OccurredAtUtc",
                table: "SecretAuditEvents",
                columns: new[] { "SecretId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecretAuditEvents_UserId",
                table: "SecretAuditEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_EnvironmentId",
                table: "Secrets",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Secrets_EnvironmentId_Key",
                table: "Secrets",
                columns: new[] { "EnvironmentId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecretVersions_CreatedByUserId",
                table: "SecretVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretVersions_SecretId",
                table: "SecretVersions",
                column: "SecretId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretVersions_SecretId_Version",
                table: "SecretVersions",
                columns: new[] { "SecretId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectMembers");

            migrationBuilder.DropTable(
                name: "SecretAuditEvents");

            migrationBuilder.DropTable(
                name: "SecretVersions");

            migrationBuilder.DropTable(
                name: "Secrets");

            migrationBuilder.DropTable(
                name: "Environments");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropColumn(
                name: "CurrentSecretCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SecretsUpdatedAtUtc",
                table: "Users");
        }
    }
}
