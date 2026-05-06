using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Needlr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase15_Moderation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "suspended_at",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "suspension_reason",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "user_warnings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by_admin_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_warnings", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_warnings_users_issued_by_admin_id",
                        column: x => x.issued_by_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_warnings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_warnings_issued_by_admin_id",
                table: "user_warnings",
                column: "issued_by_admin_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_warnings_user_id_issued_at",
                table: "user_warnings",
                columns: new[] { "user_id", "issued_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_warnings");

            migrationBuilder.DropColumn(
                name: "suspended_at",
                table: "users");

            migrationBuilder.DropColumn(
                name: "suspension_reason",
                table: "users");
        }
    }
}
