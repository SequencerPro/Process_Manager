using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase22_FactoryDesignSuite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FloorPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LayoutJson = table.Column<string>(type: "text", nullable: false),
                    ThumbnailBase64 = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FloorPlanInventoryLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FloorPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlacementId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlanInventoryLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlanInventoryLocations_FloorPlans_FloorPlanId",
                        column: x => x.FloorPlanId,
                        principalTable: "FloorPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FloorPlanInventoryLocations_StorageLocations_StorageLocatio~",
                        column: x => x.StorageLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FloorPlanWorkstations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FloorPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlacementId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrgUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    StorageLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlanWorkstations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlanWorkstations_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FloorPlanWorkstations_FloorPlans_FloorPlanId",
                        column: x => x.FloorPlanId,
                        principalTable: "FloorPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FloorPlanWorkstations_OrgUnits_OrgUnitId",
                        column: x => x.OrgUnitId,
                        principalTable: "OrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FloorPlanWorkstations_StorageLocations_StorageLocationId",
                        column: x => x.StorageLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FloorPlanWorkstationProcesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FloorPlanWorkstationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcessId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlanWorkstationProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlanWorkstationProcesses_FloorPlanWorkstations_FloorPl~",
                        column: x => x.FloorPlanWorkstationId,
                        principalTable: "FloorPlanWorkstations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FloorPlanWorkstationProcesses_Processes_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "Processes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FloorPlanWorkstationTools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FloorPlanWorkstationId = table.Column<Guid>(type: "uuid", nullable: false),
                    KindId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlanWorkstationTools", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlanWorkstationTools_FloorPlanWorkstations_FloorPlanWo~",
                        column: x => x.FloorPlanWorkstationId,
                        principalTable: "FloorPlanWorkstations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FloorPlanWorkstationTools_Kinds_KindId",
                        column: x => x.KindId,
                        principalTable: "Kinds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanInventoryLocations_FloorPlanId_PlacementId",
                table: "FloorPlanInventoryLocations",
                columns: new[] { "FloorPlanId", "PlacementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanInventoryLocations_FloorPlanId_StorageLocationId",
                table: "FloorPlanInventoryLocations",
                columns: new[] { "FloorPlanId", "StorageLocationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanInventoryLocations_StorageLocationId",
                table: "FloorPlanInventoryLocations",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_Code",
                table: "FloorPlans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanWorkstationProcesses_FloorPlanWorkstationId_Proces~",
                table: "FloorPlanWorkstationProcesses",
                columns: new[] { "FloorPlanWorkstationId", "ProcessId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanWorkstationProcesses_ProcessId",
                table: "FloorPlanWorkstationProcesses",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanWorkstations_EquipmentId",
                table: "FloorPlanWorkstations",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanWorkstations_FloorPlanId_PlacementId",
                table: "FloorPlanWorkstations",
                columns: new[] { "FloorPlanId", "PlacementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanWorkstations_OrgUnitId",
                table: "FloorPlanWorkstations",
                column: "OrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanWorkstations_StorageLocationId",
                table: "FloorPlanWorkstations",
                column: "StorageLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanWorkstationTools_FloorPlanWorkstationId_KindId",
                table: "FloorPlanWorkstationTools",
                columns: new[] { "FloorPlanWorkstationId", "KindId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanWorkstationTools_KindId",
                table: "FloorPlanWorkstationTools",
                column: "KindId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FloorPlanInventoryLocations");

            migrationBuilder.DropTable(
                name: "FloorPlanWorkstationProcesses");

            migrationBuilder.DropTable(
                name: "FloorPlanWorkstationTools");

            migrationBuilder.DropTable(
                name: "FloorPlanWorkstations");

            migrationBuilder.DropTable(
                name: "FloorPlans");
        }
    }
}
