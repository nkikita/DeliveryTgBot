using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeliveryTgBot.Migrations
{
    /// <inheritdoc />
    public partial class AddDrivrsToOrder2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Drivers_DriverDataId",
                table: "Orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "DriverDataId",
                table: "Orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Drivers_DriverDataId",
                table: "Orders",
                column: "DriverDataId",
                principalTable: "Drivers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Drivers_DriverDataId",
                table: "Orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "DriverDataId",
                table: "Orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Drivers_DriverDataId",
                table: "Orders",
                column: "DriverDataId",
                principalTable: "Drivers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
