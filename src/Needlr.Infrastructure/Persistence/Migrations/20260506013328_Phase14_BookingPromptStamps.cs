using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Needlr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase14_BookingPromptStamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "healed_photo_prompted_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "reminder_sent_at",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "healed_photo_prompted_at",
                table: "bookings");

            migrationBuilder.DropColumn(
                name: "reminder_sent_at",
                table: "bookings");
        }
    }
}
