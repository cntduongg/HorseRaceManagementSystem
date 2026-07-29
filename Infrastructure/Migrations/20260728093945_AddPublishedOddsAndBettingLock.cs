using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedOddsAndBettingLock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            // ── Backfill dữ liệu đang chạy ──────────────────────────────────────────────
            // Không backfill thì mọi race đã đóng đăng ký từ trước sẽ có PublishedOdds = 0 và
            // OddsPublishedAt = null ⇒ bảng cược của spectator trống trơn cho tới khi Admin vào
            // bấm lại từng race. Đây là DB demo đang có người dùng thật, nên vá tại chỗ.

            // 1) Odds công bố = đề xuất − 10%, sàn 1.01 (odds ≤ 1 = đoán đúng vẫn lỗ).
            migrationBuilder.Sql(@"
                UPDATE ""Entries""
                SET ""PublishedOdds"" = GREATEST(ROUND(""Odds"" * 0.9, 2), 1.01)
                WHERE ""Odds"" > 0;
            ");

            // 2) Race đã đóng đăng ký coi như đã công bố odds (giữ nguyên hành vi cũ:
            //    đóng đăng ký xong là cược được ngay).
            migrationBuilder.Sql(@"
                UPDATE ""Races""
                SET ""OddsPublishedAt"" = ""OddsComputedAt""
                WHERE ""OddsComputedAt"" IS NOT NULL;
            ");

            // 3) Race đã rời Scheduled thì sổ cược thực tế đã đóng từ lâu — đánh dấu cho khớp.
            migrationBuilder.Sql(@"
                UPDATE ""Races""
                SET ""BettingLockedAt"" = COALESCE(""UpdatedAt"", ""OddsComputedAt"")
                WHERE ""OddsComputedAt"" IS NOT NULL
                  AND ""Status"" NOT IN ('Scheduled', 'Cancelled');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
