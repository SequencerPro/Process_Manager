using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase12f_WorkflowJobExecutionRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkorderJobs_Jobs_JobId",
                table: "WorkorderJobs");

            migrationBuilder.DropIndex(
                name: "IX_WorkorderJobs_WorkorderId_JobId",
                table: "WorkorderJobs");

            migrationBuilder.DropIndex(
                name: "IX_WorkorderJobs_WorkorderId_WorkflowProcessId",
                table: "WorkorderJobs");

            migrationBuilder.AlterColumn<Guid>(
                name: "JobId",
                table: "WorkorderJobs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "NodeStatus",
                table: "WorkorderJobs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_WorkorderJobs_WorkorderId_JobId",
                table: "WorkorderJobs",
                columns: new[] { "WorkorderId", "JobId" },
                unique: true,
                filter: "\"JobId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkorderJobs_WorkorderId_WorkflowProcessId",
                table: "WorkorderJobs",
                columns: new[] { "WorkorderId", "WorkflowProcessId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkorderJobs_Jobs_JobId",
                table: "WorkorderJobs",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkorderJobs_Jobs_JobId",
                table: "WorkorderJobs");

            migrationBuilder.DropIndex(
                name: "IX_WorkorderJobs_WorkorderId_JobId",
                table: "WorkorderJobs");

            migrationBuilder.DropIndex(
                name: "IX_WorkorderJobs_WorkorderId_WorkflowProcessId",
                table: "WorkorderJobs");

            migrationBuilder.DropColumn(
                name: "NodeStatus",
                table: "WorkorderJobs");

            migrationBuilder.AlterColumn<Guid>(
                name: "JobId",
                table: "WorkorderJobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkorderJobs_WorkorderId_JobId",
                table: "WorkorderJobs",
                columns: new[] { "WorkorderId", "JobId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkorderJobs_WorkorderId_WorkflowProcessId",
                table: "WorkorderJobs",
                columns: new[] { "WorkorderId", "WorkflowProcessId" });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkorderJobs_Jobs_JobId",
                table: "WorkorderJobs",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
