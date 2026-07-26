using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase17_F7_BarcodeAndScanTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "StorageLocations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Kinds",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Items",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Workstations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    FixedLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workstations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workstations_StorageLocations_FixedLocationId",
                        column: x => x.FixedLocationId,
                        principalTable: "StorageLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WorkstationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Workstations_WorkstationId",
                        column: x => x.WorkstationId,
                        principalTable: "Workstations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScanEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkstationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApiKeyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScannedBarcode = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Result = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScanEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScanEvents_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScanEvents_InventoryTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "InventoryTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScanEvents_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScanEvents_Workstations_WorkstationId",
                        column: x => x.WorkstationId,
                        principalTable: "Workstations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StorageLocations_Barcode",
                table: "StorageLocations",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kinds_Barcode",
                table: "Kinds",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Items_Barcode",
                table: "Items",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_KeyHash",
                table: "ApiKeys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_WorkstationId",
                table: "ApiKeys",
                column: "WorkstationId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanEvents_ApiKeyId",
                table: "ScanEvents",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanEvents_ItemId",
                table: "ScanEvents",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanEvents_ScannedAt",
                table: "ScanEvents",
                column: "ScannedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ScanEvents_TransactionId",
                table: "ScanEvents",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ScanEvents_WorkstationId",
                table: "ScanEvents",
                column: "WorkstationId");

            migrationBuilder.CreateIndex(
                name: "IX_Workstations_Code",
                table: "Workstations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workstations_FixedLocationId",
                table: "Workstations",
                column: "FixedLocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScanEvents");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "Workstations");

            migrationBuilder.DropIndex(
                name: "IX_StorageLocations_Barcode",
                table: "StorageLocations");

            migrationBuilder.DropIndex(
                name: "IX_Kinds_Barcode",
                table: "Kinds");

            migrationBuilder.DropIndex(
                name: "IX_Items_Barcode",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "StorageLocations");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Kinds");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Items");
        }
    }
}
