using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RevertAllToOriginalState : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// ⚠️ Migration này VÀ <c>20260725162506_DropOrphanPerLegColumns</c> drop CÙNG một bộ cột
        /// mồ côi per-leg (T-27). Đó là hệ quả của đợt revert 2026-07-25: bản gốc bị xóa rồi thêm
        /// lại ở commit <c>eec39fb</c>, trong khi bản raw-SQL đã được viết ở giữa.
        ///
        /// Bản gốc dùng <c>DropForeignKey</c>/<c>DropIndex</c>/<c>DropColumn</c> nên nó **giả định
        /// các đối tượng còn tồn tại**. DB nào đã chạy app ở khoảng commit <c>e490dc8</c>…
        /// <c>eec39fb</c> sẽ apply <c>DropOrphanPerLegColumns</c> TRƯỚC, xóa sạch bộ cột, rồi tới
        /// lần khởi động sau EF thấy migration này còn pending và chạy nó ⇒ <c>DROP CONSTRAINT</c>
        /// trên thứ đã biến mất ⇒ **crash lúc khởi động**.
        ///
        /// Nay viết lại thành raw SQL <c>IF EXISTS</c>, khớp đúng bản kia ⇒ hai migration trở
        /// thành idempotent với nhau, chạy theo thứ tự nào cũng được. Nội dung SQL cố tình giữ
        /// nguyên xi (kể cả 2 dòng <c>LegRaceId</c> mà bản gốc không có) để hai file không lệch.
        /// </remarks>
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
