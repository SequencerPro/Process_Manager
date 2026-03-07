using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateWorkflowProcesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowProcesses_WorkflowId_ProcessId",
                table: "WorkflowProcesses");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowProcesses_WorkflowId_ProcessId",
                table: "WorkflowProcesses",
                columns: new[] { "WorkflowId", "ProcessId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowProcesses_WorkflowId_ProcessId",
                table: "WorkflowProcesses");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowProcesses_WorkflowId_ProcessId",
                table: "WorkflowProcesses",
                columns: new[] { "WorkflowId", "ProcessId" },
                unique: true);
        }
    }
}
