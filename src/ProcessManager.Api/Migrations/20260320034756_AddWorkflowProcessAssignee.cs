using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowProcessAssignee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssigneeId",
                table: "WorkflowProcesses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowProcesses_AssigneeId",
                table: "WorkflowProcesses",
                column: "AssigneeId");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowProcesses_OrgUnits_AssigneeId",
                table: "WorkflowProcesses",
                column: "AssigneeId",
                principalTable: "OrgUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowProcesses_OrgUnits_AssigneeId",
                table: "WorkflowProcesses");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowProcesses_AssigneeId",
                table: "WorkflowProcesses");

            migrationBuilder.DropColumn(
                name: "AssigneeId",
                table: "WorkflowProcesses");
        }
    }
}
