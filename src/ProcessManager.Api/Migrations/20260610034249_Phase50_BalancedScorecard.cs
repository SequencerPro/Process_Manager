using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase50_BalancedScorecard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Scorecards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MissionStatement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VisionStatement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StrategyNotes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerOrgUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scorecards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scorecards_OrgUnits_OwnerOrgUnitId",
                        column: x => x.OwnerOrgUnitId,
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ScorecardPerspectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScorecardId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScorecardPerspectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScorecardPerspectives_Scorecards_ScorecardId",
                        column: x => x.ScorecardId,
                        principalTable: "Scorecards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StrategicObjectives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PerspectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OwnerOrgUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategicObjectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategicObjectives_OrgUnits_OwnerOrgUnitId",
                        column: x => x.OwnerOrgUnitId,
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StrategicObjectives_ScorecardPerspectives_PerspectiveId",
                        column: x => x.PerspectiveId,
                        principalTable: "ScorecardPerspectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveCauseLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScorecardId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveCauseLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveCauseLinks_Scorecards_ScorecardId",
                        column: x => x.ScorecardId,
                        principalTable: "Scorecards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObjectiveCauseLinks_StrategicObjectives_SourceObjectiveId",
                        column: x => x.SourceObjectiveId,
                        principalTable: "StrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObjectiveCauseLinks_StrategicObjectives_TargetObjectiveId",
                        column: x => x.TargetObjectiveId,
                        principalTable: "StrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveMeasures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Units = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BaselineValue = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    TargetValue = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    GreenThreshold = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    RedThreshold = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceParameter = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveMeasures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveMeasures_StrategicObjectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "StrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectiveProcessLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectiveId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: true),
                    WorkflowId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveProcessLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveProcessLinks_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObjectiveProcessLinks_StrategicObjectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "StrategicObjectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObjectiveProcessLinks_Workflows_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "Workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeasureSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MeasureId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CaptureSource = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeasureSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeasureSnapshots_ObjectiveMeasures_MeasureId",
                        column: x => x.MeasureId,
                        principalTable: "ObjectiveMeasures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MeasureSnapshots_MeasureId_CapturedAt",
                table: "MeasureSnapshots",
                columns: new[] { "MeasureId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveCauseLinks_ScorecardId",
                table: "ObjectiveCauseLinks",
                column: "ScorecardId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveCauseLinks_SourceObjectiveId_TargetObjectiveId",
                table: "ObjectiveCauseLinks",
                columns: new[] { "SourceObjectiveId", "TargetObjectiveId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveCauseLinks_TargetObjectiveId",
                table: "ObjectiveCauseLinks",
                column: "TargetObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveMeasures_ObjectiveId",
                table: "ObjectiveMeasures",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveProcessLinks_ObjectiveId",
                table: "ObjectiveProcessLinks",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveProcessLinks_ProcessId",
                table: "ObjectiveProcessLinks",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveProcessLinks_WorkflowId",
                table: "ObjectiveProcessLinks",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ScorecardPerspectives_ScorecardId",
                table: "ScorecardPerspectives",
                column: "ScorecardId");

            migrationBuilder.CreateIndex(
                name: "IX_Scorecards_OwnerOrgUnitId",
                table: "Scorecards",
                column: "OwnerOrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Scorecards_TenantId_Code",
                table: "Scorecards",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StrategicObjectives_OwnerOrgUnitId",
                table: "StrategicObjectives",
                column: "OwnerOrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicObjectives_PerspectiveId",
                table: "StrategicObjectives",
                column: "PerspectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategicObjectives_TenantId_Code",
                table: "StrategicObjectives",
                columns: new[] { "TenantId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeasureSnapshots");

            migrationBuilder.DropTable(
                name: "ObjectiveCauseLinks");

            migrationBuilder.DropTable(
                name: "ObjectiveProcessLinks");

            migrationBuilder.DropTable(
                name: "ObjectiveMeasures");

            migrationBuilder.DropTable(
                name: "StrategicObjectives");

            migrationBuilder.DropTable(
                name: "ScorecardPerspectives");

            migrationBuilder.DropTable(
                name: "Scorecards");
        }
    }
}
