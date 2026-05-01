using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase17_StandardsConformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CouponCode",
                table: "TenantSubscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Standard = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LeadAuditor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditPrograms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanChangeLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPlan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ToPlan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanChangeLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StandardsClauses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Standard = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ClauseNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequirementSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsAs9100Addition = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandardsClauses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Audits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Scope = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActualDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LeadAuditor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Audits_AuditPrograms_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "AuditPrograms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClauseEvidenceLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClauseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsAutoLinked = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClauseEvidenceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClauseEvidenceLinks_StandardsClauses_ClauseId",
                        column: x => x.ClauseId,
                        principalTable: "StandardsClauses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditFindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuditId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClauseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FindingType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ObjectiveEvidence = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ActionItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosureNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditFindings_ActionItems_ActionItemId",
                        column: x => x.ActionItemId,
                        principalTable: "ActionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditFindings_Audits_AuditId",
                        column: x => x.AuditId,
                        principalTable: "Audits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditFindings_StandardsClauses_ClauseId",
                        column: x => x.ClauseId,
                        principalTable: "StandardsClauses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_ActionItemId",
                table: "AuditFindings",
                column: "ActionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_AuditId",
                table: "AuditFindings",
                column: "AuditId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_ClauseId",
                table: "AuditFindings",
                column: "ClauseId");

            migrationBuilder.CreateIndex(
                name: "IX_Audits_ProgramId",
                table: "Audits",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_ClauseEvidenceLinks_ClauseId_EntityType_EntityId",
                table: "ClauseEvidenceLinks",
                columns: new[] { "ClauseId", "EntityType", "EntityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanChangeLogs_TenantId",
                table: "PlanChangeLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StandardsClauses_Standard_ClauseNumber",
                table: "StandardsClauses",
                columns: new[] { "Standard", "ClauseNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditFindings");

            migrationBuilder.DropTable(
                name: "ClauseEvidenceLinks");

            migrationBuilder.DropTable(
                name: "PlanChangeLogs");

            migrationBuilder.DropTable(
                name: "Audits");

            migrationBuilder.DropTable(
                name: "StandardsClauses");

            migrationBuilder.DropTable(
                name: "AuditPrograms");

            migrationBuilder.DropColumn(
                name: "CouponCode",
                table: "TenantSubscriptions");
        }
    }
}
