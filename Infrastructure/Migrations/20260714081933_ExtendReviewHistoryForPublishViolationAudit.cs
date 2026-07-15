using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExtendReviewHistoryForPublishViolationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AfterData",
                table: "ReviewHistories",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeforeData",
                table: "ReviewHistories",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReviewHistories_EntityType_EntityId_CreatedAt",
                table: "ReviewHistories",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ReviewHistories_EntityType_EntityId_CreatedAt",
                table: "ReviewHistories");

            migrationBuilder.DropColumn(
                name: "AfterData",
                table: "ReviewHistories");

            migrationBuilder.DropColumn(
                name: "BeforeData",
                table: "ReviewHistories");
        }
    }
}
