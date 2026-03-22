using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class KindEnhancement_ExtendedPropertiesAndModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Kinds",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryOfOrigin",
                table: "Kinds",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeadTimeDays",
                table: "Kinds",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelFileName",
                table: "Kinds",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelMimeType",
                table: "Kinds",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelOriginalFileName",
                table: "Kinds",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Kinds",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Kinds",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Revision",
                table: "Kinds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RohsStatus",
                table: "Kinds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Kinds",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UnitOfMeasure",
                table: "Kinds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorName",
                table: "Kinds",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorPartNumber",
                table: "Kinds",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "Kinds",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeightUnit",
                table: "Kinds",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KindDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KindId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KindDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KindDocuments_Kinds_KindId",
                        column: x => x.KindId,
                        principalTable: "Kinds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KindDocuments_KindId",
                table: "KindDocuments",
                column: "KindId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KindDocuments");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "CountryOfOrigin",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "LeadTimeDays",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "ModelFileName",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "ModelMimeType",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "ModelOriginalFileName",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "RohsStatus",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "UnitOfMeasure",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "VendorName",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "VendorPartNumber",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "WeightUnit",
                table: "Kinds");
        }
    }
}
