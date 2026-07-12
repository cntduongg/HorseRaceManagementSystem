using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceJockeyInvitationUniqueIndexWithFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JockeyInvitations_HorseOwnerId_JockeyId_HorseId_RaceId",
                table: "JockeyInvitations");

            migrationBuilder.CreateIndex(
                name: "IX_JockeyInvitations_HorseOwnerId_JockeyId_HorseId_RaceId",
                table: "JockeyInvitations",
                columns: new[] { "HorseOwnerId", "JockeyId", "HorseId", "RaceId" },
                unique: true,
                filter: "\"Status\" IN ('Pending', 'Accepted', 'Confirmed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JockeyInvitations_HorseOwnerId_JockeyId_HorseId_RaceId",
                table: "JockeyInvitations");

            migrationBuilder.CreateIndex(
                name: "IX_JockeyInvitations_HorseOwnerId_JockeyId_HorseId_RaceId",
                table: "JockeyInvitations",
                columns: new[] { "HorseOwnerId", "JockeyId", "HorseId", "RaceId" },
                unique: true);
        }
    }
}
