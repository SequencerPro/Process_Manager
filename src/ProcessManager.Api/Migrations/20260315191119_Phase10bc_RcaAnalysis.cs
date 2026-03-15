using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase10bc_RcaAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiveWhysAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ProblemStatement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LinkedEntityType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LinkedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosureNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiveWhysAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IshikawaDiagrams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ProblemStatement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    LinkedEntityType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LinkedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosureNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IshikawaDiagrams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiveWhysNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    WhyStatement = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsRootCause = table.Column<bool>(type: "boolean", nullable: false),
                    RootCauseLibraryEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrectiveAction = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiveWhysNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiveWhysNodes_FiveWhysAnalyses_AnalysisId",
                        column: x => x.AnalysisId,
                        principalTable: "FiveWhysAnalyses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiveWhysNodes_FiveWhysNodes_ParentNodeId",
                        column: x => x.ParentNodeId,
                        principalTable: "FiveWhysNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiveWhysNodes_RootCauseEntries_RootCauseLibraryEntryId",
                        column: x => x.RootCauseLibraryEntryId,
                        principalTable: "RootCauseEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IshikawaCauses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiagramId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CauseText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ParentCauseId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootCauseLibraryEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsSelectedRootCause = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IshikawaCauses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IshikawaCauses_IshikawaCauses_ParentCauseId",
                        column: x => x.ParentCauseId,
                        principalTable: "IshikawaCauses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IshikawaCauses_IshikawaDiagrams_DiagramId",
                        column: x => x.DiagramId,
                        principalTable: "IshikawaDiagrams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IshikawaCauses_RootCauseEntries_RootCauseLibraryEntryId",
                        column: x => x.RootCauseLibraryEntryId,
                        principalTable: "RootCauseEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiveWhysAnalyses_LinkedEntityId",
                table: "FiveWhysAnalyses",
                column: "LinkedEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_FiveWhysNodes_AnalysisId",
                table: "FiveWhysNodes",
                column: "AnalysisId");

            migrationBuilder.CreateIndex(
                name: "IX_FiveWhysNodes_ParentNodeId",
                table: "FiveWhysNodes",
                column: "ParentNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FiveWhysNodes_RootCauseLibraryEntryId",
                table: "FiveWhysNodes",
                column: "RootCauseLibraryEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_IshikawaCauses_DiagramId",
                table: "IshikawaCauses",
                column: "DiagramId");

            migrationBuilder.CreateIndex(
                name: "IX_IshikawaCauses_ParentCauseId",
                table: "IshikawaCauses",
                column: "ParentCauseId");

            migrationBuilder.CreateIndex(
                name: "IX_IshikawaCauses_RootCauseLibraryEntryId",
                table: "IshikawaCauses",
                column: "RootCauseLibraryEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_IshikawaDiagrams_LinkedEntityId",
                table: "IshikawaDiagrams",
                column: "LinkedEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiveWhysNodes");

            migrationBuilder.DropTable(
                name: "IshikawaCauses");

            migrationBuilder.DropTable(
                name: "FiveWhysAnalyses");

            migrationBuilder.DropTable(
                name: "IshikawaDiagrams");
        }
    }
}
