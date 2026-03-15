using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase7c_ControlPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControlPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessVersion = table.Column<int>(type: "integer", nullable: false),
                    IsStale = table.Column<bool>(type: "boolean", nullable: false),
                    StalenessClearedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StalenessClearedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StalenessClearanceNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlPlans_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ControlPlanEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ControlPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacteristicName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CharacteristicType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SpecificationOrTolerance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MeasurementTechnique = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    SampleSize = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SampleFrequency = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ControlMethod = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReactionPlan = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LinkedPfmeaFailureModeId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedPortId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlPlanEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ControlPlanEntries_ControlPlans_ControlPlanId",
                        column: x => x.ControlPlanId,
                        principalTable: "ControlPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ControlPlanEntries_PfmeaFailureModes_LinkedPfmeaFailureMode~",
                        column: x => x.LinkedPfmeaFailureModeId,
                        principalTable: "PfmeaFailureModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ControlPlanEntries_Ports_LinkedPortId",
                        column: x => x.LinkedPortId,
                        principalTable: "Ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ControlPlanEntries_ProcessSteps_ProcessStepId",
                        column: x => x.ProcessStepId,
                        principalTable: "ProcessSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ControlPlanEntries_ControlPlanId",
                table: "ControlPlanEntries",
                column: "ControlPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlPlanEntries_LinkedPfmeaFailureModeId",
                table: "ControlPlanEntries",
                column: "LinkedPfmeaFailureModeId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlPlanEntries_LinkedPortId",
                table: "ControlPlanEntries",
                column: "LinkedPortId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlPlanEntries_ProcessStepId",
                table: "ControlPlanEntries",
                column: "ProcessStepId");

            migrationBuilder.CreateIndex(
                name: "IX_ControlPlans_Code",
                table: "ControlPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ControlPlans_ProcessId",
                table: "ControlPlans",
                column: "ProcessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ControlPlanEntries");

            migrationBuilder.DropTable(
                name: "ControlPlans");
        }
    }
}
