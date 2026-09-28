using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWithdrawalItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category_ids",
                table: "withdrawals");

            migrationBuilder.CreateTable(
                name: "withdrawal_items",
                columns: table => new
                {
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    withdrawal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_withdrawal_items", x => new { x.withdrawal_id, x.stock_item_id });
                    table.ForeignKey(
                        name: "FK_withdrawal_items_stock_items_stock_item_id",
                        column: x => x.stock_item_id,
                        principalTable: "stock_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_withdrawal_items_withdrawals_withdrawal_id",
                        column: x => x.withdrawal_id,
                        principalTable: "withdrawals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_withdrawal_items_stock_item_id",
                table: "withdrawal_items",
                column: "stock_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "withdrawal_items");

            migrationBuilder.AddColumn<Guid[]>(
                name: "category_ids",
                table: "withdrawals",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);
        }
    }
}
