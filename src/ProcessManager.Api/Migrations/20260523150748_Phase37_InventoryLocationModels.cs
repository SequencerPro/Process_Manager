using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase37_InventoryLocationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConversionError",
                table: "FloorPlanInventoryLocations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConversionStatus",
                table: "FloorPlanInventoryLocations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConvertedModelFileName",
                table: "FloorPlanInventoryLocations",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelFileName",
                table: "FloorPlanInventoryLocations",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModelMimeType",
                table: "FloorPlanInventoryLocations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ModelOffsetX",
                table: "FloorPlanInventoryLocations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ModelOffsetY",
                table: "FloorPlanInventoryLocations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ModelOffsetZ",
                table: "FloorPlanInventoryLocations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ModelOriginalFileName",
                table: "FloorPlanInventoryLocations",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ModelScale",
                table: "FloorPlanInventoryLocations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ModelYaw",
                table: "FloorPlanInventoryLocations",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConversionError",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ConversionStatus",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ConvertedModelFileName",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ModelFileName",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ModelMimeType",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ModelOffsetX",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ModelOffsetY",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ModelOffsetZ",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ModelOriginalFileName",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ModelScale",
                table: "FloorPlanInventoryLocations");

            migrationBuilder.DropColumn(
                name: "ModelYaw",
                table: "FloorPlanInventoryLocations");
        }
    }
}
