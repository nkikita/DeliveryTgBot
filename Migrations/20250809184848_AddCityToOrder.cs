using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryTgBot.Migrations
{
    /// <inheritdoc />
    public partial class AddCityToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Cityes_CityId",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "CityId",
                table: "Orders",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Cityes_CityId",
                table: "Orders",
                column: "CityId",
                principalTable: "Cityes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Cityes_CityId",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "CityId",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Cityes_CityId",
                table: "Orders",
                column: "CityId",
                principalTable: "Cityes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
