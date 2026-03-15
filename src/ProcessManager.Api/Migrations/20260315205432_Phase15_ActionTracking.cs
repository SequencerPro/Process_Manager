using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase15_ActionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AssignedToUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AssignedToDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AssignedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AssignedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VerifiedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManagementReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReviewType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ConductedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NcSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ActionCloseRateSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MrbSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CustomerComplaintsNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SupplierQualityNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InternalAuditStatus = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PriorActionsSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Decisions = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NextCycleTargets = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManagementReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_AssignedToUserId",
                table: "ActionItems",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_DueDate",
                table: "ActionItems",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_SourceType_SourceEntityId",
                table: "ActionItems",
                columns: new[] { "SourceType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActionItems_Status",
                table: "ActionItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementReviews_ScheduledDate",
                table: "ManagementReviews",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_ManagementReviews_Status",
                table: "ManagementReviews",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActionItems");

            migrationBuilder.DropTable(
                name: "ManagementReviews");
        }
    }
}
