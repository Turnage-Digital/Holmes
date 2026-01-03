using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holmes.IntakeSessions.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class InitialIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "consent_artifacts",
                columns: table => new
                {
                    ArtifactId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MimeType = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Length = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HashAlgorithm = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SchemaVersion = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    Payload = table.Column<byte[]>(type: "longblob", nullable: false),
                    MetadataJson = table.Column<string>(type: "json", nullable: false, defaultValue: "{}")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consent_artifacts", x => x.ArtifactId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "intake_sessions",
                columns: table => new
                {
                    IntakeSessionId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastTouchedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ResumeToken = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PolicySnapshotJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnswersSchemaVersion = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnswersPayloadHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnswersPayloadCipherText = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AnswersUpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ConsentArtifactId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsentMimeType = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsentLength = table.Column<long>(type: "bigint", nullable: true),
                    ConsentHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsentHashAlgorithm = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsentSchemaVersion = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConsentCapturedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CancellationReason = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupersededBySessionId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_intake_sessions", x => x.IntakeSessionId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "intake_sessions_projection",
                columns: table => new
                {
                    IntakeSessionId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PolicySnapshotId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PolicySnapshotSchemaVersion = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastTouchedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CancellationReason = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SupersededBySessionId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_intake_sessions_projection", x => x.IntakeSessionId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_consent_artifacts_OrderId",
                table: "consent_artifacts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_consent_artifacts_SubjectId",
                table: "consent_artifacts",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_intake_sessions_CustomerId",
                table: "intake_sessions",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_intake_sessions_OrderId",
                table: "intake_sessions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_intake_sessions_SubjectId",
                table: "intake_sessions",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_intake_sessions_projection_CustomerId",
                table: "intake_sessions_projection",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_intake_sessions_projection_OrderId",
                table: "intake_sessions_projection",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_intake_sessions_projection_Status",
                table: "intake_sessions_projection",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_intake_sessions_projection_SubjectId",
                table: "intake_sessions_projection",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "consent_artifacts");

            migrationBuilder.DropTable(
                name: "intake_sessions");

            migrationBuilder.DropTable(
                name: "intake_sessions_projection");
        }
    }
}
