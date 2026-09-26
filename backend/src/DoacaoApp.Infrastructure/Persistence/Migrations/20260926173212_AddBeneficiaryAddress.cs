using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaryAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "address_city",
                table: "beneficiaries",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_complement",
                table: "beneficiaries",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_district",
                table: "beneficiaries",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "address_latitude",
                table: "beneficiaries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "address_longitude",
                table: "beneficiaries",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_number",
                table: "beneficiaries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_state",
                table: "beneficiaries",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_street",
                table: "beneficiaries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "address_zipcode",
                table: "beneficiaries",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "address_city",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "address_complement",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "address_district",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "address_latitude",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "address_longitude",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "address_number",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "address_state",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "address_street",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "address_zipcode",
                table: "beneficiaries");
        }
    }
}
