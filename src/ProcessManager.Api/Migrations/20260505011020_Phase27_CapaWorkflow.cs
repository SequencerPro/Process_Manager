using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase27_CapaWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CapaRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProblemStatement = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ContainmentAction = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RootCauseAnalysisId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootCauseAnalysisType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    PermanentCorrectiveAction = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PreventiveAction = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    VerificationMethod = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    VerificationDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectivenessReviewDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectivenessVerifiedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    EffectivenessVerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    OwnerDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TeamMemberIds = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapaRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CapaSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CapaRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CompletedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CompletedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    AttachmentFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapaSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapaSteps_CapaRecords_CapaRecordId",
                        column: x => x.CapaRecordId,
                        principalTable: "CapaRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapaRecords_Code",
                table: "CapaRecords",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CapaRecords_OwnerUserId",
                table: "CapaRecords",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CapaRecords_Status",
                table: "CapaRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CapaSteps_CapaRecordId",
                table: "CapaSteps",
                column: "CapaRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapaSteps");

            migrationBuilder.DropTable(
                name: "CapaRecords");
        }
    }
}
