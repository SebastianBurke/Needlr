using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Needlr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "jurisdictions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    requires_studio_inspection = table.Column<bool>(type: "boolean", nullable: false),
                    requires_artist_license = table.Column<bool>(type: "boolean", nullable: false),
                    requires_artist_hygiene_training = table.Column<bool>(type: "boolean", nullable: false),
                    requires_bloodborne_pathogen_cert = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jurisdictions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "studios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    studio_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    join_policy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_by_artist_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_studios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tattoo_styles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_canonical = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tattoo_styles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "studio_hours",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<string>(type: "text", nullable: false),
                    open_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    close_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_studio_hours", x => x.id);
                    table.ForeignKey(
                        name: "fk_studio_hours_studios_studio_id",
                        column: x => x.studio_id,
                        principalTable: "studios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    years_experience = table.Column<int>(type: "integer", nullable: false),
                    hourly_rate_cad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    shop_minimum_cad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    accepting_new_bookings = table.Column<bool>(type: "boolean", nullable: false),
                    payment_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stripe_connect_account_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cancellation_policy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artists", x => x.id);
                    table.ForeignKey(
                        name: "fk_artists_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    location = table.Column<Point>(type: "geometry(Point, 4326)", nullable: true),
                    preferred_search_radius_km = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_customer_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "studio_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jurisdiction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    document_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    issued_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    verification_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    verified_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_studio_credentials", x => x.id);
                    table.ForeignKey(
                        name: "fk_studio_credentials_jurisdictions_jurisdiction_id",
                        column: x => x.jurisdiction_id,
                        principalTable: "jurisdictions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_studio_credentials_studios_studio_id",
                        column: x => x.studio_id,
                        principalTable: "studios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_studio_credentials_users_verified_by_admin_id",
                        column: x => x.verified_by_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_claims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_logins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artist_availability_projections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_bookable = table.Column<bool>(type: "boolean", nullable: false),
                    remaining_session_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    recomputed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artist_availability_projections", x => x.id);
                    table.ForeignKey(
                        name: "fk_artist_availability_projections_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artist_credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jurisdiction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    credential_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    document_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    issued_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: false),
                    verification_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    verified_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    verified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artist_credentials", x => x.id);
                    table.ForeignKey(
                        name: "fk_artist_credentials_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artist_credentials_jurisdictions_jurisdiction_id",
                        column: x => x.jurisdiction_id,
                        principalTable: "jurisdictions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_artist_credentials_users_verified_by_admin_id",
                        column: x => x.verified_by_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "artist_lead_times",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    minimum_days = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artist_lead_times", x => x.id);
                    table.ForeignKey(
                        name: "fk_artist_lead_times_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artist_studio_affiliations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    affiliation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artist_studio_affiliations", x => x.id);
                    table.ForeignKey(
                        name: "fk_artist_studio_affiliations_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artist_studio_affiliations_studios_studio_id",
                        column: x => x.studio_id,
                        principalTable: "studios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "artist_styles",
                columns: table => new
                {
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    styles_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_artist_styles", x => new { x.artist_id, x.styles_id });
                    table.ForeignKey(
                        name: "fk_artist_styles_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_artist_styles_tattoo_styles_styles_id",
                        column: x => x.styles_id,
                        principalTable: "tattoo_styles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    max_session_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_availability_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_availability_overrides_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "availability_patterns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    day_of_week = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    max_session_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_until = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_availability_patterns", x => x.id);
                    table.ForeignKey(
                        name: "fk_availability_patterns_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_windows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    window_opens_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    window_closes_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    target_range_start = table.Column<DateOnly>(type: "date", nullable: false),
                    target_range_end = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_windows", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_windows_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    studio_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    requested_date = table.Column<DateOnly>(type: "date", nullable: false),
                    estimated_duration_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    body_placement = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approximate_size_cm = table.Column<int>(type: "integer", nullable: true),
                    estimated_total_cad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    deposit_amount_cad = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    deposit_captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_session_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancellation_policy_snapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    decline_reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    decline_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_attachments_purged = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bookings", x => x.id);
                    table.ForeignKey(
                        name: "fk_bookings_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_studios_studio_id",
                        column: x => x.studio_id,
                        principalTable: "studios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bookings_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_profile_preferred_styles",
                columns: table => new
                {
                    customer_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preferred_styles_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_profile_preferred_styles", x => new { x.customer_profile_id, x.preferred_styles_id });
                    table.ForeignKey(
                        name: "fk_customer_profile_preferred_styles_customer_profiles_custome",
                        column: x => x.customer_profile_id,
                        principalTable: "customer_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_profile_preferred_styles_tattoo_styles_preferred_s",
                        column: x => x.preferred_styles_id,
                        principalTable: "tattoo_styles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "booking_feedbacks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    communication_rating = table.Column<int>(type: "integer", nullable: false),
                    cleanliness_rating = table.Column<int>(type: "integer", nullable: false),
                    respected_design_brief_rating = table.Column<int>(type: "integer", nullable: false),
                    would_book_again = table.Column<bool>(type: "boolean", nullable: false),
                    free_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_feedbacks", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_feedbacks_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_feedbacks_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "message_threads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    locked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_threads", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_threads_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_pieces",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    artist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    body_placement = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    approximate_size_cm = table.Column<int>(type: "integer", nullable: true),
                    estimated_session_length_hours = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    year_completed = table.Column<int>(type: "integer", nullable: false),
                    progression_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    linked_booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    freeform_tags = table.Column<string[]>(type: "character varying(50)[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portfolio_pieces", x => x.id);
                    table.ForeignKey(
                        name: "fk_portfolio_pieces_artists_artist_id",
                        column: x => x.artist_id,
                        principalTable: "artists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_portfolio_pieces_bookings_linked_booking_id",
                        column: x => x.linked_booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    thread_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_reported_flag = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_messages_message_threads_thread_id",
                        column: x => x.thread_id,
                        principalTable: "message_threads",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_messages_users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "portfolio_piece_styles",
                columns: table => new
                {
                    portfolio_piece_id = table.Column<Guid>(type: "uuid", nullable: false),
                    styles_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_portfolio_piece_styles", x => new { x.portfolio_piece_id, x.styles_id });
                    table.ForeignKey(
                        name: "fk_portfolio_piece_styles_portfolio_pieces_portfolio_piece_id",
                        column: x => x.portfolio_piece_id,
                        principalTable: "portfolio_pieces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_portfolio_piece_styles_tattoo_styles_styles_id",
                        column: x => x.styles_id,
                        principalTable: "tattoo_styles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    portfolio_piece_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    photo_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    image_url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    linked_session_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_hidden = table.Column<bool>(type: "boolean", nullable: false),
                    hidden_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_session_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_session_photos_portfolio_pieces_portfolio_piece_id",
                        column: x => x.portfolio_piece_id,
                        principalTable: "portfolio_pieces",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_session_photos_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "booking_attachments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: true),
                    message_id = table.Column<Guid>(type: "uuid", nullable: true),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    original_filename = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_attachments_bookings_booking_id",
                        column: x => x.booking_id,
                        principalTable: "bookings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_attachments_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_attachments_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "message_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reported_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    resolved_by_admin_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_message_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_message_reports_messages_message_id",
                        column: x => x.message_id,
                        principalTable: "messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_message_reports_users_reported_by_user_id",
                        column: x => x.reported_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_message_reports_users_resolved_by_admin_id",
                        column: x => x.resolved_by_admin_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_artist_availability_projections_artist_id_date",
                table: "artist_availability_projections",
                columns: new[] { "artist_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_artist_availability_projections_date_is_bookable",
                table: "artist_availability_projections",
                columns: new[] { "date", "is_bookable" });

            migrationBuilder.CreateIndex(
                name: "ix_artist_credentials_artist_id_credential_type_verification_s",
                table: "artist_credentials",
                columns: new[] { "artist_id", "credential_type", "verification_status" });

            migrationBuilder.CreateIndex(
                name: "ix_artist_credentials_expiry_date",
                table: "artist_credentials",
                column: "expiry_date");

            migrationBuilder.CreateIndex(
                name: "ix_artist_credentials_jurisdiction_id",
                table: "artist_credentials",
                column: "jurisdiction_id");

            migrationBuilder.CreateIndex(
                name: "ix_artist_credentials_verified_by_admin_id",
                table: "artist_credentials",
                column: "verified_by_admin_id");

            migrationBuilder.CreateIndex(
                name: "ix_artist_lead_times_artist_id_booking_type",
                table: "artist_lead_times",
                columns: new[] { "artist_id", "booking_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_artist_studio_affiliations_artist_id_is_primary_status",
                table: "artist_studio_affiliations",
                columns: new[] { "artist_id", "is_primary", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_artist_studio_affiliations_studio_id_status",
                table: "artist_studio_affiliations",
                columns: new[] { "studio_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_artist_styles_styles_id",
                table: "artist_styles",
                column: "styles_id");

            migrationBuilder.CreateIndex(
                name: "ix_artists_accepting_new_bookings_payment_status",
                table: "artists",
                columns: new[] { "accepting_new_bookings", "payment_status" });

            migrationBuilder.CreateIndex(
                name: "ix_artists_user_id",
                table: "artists",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_availability_overrides_artist_id_date",
                table: "availability_overrides",
                columns: new[] { "artist_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_availability_patterns_artist_id_effective_from",
                table: "availability_patterns",
                columns: new[] { "artist_id", "effective_from" });

            migrationBuilder.CreateIndex(
                name: "ix_booking_attachments_booking_id",
                table: "booking_attachments",
                column: "booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_attachments_message_id",
                table: "booking_attachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_attachments_uploaded_by_user_id",
                table: "booking_attachments",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_feedbacks_booking_id",
                table: "booking_feedbacks",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_feedbacks_customer_id",
                table: "booking_feedbacks",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_windows_artist_id_window_opens_at_window_closes_at",
                table: "booking_windows",
                columns: new[] { "artist_id", "window_opens_at", "window_closes_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_artist_id_status",
                table: "bookings",
                columns: new[] { "artist_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_customer_id_status",
                table: "bookings",
                columns: new[] { "customer_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_is_attachments_purged_completed_at",
                table: "bookings",
                columns: new[] { "is_attachments_purged", "completed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status_confirmed_session_date",
                table: "bookings",
                columns: new[] { "status", "confirmed_session_date" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status_requested_at",
                table: "bookings",
                columns: new[] { "status", "requested_at" });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_studio_id",
                table: "bookings",
                column: "studio_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_profile_preferred_styles_preferred_styles_id",
                table: "customer_profile_preferred_styles",
                column: "preferred_styles_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_profiles_location",
                table: "customer_profiles",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_customer_profiles_user_id",
                table: "customer_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_jurisdictions_name",
                table: "jurisdictions",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_reports_message_id",
                table: "message_reports",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_reports_reported_at",
                table: "message_reports",
                column: "reported_at");

            migrationBuilder.CreateIndex(
                name: "ix_message_reports_reported_by_user_id",
                table: "message_reports",
                column: "reported_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_reports_resolution",
                table: "message_reports",
                column: "resolution");

            migrationBuilder.CreateIndex(
                name: "ix_message_reports_resolved_by_admin_id",
                table: "message_reports",
                column: "resolved_by_admin_id");

            migrationBuilder.CreateIndex(
                name: "ix_message_threads_booking_id",
                table: "message_threads",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_message_threads_status",
                table: "message_threads",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_messages_is_reported_flag",
                table: "messages",
                column: "is_reported_flag");

            migrationBuilder.CreateIndex(
                name: "ix_messages_sender_id",
                table: "messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_thread_id_sent_at",
                table: "messages",
                columns: new[] { "thread_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_piece_styles_styles_id",
                table: "portfolio_piece_styles",
                column: "styles_id");

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_pieces_artist_id_created_at",
                table: "portfolio_pieces",
                columns: new[] { "artist_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_pieces_body_placement",
                table: "portfolio_pieces",
                column: "body_placement");

            migrationBuilder.CreateIndex(
                name: "ix_portfolio_pieces_linked_booking_id",
                table: "portfolio_pieces",
                column: "linked_booking_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                table: "role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_session_photos_portfolio_piece_id_order",
                table: "session_photos",
                columns: new[] { "portfolio_piece_id", "order" });

            migrationBuilder.CreateIndex(
                name: "ix_session_photos_uploaded_by_user_id",
                table: "session_photos",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_studio_credentials_expiry_date",
                table: "studio_credentials",
                column: "expiry_date");

            migrationBuilder.CreateIndex(
                name: "ix_studio_credentials_jurisdiction_id",
                table: "studio_credentials",
                column: "jurisdiction_id");

            migrationBuilder.CreateIndex(
                name: "ix_studio_credentials_studio_id_credential_type_verification_s",
                table: "studio_credentials",
                columns: new[] { "studio_id", "credential_type", "verification_status" });

            migrationBuilder.CreateIndex(
                name: "ix_studio_credentials_verified_by_admin_id",
                table: "studio_credentials",
                column: "verified_by_admin_id");

            migrationBuilder.CreateIndex(
                name: "ix_studio_hours_studio_id_day_of_week",
                table: "studio_hours",
                columns: new[] { "studio_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_studios_location",
                table: "studios",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_studios_name",
                table: "studios",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_studios_studio_type",
                table: "studios",
                column: "studio_type");

            migrationBuilder.CreateIndex(
                name: "ix_tattoo_styles_is_canonical",
                table: "tattoo_styles",
                column: "is_canonical");

            migrationBuilder.CreateIndex(
                name: "ix_tattoo_styles_name",
                table: "tattoo_styles",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tattoo_styles_slug",
                table: "tattoo_styles",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                table: "user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                table: "user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "users",
                column: "normalized_user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "artist_availability_projections");

            migrationBuilder.DropTable(
                name: "artist_credentials");

            migrationBuilder.DropTable(
                name: "artist_lead_times");

            migrationBuilder.DropTable(
                name: "artist_studio_affiliations");

            migrationBuilder.DropTable(
                name: "artist_styles");

            migrationBuilder.DropTable(
                name: "availability_overrides");

            migrationBuilder.DropTable(
                name: "availability_patterns");

            migrationBuilder.DropTable(
                name: "booking_attachments");

            migrationBuilder.DropTable(
                name: "booking_feedbacks");

            migrationBuilder.DropTable(
                name: "booking_windows");

            migrationBuilder.DropTable(
                name: "customer_profile_preferred_styles");

            migrationBuilder.DropTable(
                name: "message_reports");

            migrationBuilder.DropTable(
                name: "portfolio_piece_styles");

            migrationBuilder.DropTable(
                name: "role_claims");

            migrationBuilder.DropTable(
                name: "session_photos");

            migrationBuilder.DropTable(
                name: "studio_credentials");

            migrationBuilder.DropTable(
                name: "studio_hours");

            migrationBuilder.DropTable(
                name: "user_claims");

            migrationBuilder.DropTable(
                name: "user_logins");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropTable(
                name: "customer_profiles");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "tattoo_styles");

            migrationBuilder.DropTable(
                name: "portfolio_pieces");

            migrationBuilder.DropTable(
                name: "jurisdictions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "message_threads");

            migrationBuilder.DropTable(
                name: "bookings");

            migrationBuilder.DropTable(
                name: "artists");

            migrationBuilder.DropTable(
                name: "studios");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
