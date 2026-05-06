using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Needlr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase11_StripeProcessedEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stripe_processed_events",
                columns: table => new
                {
                    event_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stripe_processed_events", x => x.event_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stripe_processed_events_processed_at",
                table: "stripe_processed_events",
                column: "processed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stripe_processed_events");
        }
    }
}
