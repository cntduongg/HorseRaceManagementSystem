using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRaceScheduledEndTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Thêm cột cho phép NULL trước để không cần default sai lệch.
            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndTime",
                table: "Races",
                type: "timestamp with time zone",
                nullable: true);

            // 2) Backfill dữ liệu cũ: mặc định mỗi race kéo dài 1 giờ (end = start + 1h),
            //    đảm bảo end > start cho mọi bản ghi đã tồn tại.
            migrationBuilder.Sql(
                "UPDATE \"Races\" SET \"ScheduledEndTime\" = \"ScheduledStartTime\" + interval '1 hour' " +
                "WHERE \"ScheduledEndTime\" IS NULL;");

            // 3) Sau khi đã có dữ liệu, siết lại thành NOT NULL.
            migrationBuilder.AlterColumn<DateTime>(
                name: "ScheduledEndTime",
                table: "Races",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScheduledEndTime",
                table: "Races");
        }
    }
}
