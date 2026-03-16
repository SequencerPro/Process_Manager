using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkorders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorkorderId",
                table: "Jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Workorders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowVersion = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workorders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workorders_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkorderJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkorderId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkorderJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkorderJobs_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkorderJobs_WorkflowProcesses_WorkflowProcessId",
                        column: x => x.WorkflowProcessId,
                        principalTable: "WorkflowProcesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkorderJobs_Workorders_WorkorderId",
                        column: x => x.WorkorderId,
                        principalTable: "Workorders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_WorkorderId",
                table: "Jobs",
                column: "WorkorderId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkorderJobs_JobId",
                table: "WorkorderJobs",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkorderJobs_WorkflowProcessId",
                table: "WorkorderJobs",
                column: "WorkflowProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkorderJobs_WorkorderId_JobId",
                table: "WorkorderJobs",
                columns: new[] { "WorkorderId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkorderJobs_WorkorderId_WorkflowProcessId",
                table: "WorkorderJobs",
                columns: new[] { "WorkorderId", "WorkflowProcessId" });

            migrationBuilder.CreateIndex(
                name: "IX_Workorders_Code",
                table: "Workorders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workorders_WorkflowId",
                table: "Workorders",
                column: "WorkflowId");

            migrationBuilder.AddForeignKey(
                name: "FK_Jobs_Workorders_WorkorderId",
                table: "Jobs",
                column: "WorkorderId",
                principalTable: "Workorders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Jobs_Workorders_WorkorderId",
                table: "Jobs");

            migrationBuilder.DropTable(
                name: "WorkorderJobs");

            migrationBuilder.DropTable(
                name: "Workorders");

            migrationBuilder.DropIndex(
                name: "IX_Jobs_WorkorderId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "WorkorderId",
                table: "Jobs");
        }
    }
}
