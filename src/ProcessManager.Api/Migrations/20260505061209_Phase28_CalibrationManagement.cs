using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase28_CalibrationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalibrationRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CalibrationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CalibrationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CertificateNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CertificateFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Result = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StandardsUsed = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TemperatureHumidity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AsFoundReading = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AsLeftReading = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Uncertainty = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationRecords_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalibrationSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntervalDays = table.Column<int>(type: "integer", nullable: false),
                    IntervalAdjustmentMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ConsecutivePassCount = table.Column<int>(type: "integer", nullable: false),
                    MaxIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    MinIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    ExtensionPercent = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalibrationSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalibrationSchedules_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationRecords_EquipmentId",
                table: "CalibrationRecords",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationRecords_NextDueDate",
                table: "CalibrationRecords",
                column: "NextDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_CalibrationSchedules_EquipmentId",
                table: "CalibrationSchedules",
                column: "EquipmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalibrationRecords");

            migrationBuilder.DropTable(
                name: "CalibrationSchedules");
        }
    }
}
