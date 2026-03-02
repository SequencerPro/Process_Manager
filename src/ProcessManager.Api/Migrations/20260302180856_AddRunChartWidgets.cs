using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRunChartWidgets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RunChartWidgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ChartWindowSize = table.Column<int>(type: "integer", nullable: false),
                    SpecMin = table.Column<decimal>(type: "numeric", nullable: true),
                    SpecMax = table.Column<decimal>(type: "numeric", nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunChartWidgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunChartWidgets_StepTemplateContents_SourceContentId",
                        column: x => x.SourceContentId,
                        principalTable: "StepTemplateContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunChartWidgets_StepTemplates_StepTemplateId",
                        column: x => x.StepTemplateId,
                        principalTable: "StepTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunChartWidgets_SourceContentId",
                table: "RunChartWidgets",
                column: "SourceContentId");

            migrationBuilder.CreateIndex(
                name: "IX_RunChartWidgets_StepTemplateId",
                table: "RunChartWidgets",
                column: "StepTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunChartWidgets");
        }
    }
}
