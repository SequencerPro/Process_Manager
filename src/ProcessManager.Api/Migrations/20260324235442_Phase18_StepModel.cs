using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase18_StepModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "KindModelRefId",
                table: "StepTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StepModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StepTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StepModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StepModels_StepTemplates_StepTemplateId",
                        column: x => x.StepTemplateId,
                        principalTable: "StepTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StepTemplates_KindModelRefId",
                table: "StepTemplates",
                column: "KindModelRefId");

            migrationBuilder.CreateIndex(
                name: "IX_StepModels_StepTemplateId",
                table: "StepModels",
                column: "StepTemplateId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StepTemplates_Kinds_KindModelRefId",
                table: "StepTemplates",
                column: "KindModelRefId",
                principalTable: "Kinds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StepTemplates_Kinds_KindModelRefId",
                table: "StepTemplates");

            migrationBuilder.DropTable(
                name: "StepModels");

            migrationBuilder.DropIndex(
                name: "IX_StepTemplates_KindModelRefId",
                table: "StepTemplates");

            migrationBuilder.DropColumn(
                name: "KindModelRefId",
                table: "StepTemplates");
        }
    }
}
