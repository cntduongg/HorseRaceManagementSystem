using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscrepancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Discrepancies",
                columns: table => new
                {
                    DiscrepancyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RaceId = table.Column<int>(type: "integer", nullable: true),
                    ReportedById = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PredictedPosition = table.Column<int>(type: "integer", nullable: true),
                    OfficialPosition = table.Column<int>(type: "integer", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolutionAction = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AdjustedPointsAwarded = table.Column<int>(type: "integer", nullable: true),
                    ResolvedByAdminId = table.Column<int>(type: "integer", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discrepancies", x => x.DiscrepancyId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_CreatedAt",
                table: "Discrepancies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Discrepancies_Status",
                table: "Discrepancies",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Discrepancies");
        }
    }
}
