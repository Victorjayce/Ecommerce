using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Migrations
{
    /// <inheritdoc />
    public partial class addeduseridtorequiredmodels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "OrdersTable",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CartItemsTable",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OrdersTable_UserId",
                table: "OrdersTable",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItemsTable_UserId",
                table: "CartItemsTable",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItemsTable_AspNetUsers_UserId",
                table: "CartItemsTable",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrdersTable_AspNetUsers_UserId",
                table: "OrdersTable",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItemsTable_AspNetUsers_UserId",
                table: "CartItemsTable");

            migrationBuilder.DropForeignKey(
                name: "FK_OrdersTable_AspNetUsers_UserId",
                table: "OrdersTable");

            migrationBuilder.DropIndex(
                name: "IX_OrdersTable_UserId",
                table: "OrdersTable");

            migrationBuilder.DropIndex(
                name: "IX_CartItemsTable_UserId",
                table: "CartItemsTable");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OrdersTable");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CartItemsTable");
        }
    }
}
