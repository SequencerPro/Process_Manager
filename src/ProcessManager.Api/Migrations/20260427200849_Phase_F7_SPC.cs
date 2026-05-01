using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase_F7_SPC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpcCharts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentBlockId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ChartType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubgroupSize = table.Column<int>(type: "integer", nullable: false),
                    ControlLimitSource = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UCL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    LCL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    CL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    RangeUCL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    RangeLCL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    RangeCL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    TargetCpk = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    LSL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    USL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpcCharts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpcCharts_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpcDataPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpcChartId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    SubgroupIndex = table.Column<int>(type: "integer", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpcDataPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpcDataPoints_SpcCharts_SpcChartId",
                        column: x => x.SpcChartId,
                        principalTable: "SpcCharts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpcDataPoints_StepExecutions_StepExecutionId",
                        column: x => x.StepExecutionId,
                        principalTable: "StepExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpcCharts_ProcessId",
                table: "SpcCharts",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_SpcDataPoints_SpcChartId_SubgroupIndex",
                table: "SpcDataPoints",
                columns: new[] { "SpcChartId", "SubgroupIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_SpcDataPoints_StepExecutionId",
                table: "SpcDataPoints",
                column: "StepExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpcDataPoints");

            migrationBuilder.DropTable(
                name: "SpcCharts");
        }
    }
}
