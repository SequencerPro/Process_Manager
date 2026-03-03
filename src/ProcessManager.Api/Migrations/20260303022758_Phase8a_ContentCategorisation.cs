using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase8a_ContentCategorisation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcknowledgmentRequired",
                table: "StepTemplateContents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContentCategory",
                table: "StepTemplateContents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHardLimit",
                table: "StepTemplateContents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "NominalValue",
                table: "StepTemplateContents",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AcknowledgmentRequired",
                table: "ProcessStepContents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContentCategory",
                table: "ProcessStepContents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHardLimit",
                table: "ProcessStepContents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "NominalValue",
                table: "ProcessStepContents",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcknowledgmentRequired",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "ContentCategory",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "IsHardLimit",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "NominalValue",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "AcknowledgmentRequired",
                table: "ProcessStepContents");

            migrationBuilder.DropColumn(
                name: "ContentCategory",
                table: "ProcessStepContents");

            migrationBuilder.DropColumn(
                name: "IsHardLimit",
                table: "ProcessStepContents");

            migrationBuilder.DropColumn(
                name: "NominalValue",
                table: "ProcessStepContents");
        }
    }
}
