using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase9_ChangeControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "StepTemplates",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Released");

            migrationBuilder.AddColumn<int>(
                name: "IntroducedInVersion",
                table: "StepTemplateContents",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentProcessId",
                table: "Processes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Processes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Released");

            migrationBuilder.AddColumn<bool>(
                name: "IsStale",
                table: "Pfmeas",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ProcessVersion",
                table: "Pfmeas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StalenessClearanceNotes",
                table: "Pfmeas",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StalenessClearedAt",
                table: "Pfmeas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StalenessClearedBy",
                table: "Pfmeas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessVersion",
                table: "Jobs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "ApprovalRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityVersion = table.Column<int>(type: "integer", nullable: false),
                    SubmittedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: true),
                    StepTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalRecords_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalRecords_StepTemplates_StepTemplateId",
                        column: x => x.StepTemplateId,
                        principalTable: "StepTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Processes_ParentProcessId",
                table: "Processes",
                column: "ParentProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_EntityType_EntityId",
                table: "ApprovalRecords",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_ProcessId",
                table: "ApprovalRecords",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRecords_StepTemplateId",
                table: "ApprovalRecords",
                column: "StepTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Processes_Processes_ParentProcessId",
                table: "Processes",
                column: "ParentProcessId",
                principalTable: "Processes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Processes_Processes_ParentProcessId",
                table: "Processes");

            migrationBuilder.DropTable(
                name: "ApprovalRecords");

            migrationBuilder.DropIndex(
                name: "IX_Processes_ParentProcessId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "StepTemplates");

            migrationBuilder.DropColumn(
                name: "IntroducedInVersion",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "ParentProcessId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "IsStale",
                table: "Pfmeas");

            migrationBuilder.DropColumn(
                name: "ProcessVersion",
                table: "Pfmeas");

            migrationBuilder.DropColumn(
                name: "StalenessClearanceNotes",
                table: "Pfmeas");

            migrationBuilder.DropColumn(
                name: "StalenessClearedAt",
                table: "Pfmeas");

            migrationBuilder.DropColumn(
                name: "StalenessClearedBy",
                table: "Pfmeas");

            migrationBuilder.DropColumn(
                name: "ProcessVersion",
                table: "Jobs");
        }
    }
}
