using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RevertAllToOriginalState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Legs_RaceId_LegNumber",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_RaceId_LegNumber",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "Stamina",
                table: "Horses");

            migrationBuilder.AlterColumn<int>(
                name: "LegNumber",
                table: "Predictions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "LegRaceId",
                table: "Predictions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_LegRaceId_LegNumber",
                table: "Predictions",
                columns: new[] { "LegRaceId", "LegNumber" });

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Legs_LegRaceId_LegNumber",
                table: "Predictions",
                columns: new[] { "LegRaceId", "LegNumber" },
                principalTable: "Legs",
                principalColumns: new[] { "RaceId", "LegNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Legs_LegRaceId_LegNumber",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_LegRaceId_LegNumber",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "LegRaceId",
                table: "Predictions");

            migrationBuilder.AlterColumn<int>(
                name: "LegNumber",
                table: "Predictions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Stamina",
                table: "Horses",
                type: "integer",
                nullable: false,
                defaultValue: 3);

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
    }
}
