using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGranularStockAndBasketTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "stock",
                table: "collection_points");

            migrationBuilder.DropColumn(
                name: "stock_settings",
                table: "collection_points");

            migrationBuilder.AddColumn<int>(
                name: "basket_quantity",
                table: "withdrawals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "basket_template_id",
                table: "withdrawals",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "basket_templates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_basket_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_basket_templates_collection_points_PointId",
                        column: x => x.PointId,
                        principalTable: "collection_points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stock_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Goal = table.Column<int>(type: "integer", nullable: false),
                    AlertThreshold = table.Column<int>(type: "integer", nullable: false),
                    IsTracked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_items_collection_points_PointId",
                        column: x => x.PointId,
                        principalTable: "collection_points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "basket_template_items",
                columns: table => new
                {
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    basket_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_basket_template_items", x => new { x.basket_template_id, x.stock_item_id });
                    table.ForeignKey(
                        name: "FK_basket_template_items_basket_templates_basket_template_id",
                        column: x => x.basket_template_id,
                        principalTable: "basket_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_basket_template_items_stock_items_stock_item_id",
                        column: x => x.stock_item_id,
                        principalTable: "stock_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_withdrawals_basket_template_id",
                table: "withdrawals",
                column: "basket_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_basket_template_items_stock_item_id",
                table: "basket_template_items",
                column: "stock_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_basket_templates_PointId",
                table: "basket_templates",
                column: "PointId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_items_PointId_Name",
                table: "stock_items",
                columns: new[] { "PointId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_withdrawals_basket_templates_basket_template_id",
                table: "withdrawals",
                column: "basket_template_id",
                principalTable: "basket_templates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_withdrawals_basket_templates_basket_template_id",
                table: "withdrawals");

            migrationBuilder.DropTable(
                name: "basket_template_items");

            migrationBuilder.DropTable(
                name: "basket_templates");

            migrationBuilder.DropTable(
                name: "stock_items");

            migrationBuilder.DropIndex(
                name: "IX_withdrawals_basket_template_id",
                table: "withdrawals");

            migrationBuilder.DropColumn(
                name: "basket_quantity",
                table: "withdrawals");

            migrationBuilder.DropColumn(
                name: "basket_template_id",
                table: "withdrawals");

            migrationBuilder.AddColumn<string>(
                name: "stock",
                table: "collection_points",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "stock_settings",
                table: "collection_points",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");
        }
    }
}
