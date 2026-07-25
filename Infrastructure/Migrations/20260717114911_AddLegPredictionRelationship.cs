using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLegPredictionRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LegNumber",
                table: "Predictions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionStatus",
                table: "Legs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PredictionClosedAt",
                table: "Legs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PredictionOpenedAt",
                table: "Legs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_RaceId_LegNumber",
                table: "Predictions",
                columns: new[] { "RaceId", "LegNumber" });

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Legs_RaceId_LegNumber",
                table: "Predictions",
                columns: new[] { "RaceId", "LegNumber" },
                principalTable: "Legs",
                principalColumns: new[] { "RaceId", "LegNumber" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Legs_RaceId_LegNumber",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_RaceId_LegNumber",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "LegNumber",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "ExecutionStatus",
                table: "Legs");

            migrationBuilder.DropColumn(
                name: "PredictionClosedAt",
                table: "Legs");

            migrationBuilder.DropColumn(
                name: "PredictionOpenedAt",
                table: "Legs");
        }
    }
}
