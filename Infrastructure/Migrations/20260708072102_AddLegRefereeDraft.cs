using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegRefereeDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegRefereeDrafts",
                columns: table => new
                {
                    LegRefereeDraftId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    LegNumber = table.Column<int>(type: "integer", nullable: false),
                    RefereeUserId = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegRefereeDrafts", x => x.LegRefereeDraftId);
                    table.ForeignKey(
                        name: "FK_LegRefereeDrafts_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegRefereeDrafts_Legs_RaceId_LegNumber",
                        columns: x => new { x.RaceId, x.LegNumber },
                        principalTable: "Legs",
                        principalColumns: new[] { "RaceId", "LegNumber" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegRefereeDrafts_Referee",
                        column: x => x.RefereeUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegRefereeDrafts_EntryId",
                table: "LegRefereeDrafts",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_LegRefereeDrafts_RaceId_LegNumber_RefereeUserId_EntryId",
                table: "LegRefereeDrafts",
                columns: new[] { "RaceId", "LegNumber", "RefereeUserId", "EntryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegRefereeDrafts_RefereeUserId",
                table: "LegRefereeDrafts",
                column: "RefereeUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegRefereeDrafts");
        }
    }
}
