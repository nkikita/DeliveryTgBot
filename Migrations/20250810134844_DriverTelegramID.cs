using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryTgBot.Migrations
{
    /// <inheritdoc />
    public partial class DriverTelegramID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TelegramId",
                table: "Drivers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TelegramId",
                table: "Drivers");
        }
    }
}
