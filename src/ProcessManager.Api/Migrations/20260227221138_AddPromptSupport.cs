using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Choices",
                table: "StepTemplateContents",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "StepTemplateContents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "StepTemplateContents",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxValue",
                table: "StepTemplateContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinValue",
                table: "StepTemplateContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptType",
                table: "StepTemplateContents",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Units",
                table: "StepTemplateContents",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Choices",
                table: "ProcessStepContents",
                type: "TEXT",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRequired",
                table: "ProcessStepContents",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "ProcessStepContents",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxValue",
                table: "ProcessStepContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinValue",
                table: "ProcessStepContents",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromptType",
                table: "ProcessStepContents",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Units",
                table: "ProcessStepContents",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PromptResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StepExecutionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProcessStepContentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StepTemplateContentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ResponseValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsOutOfRange = table.Column<bool>(type: "INTEGER", nullable: false),
                    OverrideNote = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptResponses_ProcessStepContents_ProcessStepContentId",
                        column: x => x.ProcessStepContentId,
                        principalTable: "ProcessStepContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PromptResponses_StepExecutions_StepExecutionId",
                        column: x => x.StepExecutionId,
                        principalTable: "StepExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromptResponses_StepTemplateContents_StepTemplateContentId",
                        column: x => x.StepTemplateContentId,
                        principalTable: "StepTemplateContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PromptResponses_ProcessStepContentId",
                table: "PromptResponses",
                column: "ProcessStepContentId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptResponses_StepExecutionId",
                table: "PromptResponses",
                column: "StepExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptResponses_StepTemplateContentId",
                table: "PromptResponses",
                column: "StepTemplateContentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromptResponses");

            migrationBuilder.DropColumn(
                name: "Choices",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "PromptType",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "Units",
                table: "StepTemplateContents");

            migrationBuilder.DropColumn(
                name: "Choices",
                table: "ProcessStepContents");

            migrationBuilder.DropColumn(
                name: "IsRequired",
                table: "ProcessStepContents");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "ProcessStepContents");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "ProcessStepContents");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "ProcessStepContents");

            migrationBuilder.DropColumn(
                name: "PromptType",
                table: "ProcessStepContents");

            migrationBuilder.DropColumn(
                name: "Units",
                table: "ProcessStepContents");
        }
    }
}
