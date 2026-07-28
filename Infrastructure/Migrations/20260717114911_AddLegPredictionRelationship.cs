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
            /*
             * Thêm nullable trước vì bảng Predictions đang có dữ liệu cũ.
             * Không được thêm NOT NULL ngay vì PostgreSQL sẽ gán mặc định 0.
             */
            migrationBuilder.AddColumn<int>(
                name: "LegNumber",
                table: "Predictions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutionStatus",
                table: "Legs",
                type: "text",
                nullable: false,
                defaultValue: "Pending");

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

            /*
             * Kiểm tra tất cả Race có prediction cũ đều đã có Leg 1.
             * Nếu không có, migration dừng với thông báo rõ ràng thay vì lỗi FK khó hiểu.
             */
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS
                    (
                        SELECT 1
                        FROM "Predictions" AS p
                        LEFT JOIN "Legs" AS l
                            ON l."RaceId" = p."RaceId"
                           AND l."LegNumber" = 1
                        WHERE l."RaceId" IS NULL
                    )
                    THEN
                        RAISE EXCEPTION
                            'Cannot migrate legacy predictions: some races do not have Leg 1.';
                    END IF;
                END $$;
                """);

            /*
             * Prediction cũ trước đây thuộc cả Race.
             * Tạm chuyển dữ liệu legacy về Leg 1.
             */
            migrationBuilder.Sql(
                """
                UPDATE "Predictions"
                SET "LegNumber" = 1
                WHERE "LegNumber" IS NULL;
                """);

            /*
             * Sau khi dữ liệu đã được backfill đầy đủ,
             * mới chuyển LegNumber thành NOT NULL.
             */
            migrationBuilder.AlterColumn<int>(
                name: "LegNumber",
                table: "Predictions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

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
        /// <remarks>
        /// Best-effort rollback for dev only. Up() backfilled Predictions.LegNumber (legacy → 1);
        /// dropping the column destroys per-leg assignment for all predictions and cannot be reconstructed.
        /// Do not run Down() on a database with real prediction data.
        /// </remarks>
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
