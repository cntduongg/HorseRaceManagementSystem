using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenPasswordResetFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PasswordResetOtps_UserId",
                table: "PasswordResetOtps");

            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "PasswordResetOtps");

            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "PasswordResetOtps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OtpCodeHash",
                table: "PasswordResetOtps",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_Active_UserId_CreatedAt",
                table: "PasswordResetOtps",
                columns: new[] { "UserId", "CreatedAt" },
                filter: "\"UsedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PasswordResetOtps_Active_UserId_CreatedAt",
                table: "PasswordResetOtps");

            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "PasswordResetOtps");

            migrationBuilder.DropColumn(
                name: "OtpCodeHash",
                table: "PasswordResetOtps");

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "PasswordResetOtps",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_UserId",
                table: "PasswordResetOtps",
                column: "UserId");
        }
    }
}
