using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropTournamentLogoUrl : Migration
    {
        /// <inheritdoc />
        // Gỡ hẳn logo của Tournament (cả FE lẫn BE). Cột này chỉ là ô nhập URL tự do:
        // không có endpoint upload ảnh, không validate gì, và chỗ duy nhất đọc nó là
        // avatar 24px ở bảng Admin Tournaments — không đáng để giữ một cột dữ liệu.
        // DROP là mất dữ liệu không lấy lại được: Down() dựng lại cột nhưng các URL cũ đã đi.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Tournaments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Tournaments",
                type: "text",
                nullable: true);
        }
    }
}
