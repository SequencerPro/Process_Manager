using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_DocumentControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "StepExecutions",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParallelGroup",
                table: "StepExecutions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovalProcessId",
                table: "Processes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChangeDescription",
                table: "Processes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveDate",
                table: "Processes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessRole",
                table: "Processes",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RevisionCode",
                table: "Processes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DocumentApprovalRequestId",
                table: "Jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessVersion = table.Column<int>(type: "integer", nullable: false),
                    ApprovalJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentApprovalRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentApprovalRequests_Jobs_ApprovalJobId",
                        column: x => x.ApprovalJobId,
                        principalTable: "Jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentApprovalRequests_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Processes_ApprovalProcessId",
                table: "Processes",
                column: "ApprovalProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalRequests_ApprovalJobId",
                table: "DocumentApprovalRequests",
                column: "ApprovalJobId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentApprovalRequests_ProcessId",
                table: "DocumentApprovalRequests",
                column: "ProcessId");

            migrationBuilder.AddForeignKey(
                name: "FK_Processes_Processes_ApprovalProcessId",
                table: "Processes",
                column: "ApprovalProcessId",
                principalTable: "Processes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Processes_Processes_ApprovalProcessId",
                table: "Processes");

            migrationBuilder.DropTable(
                name: "DocumentApprovalRequests");

            migrationBuilder.DropIndex(
                name: "IX_Processes_ApprovalProcessId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "StepExecutions");

            migrationBuilder.DropColumn(
                name: "ParallelGroup",
                table: "StepExecutions");

            migrationBuilder.DropColumn(
                name: "ApprovalProcessId",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "ChangeDescription",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "EffectiveDate",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "ProcessRole",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "RevisionCode",
                table: "Processes");

            migrationBuilder.DropColumn(
                name: "DocumentApprovalRequestId",
                table: "Jobs");
        }
    }
}
