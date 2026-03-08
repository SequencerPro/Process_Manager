using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessStepPortOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PatternOverride",
                table: "ProcessSteps",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProcessStepPortOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessStepId = table.Column<Guid>(type: "uuid", nullable: false),
                    PortId = table.Column<Guid>(type: "uuid", nullable: false),
                    NameOverride = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DirectionOverride = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    KindIdOverride = table.Column<Guid>(type: "uuid", nullable: true),
                    GradeIdOverride = table.Column<Guid>(type: "uuid", nullable: true),
                    QtyRuleModeOverride = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    QtyRuleNOverride = table.Column<int>(type: "integer", nullable: true),
                    SortOrderOverride = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessStepPortOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessStepPortOverrides_Grades_GradeIdOverride",
                        column: x => x.GradeIdOverride,
                        principalTable: "Grades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessStepPortOverrides_Kinds_KindIdOverride",
                        column: x => x.KindIdOverride,
                        principalTable: "Kinds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessStepPortOverrides_Ports_PortId",
                        column: x => x.PortId,
                        principalTable: "Ports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcessStepPortOverrides_ProcessSteps_ProcessStepId",
                        column: x => x.ProcessStepId,
                        principalTable: "ProcessSteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStepPortOverrides_GradeIdOverride",
                table: "ProcessStepPortOverrides",
                column: "GradeIdOverride");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStepPortOverrides_KindIdOverride",
                table: "ProcessStepPortOverrides",
                column: "KindIdOverride");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStepPortOverrides_PortId",
                table: "ProcessStepPortOverrides",
                column: "PortId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessStepPortOverrides_ProcessStepId_PortId",
                table: "ProcessStepPortOverrides",
                columns: new[] { "ProcessStepId", "PortId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessStepPortOverrides");

            migrationBuilder.DropColumn(
                name: "PatternOverride",
                table: "ProcessSteps");
        }
    }
}
