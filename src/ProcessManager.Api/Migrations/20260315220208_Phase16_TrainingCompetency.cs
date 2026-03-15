using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase16_TrainingCompetency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompetencyExpiryDays",
                table: "Processes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompetencyTitle",
                table: "Processes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainingComplianceSummary",
                table: "ManagementReviews",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompetencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TrainingProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingProcessVersion = table.Column<int>(type: "integer", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    InstructorUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    InstructorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetencyRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyRecords_Jobs_JobId",
                        column: x => x.JobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CompetencyRecords_Processes_TrainingProcessId",
                        column: x => x.TrainingProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcessTrainingRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubjectEntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredTrainingProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnforced = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessTrainingRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessTrainingRequirements_Processes_RequiredTrainingProce~",
                        column: x => x.RequiredTrainingProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyRecords_JobId",
                table: "CompetencyRecords",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyRecords_Status",
                table: "CompetencyRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyRecords_TrainingProcessId",
                table: "CompetencyRecords",
                column: "TrainingProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyRecords_UserId",
                table: "CompetencyRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyRecords_UserId_TrainingProcessId_Status",
                table: "CompetencyRecords",
                columns: new[] { "UserId", "TrainingProcessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessTrainingRequirements_RequiredTrainingProcessId",
                table: "ProcessTrainingRequirements",
                column: "RequiredTrainingProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessTrainingRequirements_SubjectType_SubjectEntityId",
                table: "ProcessTrainingRequirements",
                columns: new[] { "SubjectType", "SubjectEntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetencyRecords");

            migrationBuilder.DropTable(
                name: "ProcessTrainingRequirements");

            migrationBuilder.DropColumn(
                name: "CompetencyExpiryDays",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "CompetencyTitle",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "TrainingComplianceSummary",
                table: "ManagementReviews");
        }
    }
}
