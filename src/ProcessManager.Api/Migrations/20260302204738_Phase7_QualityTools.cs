using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_QualityTools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CeMatrices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CeMatrices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CeMatrices_ProcessSteps_ProcessStepId",
                        column: x => x.ProcessStepId,
                        principalTable: "ProcessSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pfmeas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pfmeas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pfmeas_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CeInputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CeMatrixId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PortId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CeInputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CeInputs_CeMatrices_CeMatrixId",
                        column: x => x.CeMatrixId,
                        principalTable: "CeMatrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CeInputs_Ports_PortId",
                        column: x => x.PortId,
                        principalTable: "Ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CeOutputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CeMatrixId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PortId = table.Column<Guid>(type: "uuid", nullable: true),
                    Importance = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CeOutputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CeOutputs_CeMatrices_CeMatrixId",
                        column: x => x.CeMatrixId,
                        principalTable: "CeMatrices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CeOutputs_Ports_PortId",
                        column: x => x.PortId,
                        principalTable: "Ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PfmeaFailureModes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PfmeaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepFunction = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FailureMode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FailureEffect = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FailureCause = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PreventionControls = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DetectionControls = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    Occurrence = table.Column<int>(type: "integer", nullable: false),
                    Detection = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PfmeaFailureModes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PfmeaFailureModes_Pfmeas_PfmeaId",
                        column: x => x.PfmeaId,
                        principalTable: "Pfmeas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PfmeaFailureModes_ProcessSteps_ProcessStepId",
                        column: x => x.ProcessStepId,
                        principalTable: "ProcessSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CeCorrelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CeInputId = table.Column<Guid>(type: "uuid", nullable: false),
                    CeOutputId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    CeMatrixId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CeCorrelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CeCorrelations_CeInputs_CeInputId",
                        column: x => x.CeInputId,
                        principalTable: "CeInputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CeCorrelations_CeMatrices_CeMatrixId",
                        column: x => x.CeMatrixId,
                        principalTable: "CeMatrices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CeCorrelations_CeOutputs_CeOutputId",
                        column: x => x.CeOutputId,
                        principalTable: "CeOutputs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PfmeaActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FailureModeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ResponsiblePerson = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TargetDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletionNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RevisedOccurrence = table.Column<int>(type: "integer", nullable: true),
                    RevisedDetection = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PfmeaActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PfmeaActions_PfmeaFailureModes_FailureModeId",
                        column: x => x.FailureModeId,
                        principalTable: "PfmeaFailureModes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CeCorrelations_CeInputId_CeOutputId",
                table: "CeCorrelations",
                columns: new[] { "CeInputId", "CeOutputId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CeCorrelations_CeMatrixId",
                table: "CeCorrelations",
                column: "CeMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_CeCorrelations_CeOutputId",
                table: "CeCorrelations",
                column: "CeOutputId");

            migrationBuilder.CreateIndex(
                name: "IX_CeInputs_CeMatrixId",
                table: "CeInputs",
                column: "CeMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_CeInputs_PortId",
                table: "CeInputs",
                column: "PortId");

            migrationBuilder.CreateIndex(
                name: "IX_CeMatrices_ProcessStepId",
                table: "CeMatrices",
                column: "ProcessStepId");

            migrationBuilder.CreateIndex(
                name: "IX_CeOutputs_CeMatrixId",
                table: "CeOutputs",
                column: "CeMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_CeOutputs_PortId",
                table: "CeOutputs",
                column: "PortId");

            migrationBuilder.CreateIndex(
                name: "IX_PfmeaActions_FailureModeId",
                table: "PfmeaActions",
                column: "FailureModeId");

            migrationBuilder.CreateIndex(
                name: "IX_PfmeaFailureModes_PfmeaId",
                table: "PfmeaFailureModes",
                column: "PfmeaId");

            migrationBuilder.CreateIndex(
                name: "IX_PfmeaFailureModes_ProcessStepId",
                table: "PfmeaFailureModes",
                column: "ProcessStepId");

            migrationBuilder.CreateIndex(
                name: "IX_Pfmeas_Code",
                table: "Pfmeas",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pfmeas_ProcessId",
                table: "Pfmeas",
                column: "ProcessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CeCorrelations");

            migrationBuilder.DropTable(
                name: "PfmeaActions");

            migrationBuilder.DropTable(
                name: "CeInputs");

            migrationBuilder.DropTable(
                name: "CeOutputs");

            migrationBuilder.DropTable(
                name: "PfmeaFailureModes");

            migrationBuilder.DropTable(
                name: "CeMatrices");

            migrationBuilder.DropTable(
                name: "Pfmeas");
        }
    }
}
