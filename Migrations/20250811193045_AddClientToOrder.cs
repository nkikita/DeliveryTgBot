using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryTgBot.Migrations
{
    /// <inheritdoc />
    public partial class AddClientToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Drivers_DriverDataId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DriverDataId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DriverDataId",
                table: "Orders");

            migrationBuilder.AddColumn<string>(
                name: "ClientTelegramUsername",
                table: "Orders",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientTelegramUsername",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "DriverDataId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DriverDataId",
                table: "Orders",
                column: "DriverDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Drivers_DriverDataId",
                table: "Orders",
                column: "DriverDataId",
                principalTable: "Drivers",
                principalColumn: "Id");
        }
    }
}
