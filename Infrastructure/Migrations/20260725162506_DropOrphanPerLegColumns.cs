using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropOrphanPerLegColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Predictions" DROP CONSTRAINT IF EXISTS "FK_Predictions_Legs_RaceId_LegNumber";
                ALTER TABLE "Predictions" DROP CONSTRAINT IF EXISTS "FK_Predictions_Legs_LegRaceId_LegNumber";
                DROP INDEX IF EXISTS "IX_Predictions_RaceId_LegNumber";
                DROP INDEX IF EXISTS "IX_Predictions_LegRaceId_LegNumber";
                ALTER TABLE "Predictions" DROP COLUMN IF EXISTS "LegNumber";
                ALTER TABLE "Predictions" DROP COLUMN IF EXISTS "LegRaceId";
                ALTER TABLE "Legs"   DROP COLUMN IF EXISTS "ExecutionStatus";
                ALTER TABLE "Legs"   DROP COLUMN IF EXISTS "PredictionOpenedAt";
                ALTER TABLE "Legs"   DROP COLUMN IF EXISTS "PredictionClosedAt";
                ALTER TABLE "Horses" DROP COLUMN IF EXISTS "Stamina";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot restore NOT NULL columns (LegNumber, etc.) on tables that may already have rows.
        }
    }
}
