using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase12e_WorkflowSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScheduleId",
                table: "Workorders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecurrenceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecurrenceInterval = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: true),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubjectTemplate = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NextRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSchedules_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Workorders_ScheduleId",
                table: "Workorders",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSchedules_IsActive_NextRunAt",
                table: "WorkflowSchedules",
                columns: new[] { "IsActive", "NextRunAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowSchedules_WorkflowId",
                table: "WorkflowSchedules",
                column: "WorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_Workorders_WorkflowSchedules_ScheduleId",
                table: "Workorders",
                column: "ScheduleId",
                principalTable: "WorkflowSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Workorders_WorkflowSchedules_ScheduleId",
                table: "Workorders");

            migrationBuilder.DropTable(
                name: "WorkflowSchedules");

            migrationBuilder.DropIndex(
                name: "IX_Workorders_ScheduleId",
                table: "Workorders");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "Workorders");
        }
    }
}
