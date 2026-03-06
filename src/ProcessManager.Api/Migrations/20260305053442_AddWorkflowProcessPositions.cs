using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowProcessPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRecords_StepTemplates_StepTemplateId",
                table: "ApprovalRecords");

            migrationBuilder.AddColumn<double>(
                name: "PositionX",
                table: "WorkflowProcesses",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PositionY",
                table: "WorkflowProcesses",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "PowerBiDashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmbedUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PowerBiDashboards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PowerBiDashboards_Name",
                table: "PowerBiDashboards",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRecords_StepTemplates_StepTemplateId",
                table: "ApprovalRecords",
                column: "StepTemplateId",
                principalTable: "StepTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalRecords_StepTemplates_StepTemplateId",
                table: "ApprovalRecords");

            migrationBuilder.DropTable(
                name: "PowerBiDashboards");

            migrationBuilder.DropColumn(
                name: "PositionX",
                table: "WorkflowProcesses");

            migrationBuilder.DropColumn(
                name: "PositionY",
                table: "WorkflowProcesses");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalRecords_StepTemplates_StepTemplateId",
                table: "ApprovalRecords",
                column: "StepTemplateId",
                principalTable: "StepTemplates",
                principalColumn: "Id");
        }
    }
}
