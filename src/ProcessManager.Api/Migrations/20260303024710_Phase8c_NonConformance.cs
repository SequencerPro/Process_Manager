using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase8c_NonConformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NonConformances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentBlockId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActualValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LimitType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DispositionStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisposedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisposedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    JustificationText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NonConformances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NonConformances_ProcessStepContents_ContentBlockId",
                        column: x => x.ContentBlockId,
                        principalTable: "ProcessStepContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NonConformances_StepExecutions_StepExecutionId",
                        column: x => x.StepExecutionId,
                        principalTable: "StepExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NonConformances_ContentBlockId",
                table: "NonConformances",
                column: "ContentBlockId");

            migrationBuilder.CreateIndex(
                name: "IX_NonConformances_StepExecutionId",
                table: "NonConformances",
                column: "StepExecutionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NonConformances");
        }
    }
}
