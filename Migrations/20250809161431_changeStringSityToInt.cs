using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryTgBot.Migrations
{
    /// <inheritdoc />
    public partial class changeStringSityToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Drivers");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Cityes",
                newName: "Id");

            migrationBuilder.AddColumn<int>(
                name: "CityId",
                table: "Drivers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Drivers_CityId",
                table: "Drivers",
                column: "CityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Cityes_CityId",
                table: "Drivers",
                column: "CityId",
                principalTable: "Cityes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Cityes_CityId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_CityId",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CityId",
                table: "Drivers");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Cityes",
                newName: "id");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Drivers",
                type: "text",
                nullable: true);
        }
    }
}
