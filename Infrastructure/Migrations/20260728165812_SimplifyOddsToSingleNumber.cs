using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyOddsToSingleNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Odds gộp về MỘT cột: Entries.Odds. Cột này đã chứa đúng giá trị mô hình mới cần
            // (output thô của RaceOddsAssigner.OddsFor, không trừ biên nhà cái) nên không phải
            // backfill gì. Câu dưới chỉ để cứu row nào lỡ có PublishedOdds mà mất Odds.
            migrationBuilder.Sql(@"
                UPDATE ""Entries""
                SET ""Odds"" = ""PublishedOdds""
                WHERE ""Odds"" <= 0 AND ""PublishedOdds"" > 0;
            ");

            migrationBuilder.DropColumn(
                name: "BettingLockedAt",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "OddsPublishedAt",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "PublishedOdds",
                table: "Entries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BettingLockedAt",
                table: "Races",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OddsPublishedAt",
                table: "Races",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PublishedOdds",
                table: "Entries",
                type: "numeric(10,4)",
                precision: 10,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            // Dựng lại dữ liệu cho mô hình hai tầng (copy từ AddPublishedOddsAndBettingLock).
            // Không có mấy câu này thì rollback để lại 3 cột rỗng: mọi race Scheduled sẽ có bảng
            // cược trắng và CreatePrediction từ chối mọi lệnh vì "does not have published odds".
            migrationBuilder.Sql(@"
                UPDATE ""Entries""
                SET ""PublishedOdds"" = GREATEST(ROUND(""Odds"" * 0.9, 2), 1.01)
                WHERE ""Odds"" > 0;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Races""
                SET ""OddsPublishedAt"" = ""OddsComputedAt""
                WHERE ""OddsComputedAt"" IS NOT NULL;
            ");

            migrationBuilder.Sql(@"
                UPDATE ""Races""
                SET ""BettingLockedAt"" = COALESCE(""UpdatedAt"", ""OddsComputedAt"")
                WHERE ""OddsComputedAt"" IS NOT NULL
                  AND ""Status"" NOT IN ('Scheduled', 'Cancelled');
            ");
        }
    }
}
