using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "categories",
                table: "withdrawals");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "accepted_items",
                table: "collection_points");

            migrationBuilder.AddColumn<Guid[]>(
                name: "category_ids",
                table: "withdrawals",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "stock_items",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PointId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Icon = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categories_collection_points_PointId",
                        column: x => x.PointId,
                        principalTable: "collection_points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_items_category_id",
                table: "stock_items",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_PointId_Name",
                table: "categories",
                columns: new[] { "PointId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_items_categories_category_id",
                table: "stock_items",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_stock_items_categories_category_id",
                table: "stock_items");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropIndex(
                name: "IX_stock_items_category_id",
                table: "stock_items");

            migrationBuilder.DropColumn(
                name: "category_ids",
                table: "withdrawals");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "stock_items");

            migrationBuilder.AddColumn<string[]>(
                name: "categories",
                table: "withdrawals",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "stock_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "accepted_items",
                table: "collection_points",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");
        }
    }
}
