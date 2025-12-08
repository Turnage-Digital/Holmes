using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holmes.Services.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class InitialServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_catalog_snapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(26)", fixedLength: true, maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<string>(type: "char(26)", fixedLength: true, maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ConfigJson = table.Column<string>(type: "json", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    CreatedBy = table.Column<string>(type: "char(26)", fixedLength: true, maxLength: 26, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_catalog_snapshots", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(26)", fixedLength: true, maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderId = table.Column<string>(type: "char(26)", fixedLength: true, maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<string>(type: "char(26)", fixedLength: true, maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CatalogSnapshotId = table.Column<string>(type: "char(26)", fixedLength: true, maxLength: 26, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServiceTypeCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    ScopeType = table.Column<int>(type: "int", nullable: true),
                    ScopeValue = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    VendorCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VendorReferenceId = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "varchar(1024)", maxLength: 1024, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    DispatchedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    FailedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true),
                    CanceledAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "service_results",
                columns: table => new
                {
                    Id = table.Column<string>(type: "char(26)", fixedLength: true, maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServiceRequestId = table.Column<string>(type: "char(26)", fixedLength: true, maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RecordsJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RawResponseHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VendorReferenceId = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReceivedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: false),
                    NormalizedAt = table.Column<DateTime>(type: "datetime(6)", precision: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_results_service_requests_ServiceRequestId",
                        column: x => x.ServiceRequestId,
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "idx_customer",
                table: "service_catalog_snapshots",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "idx_customer_version",
                table: "service_catalog_snapshots",
                columns: new[] { "CustomerId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_customer_status",
                table: "service_requests",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "idx_order",
                table: "service_requests",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "idx_order_tier",
                table: "service_requests",
                columns: new[] { "OrderId", "Tier" });

            migrationBuilder.CreateIndex(
                name: "idx_status_created",
                table: "service_requests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "idx_vendor_ref",
                table: "service_requests",
                columns: new[] { "VendorCode", "VendorReferenceId" });

            migrationBuilder.CreateIndex(
                name: "idx_service_request",
                table: "service_results",
                column: "ServiceRequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_catalog_snapshots");

            migrationBuilder.DropTable(
                name: "service_results");

            migrationBuilder.DropTable(
                name: "service_requests");
        }
    }
}
