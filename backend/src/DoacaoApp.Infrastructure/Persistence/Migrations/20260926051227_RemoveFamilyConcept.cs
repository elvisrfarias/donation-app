using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFamilyConcept : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cycle_type",
                table: "beneficiaries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.AddColumn<int>(
                name: "cycle_max_withdrawals",
                table: "beneficiaries",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
                UPDATE beneficiaries AS b
                SET cycle_type = f.cycle_type,
                    cycle_max_withdrawals = f.cycle_max_withdrawals
                FROM families AS f
                WHERE f.""Id"" = b.""FamilyId"";");

            migrationBuilder.Sql(@"ALTER TABLE beneficiaries ALTER COLUMN cycle_type DROP DEFAULT;");
            migrationBuilder.Sql(@"ALTER TABLE beneficiaries ALTER COLUMN cycle_max_withdrawals DROP DEFAULT;");

            migrationBuilder.DropForeignKey(
                name: "FK_beneficiaries_families_FamilyId",
                table: "beneficiaries");

            migrationBuilder.DropTable(
                name: "families");

            migrationBuilder.DropIndex(
                name: "IX_withdrawals_FamilyId_OccurredAtUtc",
                table: "withdrawals");

            migrationBuilder.DropIndex(
                name: "IX_beneficiaries_FamilyId",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "withdrawals");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "beneficiaries");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawals_BeneficiaryId_OccurredAtUtc",
                table: "withdrawals",
                columns: new[] { "BeneficiaryId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_withdrawals_BeneficiaryId_OccurredAtUtc",
                table: "withdrawals");

            migrationBuilder.CreateTable(
                name: "families",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cycle_max_withdrawals = table.Column<int>(type: "integer", nullable: false),
                    cycle_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_families", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "beneficiaries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "withdrawals",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(@"UPDATE beneficiaries SET ""FamilyId"" = ""Id"";");

            migrationBuilder.Sql(@"
                INSERT INTO families (""Id"", ""Name"", ""CreatedAtUtc"", cycle_type, cycle_max_withdrawals)
                SELECT ""Id"", 'Família ' || ""Name"", ""CreatedAtUtc"", cycle_type, cycle_max_withdrawals
                FROM beneficiaries;");

            migrationBuilder.Sql(@"
                UPDATE withdrawals AS w
                SET ""FamilyId"" = b.""FamilyId""
                FROM beneficiaries AS b
                WHERE b.""Id"" = w.""BeneficiaryId"";");

            migrationBuilder.DropColumn(
                name: "cycle_max_withdrawals",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "cycle_type",
                table: "beneficiaries");

            migrationBuilder.CreateIndex(
                name: "IX_withdrawals_FamilyId_OccurredAtUtc",
                table: "withdrawals",
                columns: new[] { "FamilyId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_beneficiaries_FamilyId",
                table: "beneficiaries",
                column: "FamilyId");

            migrationBuilder.AddForeignKey(
                name: "FK_beneficiaries_families_FamilyId",
                table: "beneficiaries",
                column: "FamilyId",
                principalTable: "families",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
