using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase37_FactoryDesignModelsAndDesignations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConversionError",
                table: "FloorPlanWorkstations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConversionStatus",
                table: "FloorPlanWorkstations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConvertedModelFileName",
                table: "FloorPlanWorkstations",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelFileName",
                table: "FloorPlanWorkstations",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelMimeType",
                table: "FloorPlanWorkstations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ModelOffsetX",
                table: "FloorPlanWorkstations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ModelOffsetY",
                table: "FloorPlanWorkstations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ModelOffsetZ",
                table: "FloorPlanWorkstations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ModelOriginalFileName",
                table: "FloorPlanWorkstations",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ModelScale",
                table: "FloorPlanWorkstations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ModelYaw",
                table: "FloorPlanWorkstations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "FloorPlanInventoryLocationKinds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FloorPlanInventoryLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    KindId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlanInventoryLocationKinds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlanInventoryLocationKinds_FloorPlanInventoryLocations~",
                        column: x => x.FloorPlanInventoryLocationId,
                        principalTable: "FloorPlanInventoryLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FloorPlanInventoryLocationKinds_Kinds_KindId",
                        column: x => x.KindId,
                        principalTable: "Kinds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanInventoryLocationKinds_FloorPlanInventoryLocationI~",
                table: "FloorPlanInventoryLocationKinds",
                columns: new[] { "FloorPlanInventoryLocationId", "KindId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanInventoryLocationKinds_KindId",
                table: "FloorPlanInventoryLocationKinds",
                column: "KindId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FloorPlanInventoryLocationKinds");

            migrationBuilder.DropColumn(
                name: "ConversionError",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ConversionStatus",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ConvertedModelFileName",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ModelFileName",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ModelMimeType",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ModelOffsetX",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ModelOffsetY",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ModelOffsetZ",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ModelOriginalFileName",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ModelScale",
                table: "FloorPlanWorkstations");

            migrationBuilder.DropColumn(
                name: "ModelYaw",
                table: "FloorPlanWorkstations");
        }
    }
}
