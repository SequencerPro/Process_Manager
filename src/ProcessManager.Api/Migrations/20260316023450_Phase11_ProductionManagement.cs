using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase11_ProductionManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpectedDurationMinutes",
                table: "StepTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RequiredEquipmentCategoryId",
                table: "StepTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EquipmentId",
                table: "StepExecutions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PlannedStartDate",
                table: "Jobs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EquipmentCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InstallDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipment_EquipmentCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "EquipmentCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DowntimeRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ResolvedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LinkedMaintenanceTaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DowntimeRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DowntimeRecords_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTriggers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TriggerType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IntervalDays = table.Column<int>(type: "integer", nullable: true),
                    IntervalUsageCycles = table.Column<int>(type: "integer", nullable: true),
                    LastTriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdvanceNoticeDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTriggers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceTriggers_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Type = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    Status = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LinkedDowntimeRecordId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceTasks_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceTasks_MaintenanceTriggers_TriggerId",
                        column: x => x.TriggerId,
                        principalTable: "MaintenanceTriggers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StepTemplates_RequiredEquipmentCategoryId",
                table: "StepTemplates",
                column: "RequiredEquipmentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_StepExecutions_EquipmentId",
                table: "StepExecutions",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DowntimeRecords_EquipmentId_StartedAt",
                table: "DowntimeRecords",
                columns: new[] { "EquipmentId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_CategoryId",
                table: "Equipment",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Code",
                table: "Equipment",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentCategories_Code",
                table: "EquipmentCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTasks_DueDate",
                table: "MaintenanceTasks",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTasks_EquipmentId_Status",
                table: "MaintenanceTasks",
                columns: new[] { "EquipmentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTasks_TriggerId",
                table: "MaintenanceTasks",
                column: "TriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceTriggers_EquipmentId",
                table: "MaintenanceTriggers",
                column: "EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StepExecutions_Equipment_EquipmentId",
                table: "StepExecutions",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StepTemplates_EquipmentCategories_RequiredEquipmentCategory~",
                table: "StepTemplates",
                column: "RequiredEquipmentCategoryId",
                principalTable: "EquipmentCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StepExecutions_Equipment_EquipmentId",
                table: "StepExecutions");

            migrationBuilder.DropForeignKey(
                name: "FK_StepTemplates_EquipmentCategories_RequiredEquipmentCategory~",
                table: "StepTemplates");

            migrationBuilder.DropTable(
                name: "DowntimeRecords");

            migrationBuilder.DropTable(
                name: "MaintenanceTasks");

            migrationBuilder.DropTable(
                name: "MaintenanceTriggers");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "EquipmentCategories");

            migrationBuilder.DropIndex(
                name: "IX_StepTemplates_RequiredEquipmentCategoryId",
                table: "StepTemplates");

            migrationBuilder.DropIndex(
                name: "IX_StepExecutions_EquipmentId",
                table: "StepExecutions");

            migrationBuilder.DropColumn(
                name: "ExpectedDurationMinutes",
                table: "StepTemplates");

            migrationBuilder.DropColumn(
                name: "RequiredEquipmentCategoryId",
                table: "StepTemplates");

            migrationBuilder.DropColumn(
                name: "EquipmentId",
                table: "StepExecutions");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "PlannedStartDate",
                table: "Jobs");
        }
    }
}
