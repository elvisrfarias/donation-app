using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationsAndNotices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "donations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    donor_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    donor_phone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_donations_collection_points_collection_point_id",
                        column: x => x.collection_point_id,
                        principalTable: "collection_points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "point_notices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    audience = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    author_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_point_notices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_point_notices_collection_points_collection_point_id",
                        column: x => x.collection_point_id,
                        principalTable: "collection_points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_donations_collection_point_id_created_at_utc",
                table: "donations",
                columns: new[] { "collection_point_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_point_notices_collection_point_id_is_archived_created_at_utc",
                table: "point_notices",
                columns: new[] { "collection_point_id", "is_archived", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "donations");

            migrationBuilder.DropTable(
                name: "point_notices");
        }
    }
}
