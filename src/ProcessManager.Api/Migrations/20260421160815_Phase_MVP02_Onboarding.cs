using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase_MVP02_Onboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantFeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShowAdvancedModules = table.Column<bool>(type: "boolean", nullable: false),
                    ShowQualityTools = table.Column<bool>(type: "boolean", nullable: false),
                    ShowProductionTools = table.Column<bool>(type: "boolean", nullable: false),
                    ShowWarehouseTools = table.Column<bool>(type: "boolean", nullable: false),
                    ShowTrainingTools = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantFeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantOnboardingStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Industry = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SkippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstKindId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstStepTemplateId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstProcessId = table.Column<Guid>(type: "uuid", nullable: true),
                    FirstJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    SignupAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstJobCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantOnboardingStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantFeatureFlags_TenantId",
                table: "TenantFeatureFlags",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantOnboardingStates_TenantId",
                table: "TenantOnboardingStates",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantFeatureFlags");

            migrationBuilder.DropTable(
                name: "TenantOnboardingStates");
        }
    }
}
