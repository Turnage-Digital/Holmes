using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holmes.SlaClocks.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class InitialSlaClocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsObserved = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holidays", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sla_clocks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<string>(type: "varchar(26)", maxLength: 26, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DeadlineAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtRiskThresholdAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AtRiskAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    BreachedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PauseReason = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccumulatedPauseMs = table.Column<long>(type: "bigint", nullable: false),
                    TargetBusinessDays = table.Column<int>(type: "int", nullable: false),
                    AtRiskThresholdPercent = table.Column<decimal>(type: "decimal(3,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sla_clocks", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_holidays_CustomerId_Date",
                table: "holidays",
                columns: new[] { "CustomerId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_sla_clocks_CustomerId_State",
                table: "sla_clocks",
                columns: new[] { "CustomerId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_sla_clocks_OrderId",
                table: "sla_clocks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_sla_clocks_State_AtRiskThresholdAt",
                table: "sla_clocks",
                columns: new[] { "State", "AtRiskThresholdAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sla_clocks_State_DeadlineAt",
                table: "sla_clocks",
                columns: new[] { "State", "DeadlineAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holidays");

            migrationBuilder.DropTable(
                name: "sla_clocks");
        }
    }
}
