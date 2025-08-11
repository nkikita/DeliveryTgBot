using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryTgBot.Migrations
{
    /// <inheritdoc />
    public partial class AddDrivrsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DriverDataId",
                table: "Orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DriverDataId",
                table: "Orders",
                column: "DriverDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Drivers_DriverDataId",
                table: "Orders",
                column: "DriverDataId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
