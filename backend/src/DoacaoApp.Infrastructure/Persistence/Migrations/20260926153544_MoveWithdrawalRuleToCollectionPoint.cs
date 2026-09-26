using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MoveWithdrawalRuleToCollectionPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "withdrawal_cycle_type",
                table: "collection_points",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.AddColumn<int>(
                name: "withdrawal_cycle_max",
                table: "collection_points",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql(@"
                UPDATE collection_points AS p
                SET withdrawal_cycle_type = s.cycle_type,
                    withdrawal_cycle_max = s.cycle_max_withdrawals
                FROM (
                    SELECT DISTINCT ON (""RegisteredByPointId"")
                        ""RegisteredByPointId"", cycle_type, cycle_max_withdrawals
                    FROM beneficiaries
                    ORDER BY ""RegisteredByPointId"", cycle_max_withdrawals DESC
                ) AS s
                WHERE s.""RegisteredByPointId"" = p.""Id"";");

            migrationBuilder.Sql(@"ALTER TABLE collection_points ALTER COLUMN withdrawal_cycle_type DROP DEFAULT;");
            migrationBuilder.Sql(@"ALTER TABLE collection_points ALTER COLUMN withdrawal_cycle_max DROP DEFAULT;");

            migrationBuilder.DropColumn(
                name: "cycle_max_withdrawals",
                table: "beneficiaries");

            migrationBuilder.DropColumn(
                name: "cycle_type",
                table: "beneficiaries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cycle_max_withdrawals",
                table: "beneficiaries",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "cycle_type",
                table: "beneficiaries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.Sql(@"
                UPDATE beneficiaries AS b
                SET cycle_type = p.withdrawal_cycle_type,
                    cycle_max_withdrawals = p.withdrawal_cycle_max
                FROM collection_points AS p
                WHERE p.""Id"" = b.""RegisteredByPointId"";");

            migrationBuilder.Sql(@"ALTER TABLE beneficiaries ALTER COLUMN cycle_type DROP DEFAULT;");
            migrationBuilder.Sql(@"ALTER TABLE beneficiaries ALTER COLUMN cycle_max_withdrawals DROP DEFAULT;");

            migrationBuilder.DropColumn(
                name: "withdrawal_cycle_max",
                table: "collection_points");

            migrationBuilder.DropColumn(
                name: "withdrawal_cycle_type",
                table: "collection_points");
        }
    }
}
