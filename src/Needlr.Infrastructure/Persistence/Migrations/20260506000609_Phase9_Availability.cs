using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Needlr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase9_Availability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ical_token",
                table: "artists",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_artists_ical_token",
                table: "artists",
                column: "ical_token",
                unique: true,
                filter: "ical_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_artists_ical_token",
                table: "artists");

            migrationBuilder.DropColumn(
                name: "ical_token",
                table: "artists");
        }
    }
}
