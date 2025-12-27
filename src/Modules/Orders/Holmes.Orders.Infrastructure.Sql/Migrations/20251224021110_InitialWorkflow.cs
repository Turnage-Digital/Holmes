using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holmes.Orders.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "order_summary",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PolicySnapshotId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PackageCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastStatusReason = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ReadyForFulfillmentAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CanceledAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_summary", x => x.OrderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "order_timeline_events",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EventType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MetadataJson = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_timeline_events", x => x.EventId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "workflow_orders",
                columns: table => new
                {
                    OrderId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubjectId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PolicySnapshotId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PackageCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BlockedFromStatus = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastStatusReason = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    ActiveIntakeSessionId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastCompletedIntakeSessionId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InvitedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    IntakeStartedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    IntakeCompletedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ReadyForFulfillmentAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    CanceledAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workflow_orders", x => x.OrderId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_order_summary_CustomerId",
                table: "order_summary",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_order_summary_Status",
                table: "order_summary",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_order_timeline_events_OccurredAt",
                table: "order_timeline_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_order_timeline_events_OrderId",
                table: "order_timeline_events",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_orders_CustomerId",
                table: "workflow_orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_orders_Status",
                table: "workflow_orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_workflow_orders_SubjectId",
                table: "workflow_orders",
                column: "SubjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_summary");

            migrationBuilder.DropTable(
                name: "order_timeline_events");

            migrationBuilder.DropTable(
                name: "workflow_orders");
        }
    }
}
