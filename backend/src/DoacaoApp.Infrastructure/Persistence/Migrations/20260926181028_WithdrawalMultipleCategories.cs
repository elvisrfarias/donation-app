using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WithdrawalMultipleCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "categories",
                table: "withdrawals",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);

            migrationBuilder.Sql("UPDATE withdrawals SET categories = ARRAY[\"Category\"];");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "withdrawals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "withdrawals",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Food");

            migrationBuilder.Sql(
                "UPDATE withdrawals SET \"Category\" = COALESCE(categories[1], 'Food');");

            migrationBuilder.DropColumn(
                name: "categories",
                table: "withdrawals");
        }
    }
}
