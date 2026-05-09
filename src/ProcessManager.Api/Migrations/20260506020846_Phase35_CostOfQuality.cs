using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase35_CostOfQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityCostRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerEvent = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DefaultCategory = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DefaultSourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityCostRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualityCosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceEntityCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "USD"),
                    CostCategory = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    KindId = table.Column<Guid>(type: "uuid", nullable: true),
                    KindName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    JobId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecordedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RecordedByDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityCosts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityCostRules_TriggerEvent",
                table: "QualityCostRules",
                column: "TriggerEvent");

            migrationBuilder.CreateIndex(
                name: "IX_QualityCosts_CostCategory",
                table: "QualityCosts",
                column: "CostCategory");

            migrationBuilder.CreateIndex(
                name: "IX_QualityCosts_RecordedAt",
                table: "QualityCosts",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_QualityCosts_SourceEntityId",
                table: "QualityCosts",
                column: "SourceEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QualityCostRules");

            migrationBuilder.DropTable(
                name: "QualityCosts");
        }
    }
}
