using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase32_ChangeOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GageStudies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StudyType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: true),
                    CharacteristicName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Tolerance = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    LSL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    USL = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    NumberOfParts = table.Column<int>(type: "integer", nullable: false),
                    NumberOfOperators = table.Column<int>(type: "integer", nullable: false),
                    NumberOfTrials = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GrrPercent = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Ndc = table.Column<int>(type: "integer", nullable: true),
                    AcceptanceDecision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GageStudies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GageStudies_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GageStudies_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GageStudyMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GageStudyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<int>(type: "integer", nullable: false),
                    OperatorId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TrialNumber = table.Column<int>(type: "integer", nullable: false),
                    MeasuredValue = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GageStudyMeasurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GageStudyMeasurements_GageStudies_GageStudyId",
                        column: x => x.GageStudyId,
                        principalTable: "GageStudies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GageStudies_EquipmentId",
                table: "GageStudies",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_GageStudies_ProcessId",
                table: "GageStudies",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_GageStudies_Status",
                table: "GageStudies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GageStudyMeasurements_GageStudyId_PartNumber_OperatorId_Tri~",
                table: "GageStudyMeasurements",
                columns: new[] { "GageStudyId", "PartNumber", "OperatorId", "TrialNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GageStudyMeasurements");

            migrationBuilder.DropTable(
                name: "GageStudies");
        }
    }
}
