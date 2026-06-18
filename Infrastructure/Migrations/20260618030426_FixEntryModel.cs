using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixEntryModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrizePointTransactions_RaceResults_RaceResultId",
                table: "PrizePointTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RaceResults",
                table: "RaceResults");

            migrationBuilder.DropIndex(
                name: "IX_RaceResults_EntryId",
                table: "RaceResults");

            migrationBuilder.DropIndex(
                name: "IX_PrizePointTransactions_RaceResultId",
                table: "PrizePointTransactions");

            migrationBuilder.DropColumn(
                name: "RaceResultId",
                table: "RaceResults");

            migrationBuilder.DropColumn(
                name: "RaceResultId",
                table: "PrizePointTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "TotalPoints",
                table: "RaceResults",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "LegWinCount",
                table: "RaceResults",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "LegTop3Count",
                table: "RaceResults",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRaceDQ",
                table: "RaceResults",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RaceResults",
                table: "RaceResults",
                columns: new[] { "RaceId", "EntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_RaceResults_EntryId",
                table: "RaceResults",
                column: "EntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrizePointTransactions_RaceResults_RaceId_EntryId",
                table: "PrizePointTransactions",
                columns: new[] { "RaceId", "EntryId" },
                principalTable: "RaceResults",
                principalColumns: new[] { "RaceId", "EntryId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PrizePointTransactions_RaceResults_RaceId_EntryId",
                table: "PrizePointTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RaceResults",
                table: "RaceResults");

            migrationBuilder.DropIndex(
                name: "IX_RaceResults_EntryId",
                table: "RaceResults");

            migrationBuilder.AlterColumn<int>(
                name: "TotalPoints",
                table: "RaceResults",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "LegWinCount",
                table: "RaceResults",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "LegTop3Count",
                table: "RaceResults",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<bool>(
                name: "IsRaceDQ",
                table: "RaceResults",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RaceResultId",
                table: "RaceResults",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<int>(
                name: "RaceResultId",
                table: "PrizePointTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RaceResults",
                table: "RaceResults",
                column: "RaceResultId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceResults_EntryId",
                table: "RaceResults",
                column: "EntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrizePointTransactions_RaceResultId",
                table: "PrizePointTransactions",
                column: "RaceResultId");

            migrationBuilder.AddForeignKey(
                name: "FK_PrizePointTransactions_RaceResults_RaceResultId",
                table: "PrizePointTransactions",
                column: "RaceResultId",
                principalTable: "RaceResults",
                principalColumn: "RaceResultId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
