using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewsAndCollectionPointCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE collection_points ADD COLUMN IF NOT EXISTS address_latitude double precision;");

            migrationBuilder.Sql(
                "ALTER TABLE collection_points ADD COLUMN IF NOT EXISTS address_longitude double precision;");

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reviews_collection_points_collection_point_id",
                        column: x => x.collection_point_id,
                        principalTable: "collection_points",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_reviews_collection_point_id",
                table: "reviews",
                column: "collection_point_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.Sql(
                "ALTER TABLE collection_points DROP COLUMN IF EXISTS address_latitude;");

            migrationBuilder.Sql(
                "ALTER TABLE collection_points DROP COLUMN IF EXISTS address_longitude;");
        }
    }
}
