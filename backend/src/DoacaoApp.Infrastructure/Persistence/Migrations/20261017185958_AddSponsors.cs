using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoacaoApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSponsors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sponsors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tier = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    website_url = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    logo_file_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sponsors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sponsors_stored_files_logo_file_id",
                        column: x => x.logo_file_id,
                        principalTable: "stored_files",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sponsors_is_active_tier",
                table: "sponsors",
                columns: new[] { "is_active", "tier" });

            migrationBuilder.CreateIndex(
                name: "IX_sponsors_logo_file_id",
                table: "sponsors",
                column: "logo_file_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sponsors");
        }
    }
}
