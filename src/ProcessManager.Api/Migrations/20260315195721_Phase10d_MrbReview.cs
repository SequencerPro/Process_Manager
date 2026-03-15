using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessManager.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase10d_MrbReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MrbRequired",
                table: "NonConformances",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "MrbReviewId",
                table: "NonConformances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MrbReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NonConformanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ItemDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    QuantityAffected = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ProblemStatement = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CustomerNotificationRequired = table.Column<bool>(type: "boolean", nullable: false),
                    ScarRequired = table.Column<bool>(type: "boolean", nullable: false),
                    SupplierCaused = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresRca = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedRcaAnalysisType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    LinkedRcaId = table.Column<Guid>(type: "uuid", nullable: true),
                    DispositionDecision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    DispositionJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DecidedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MrbReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MrbReviews_NonConformances_NonConformanceId",
                        column: x => x.NonConformanceId,
                        principalTable: "NonConformances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MrbParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MrbReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Assessment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    AssessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MrbParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MrbParticipants_MrbReviews_MrbReviewId",
                        column: x => x.MrbReviewId,
                        principalTable: "MrbReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MrbParticipants_MrbReviewId",
                table: "MrbParticipants",
                column: "MrbReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_MrbReviews_NonConformanceId",
                table: "MrbReviews",
                column: "NonConformanceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MrbReviews_Status",
                table: "MrbReviews",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MrbParticipants");

            migrationBuilder.DropTable(
                name: "MrbReviews");

            migrationBuilder.DropColumn(
                name: "MrbRequired",
                table: "NonConformances");

            migrationBuilder.DropColumn(
                name: "MrbReviewId",
                table: "NonConformances");
        }
    }
}
