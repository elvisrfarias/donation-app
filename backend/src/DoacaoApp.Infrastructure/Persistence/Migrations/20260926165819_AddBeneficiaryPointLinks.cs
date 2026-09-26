using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBeneficiaryPointLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "beneficiary_point_links",
                columns: table => new
                {
                    point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beneficiary_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_beneficiary_point_links", x => new { x.beneficiary_id, x.point_id });
                    table.ForeignKey(
                        name: "FK_beneficiary_point_links_beneficiaries_beneficiary_id",
                        column: x => x.beneficiary_id,
                        principalTable: "beneficiaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_beneficiary_point_links_point_id",
                table: "beneficiary_point_links",
                column: "point_id");

            migrationBuilder.Sql(
                "INSERT INTO beneficiary_point_links (beneficiary_id, point_id, linked_at_utc) " +
                "SELECT \"Id\", \"RegisteredByPointId\", \"CreatedAtUtc\" FROM beneficiaries;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "beneficiary_point_links");
        }
    }
}
