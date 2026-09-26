using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaryContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "beneficiaries",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phones",
                table: "beneficiaries",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "phones",
                table: "beneficiaries");
        }
    }
}
