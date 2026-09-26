using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DefaultWithdrawalCycleToAnnual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE collection_points SET withdrawal_cycle_type = 'Annual', withdrawal_cycle_max = 3 " +
                "WHERE withdrawal_cycle_type IN ('Monthly', 'Annual') AND withdrawal_cycle_max = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE collection_points SET withdrawal_cycle_type = 'Monthly', withdrawal_cycle_max = 1 " +
                "WHERE withdrawal_cycle_type = 'Annual' AND withdrawal_cycle_max = 3;");
        }
    }
}
