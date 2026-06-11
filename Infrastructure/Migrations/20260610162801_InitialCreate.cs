using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    TournamentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CancelReason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.TournamentId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LicenseNumber = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<decimal>(type: "numeric", nullable: true),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    IsProfileComplete = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Horses",
                columns: table => new
                {
                    HorseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Breed = table.Column<string>(type: "text", nullable: true),
                    BirthYear = table.Column<int>(type: "integer", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ApprovedBy = table.Column<int>(type: "integer", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horses", x => x.HorseId);
                    table.ForeignKey(
                        name: "FK_Horses_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Horses_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JockeyProfiles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Weight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    Bio = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TotalRaces = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalWins = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalTop3 = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CareerPrizePoints = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JockeyProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_JockeyProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetOtps",
                columns: table => new
                {
                    OtpId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    OtpCode = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetOtps", x => x.OtpId);
                    table.ForeignKey(
                        name: "FK_PasswordResetOtps_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Races",
                columns: table => new
                {
                    RaceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TournamentId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ScheduledStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NumberOfLegs = table.Column<int>(type: "integer", nullable: false),
                    MaxHorses = table.Column<int>(type: "integer", nullable: false),
                    RoundType = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Referee1Id = table.Column<int>(type: "integer", nullable: true),
                    Referee2Id = table.Column<int>(type: "integer", nullable: true),
                    RegistrationOpenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RegistrationCloseAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OddsComputedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Races", x => x.RaceId);
                    table.CheckConstraint("CK_Races_DifferentReferees", "\"Referee1Id\" <> \"Referee2Id\"");
                    table.CheckConstraint("CK_Races_NumberOfLegs", "\"NumberOfLegs\" >= 1 AND \"NumberOfLegs\" <= 10");
                    table.ForeignKey(
                        name: "FK_Races_Referee1",
                        column: x => x.Referee1Id,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Races_Referee2",
                        column: x => x.Referee2Id,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Races_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Spectators",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Spectators", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Spectators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Entries",
                columns: table => new
                {
                    EntryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    HorseId = table.Column<int>(type: "integer", nullable: false),
                    JockeyId = table.Column<int>(type: "integer", nullable: false),
                    HorseOwnerId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    GateNumber = table.Column<int>(type: "integer", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entries", x => x.EntryId);
                    table.ForeignKey(
                        name: "FK_Entries_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Entries_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "HorseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entries_Jockey",
                        column: x => x.JockeyId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entries_Owner",
                        column: x => x.HorseOwnerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Entries_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "RaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JockeyInvitations",
                columns: table => new
                {
                    InvitationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HorseOwnerId = table.Column<int>(type: "integer", nullable: false),
                    JockeyId = table.Column<int>(type: "integer", nullable: false),
                    HorseId = table.Column<int>(type: "integer", nullable: false),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: true),
                    ResponseReason = table.Column<string>(type: "text", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JockeyInvitations", x => x.InvitationId);
                    table.ForeignKey(
                        name: "FK_JockeyInvitations_Horses_HorseId",
                        column: x => x.HorseId,
                        principalTable: "Horses",
                        principalColumn: "HorseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JockeyInvitations_Jockey",
                        column: x => x.JockeyId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JockeyInvitations_Owner",
                        column: x => x.HorseOwnerId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JockeyInvitations_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "RaceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Legs",
                columns: table => new
                {
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    LegNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ConfirmationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConflictReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminOverrideReason = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Legs", x => new { x.RaceId, x.LegNumber });
                    table.CheckConstraint("CK_Legs_LegNumber", "\"LegNumber\" >= 1 AND \"LegNumber\" <= 10");
                    table.ForeignKey(
                        name: "FK_Legs_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "RaceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SettlementRuns",
                columns: table => new
                {
                    SettlementRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TotalPredictions = table.Column<int>(type: "integer", nullable: false),
                    TotalBetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPayoutAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TriggeredByAdminId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementRuns", x => x.SettlementRunId);
                    table.ForeignKey(
                        name: "FK_SettlementRuns_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "RaceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SettlementRuns_TriggeredByAdmin",
                        column: x => x.TriggeredByAdminId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PointWallets",
                columns: table => new
                {
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpectatorId = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 100m),
                    IsFrozen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointWallets", x => x.WalletId);
                    table.CheckConstraint("CK_PointWallets_Balance", "\"Balance\" >= 0");
                    table.ForeignKey(
                        name: "FK_PointWallets_Spectators_SpectatorId",
                        column: x => x.SpectatorId,
                        principalTable: "Spectators",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Predictions",
                columns: table => new
                {
                    PredictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    SpectatorId = table.Column<int>(type: "integer", nullable: false),
                    FirstEntryId = table.Column<int>(type: "integer", nullable: false),
                    SecondEntryId = table.Column<int>(type: "integer", nullable: false),
                    ThirdEntryId = table.Column<int>(type: "integer", nullable: false),
                    BetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OddsLocked1 = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    OddsLocked2 = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    OddsLocked3 = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.PredictionId);
                    table.CheckConstraint("CK_Predictions_BetAmount", "\"BetAmount\" >= 10");
                    table.CheckConstraint("CK_Predictions_DifferentEntries", "\"FirstEntryId\" <> \"SecondEntryId\" AND \"FirstEntryId\" <> \"ThirdEntryId\" AND \"SecondEntryId\" <> \"ThirdEntryId\"");
                    table.ForeignKey(
                        name: "FK_Predictions_FirstEntry",
                        column: x => x.FirstEntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Predictions_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "RaceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Predictions_SecondEntry",
                        column: x => x.SecondEntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Predictions_Spectators_SpectatorId",
                        column: x => x.SpectatorId,
                        principalTable: "Spectators",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Predictions_ThirdEntry",
                        column: x => x.ThirdEntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RaceResults",
                columns: table => new
                {
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<int>(type: "integer", nullable: false),
                    TotalPoints = table.Column<int>(type: "integer", nullable: false),
                    FinalPosition = table.Column<int>(type: "integer", nullable: true),
                    IsRaceDQ = table.Column<bool>(type: "boolean", nullable: false),
                    LegWinCount = table.Column<int>(type: "integer", nullable: false),
                    LegTop3Count = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceResults", x => new { x.RaceId, x.EntryId });
                    table.ForeignKey(
                        name: "FK_RaceResults_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaceResults_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "RaceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LegOfficialResults",
                columns: table => new
                {
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    LegNumber = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<int>(type: "integer", nullable: false),
                    FinishPosition = table.Column<int>(type: "integer", nullable: true),
                    ResultStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LegPoints = table.Column<int>(type: "integer", nullable: false),
                    ConfirmationType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedByAdminId = table.Column<int>(type: "integer", nullable: true),
                    OverrideReason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegOfficialResults", x => new { x.RaceId, x.LegNumber, x.EntryId });
                    table.ForeignKey(
                        name: "FK_LegOfficialResults_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegOfficialResults_Legs_RaceId_LegNumber",
                        columns: x => new { x.RaceId, x.LegNumber },
                        principalTable: "Legs",
                        principalColumns: new[] { "RaceId", "LegNumber" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LegRefereeEntries",
                columns: table => new
                {
                    LegRefereeEntryId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    LegNumber = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<int>(type: "integer", nullable: false),
                    RefereeUserId = table.Column<int>(type: "integer", nullable: false),
                    FinishPosition = table.Column<int>(type: "integer", nullable: true),
                    ResultStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegRefereeEntries", x => x.LegRefereeEntryId);
                    table.ForeignKey(
                        name: "FK_LegRefereeEntries_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegRefereeEntries_Legs_RaceId_LegNumber",
                        columns: x => new { x.RaceId, x.LegNumber },
                        principalTable: "Legs",
                        principalColumns: new[] { "RaceId", "LegNumber" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LegRefereeEntries_Referee",
                        column: x => x.RefereeUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Violations",
                columns: table => new
                {
                    ViolationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    LegNumber = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<int>(type: "integer", nullable: false),
                    ReportedByRefereeId = table.Column<int>(type: "integer", nullable: false),
                    ViolationType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Penalty = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReviewedByAdminId = table.Column<int>(type: "integer", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Violations", x => x.ViolationId);
                    table.ForeignKey(
                        name: "FK_Violations_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Violations_Legs_RaceId_LegNumber",
                        columns: x => new { x.RaceId, x.LegNumber },
                        principalTable: "Legs",
                        principalColumns: new[] { "RaceId", "LegNumber" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Violations_Referee",
                        column: x => x.ReportedByRefereeId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    WalletTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpectatorId = table.Column<int>(type: "integer", nullable: false),
                    PredictionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SettlementRunId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdminId = table.Column<int>(type: "integer", nullable: true),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RollbackOfTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.WalletTransactionId);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Admin",
                        column: x => x.AdminId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_PointWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "PointWallets",
                        principalColumn: "WalletId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Predictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "Predictions",
                        principalColumn: "PredictionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_RollbackOfTransaction",
                        column: x => x.RollbackOfTransactionId,
                        principalTable: "WalletTransactions",
                        principalColumn: "WalletTransactionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_SettlementRuns_SettlementRunId",
                        column: x => x.SettlementRunId,
                        principalTable: "SettlementRuns",
                        principalColumn: "SettlementRunId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_Spectators_SpectatorId",
                        column: x => x.SpectatorId,
                        principalTable: "Spectators",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrizePointTransactions",
                columns: table => new
                {
                    PrizePointTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TournamentId = table.Column<int>(type: "integer", nullable: false),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    EntryId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FinalPosition = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RollbackOfId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrizePointTransactions", x => x.PrizePointTransactionId);
                    table.CheckConstraint("CK_PrizePointTransactions_FinalPosition", "\"FinalPosition\" >= 1");
                    table.CheckConstraint("CK_PrizePointTransactions_Points", "\"Points\" >= 0");
                    table.ForeignKey(
                        name: "FK_PrizePointTransactions_Entries_EntryId",
                        column: x => x.EntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrizePointTransactions_RaceResults_RaceId_EntryId",
                        columns: x => new { x.RaceId, x.EntryId },
                        principalTable: "RaceResults",
                        principalColumns: new[] { "RaceId", "EntryId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrizePointTransactions_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "RaceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrizePointTransactions_RollbackOf",
                        column: x => x.RollbackOfId,
                        principalTable: "PrizePointTransactions",
                        principalColumn: "PrizePointTransactionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrizePointTransactions_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "TournamentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrizePointTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PredictionSettlements",
                columns: table => new
                {
                    PredictionSettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    SettlementRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    PredictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    SpectatorId = table.Column<int>(type: "integer", nullable: false),
                    MatchedCount = table.Column<int>(type: "integer", nullable: false),
                    Outcome = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    BetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OddsAverage = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    PayoutAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PayoutTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RollbackOfSettlementId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsRollbacked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RollbackAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionSettlements", x => x.PredictionSettlementId);
                    table.CheckConstraint("CK_PredictionSettlements_MatchedCount", "\"MatchedCount\" >= 0 AND \"MatchedCount\" <= 3");
                    table.ForeignKey(
                        name: "FK_PredictionSettlements_PayoutTransaction",
                        column: x => x.PayoutTransactionId,
                        principalTable: "WalletTransactions",
                        principalColumn: "WalletTransactionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredictionSettlements_Predictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "Predictions",
                        principalColumn: "PredictionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredictionSettlements_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "RaceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredictionSettlements_RollbackOfSettlement",
                        column: x => x.RollbackOfSettlementId,
                        principalTable: "PredictionSettlements",
                        principalColumn: "PredictionSettlementId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredictionSettlements_SettlementRuns_SettlementRunId",
                        column: x => x.SettlementRunId,
                        principalTable: "SettlementRuns",
                        principalColumn: "SettlementRunId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PredictionSettlements_Spectators_SpectatorId",
                        column: x => x.SpectatorId,
                        principalTable: "Spectators",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "HORSE_OWNER", "Horse Owner" },
                    { 2, "JOCKEY", "Jockey" },
                    { 3, "REFEREE", "Race Referee" },
                    { 4, "SPECTATOR", "Spectator" },
                    { 5, "ADMIN", "Administrator" }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "AvatarUrl", "Bio", "CreatedAt", "Email", "FullName", "IsActive", "IsProfileComplete", "LicenseNumber", "LockedUntil", "PasswordHash", "PhoneNumber", "RoleId", "UpdatedAt", "Weight" },
                values: new object[] { 1, null, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@horserace.com", "System Admin", true, false, null, null, "$2a$12$QqfH5MsD9ZWO1A9UcXq/8edFa/8DR6cN4t.KfrUlOvl5F658ZJeZC", null, 5, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Entries_ApprovedBy",
                table: "Entries",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_HorseId",
                table: "Entries",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_HorseOwnerId",
                table: "Entries",
                column: "HorseOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_JockeyId",
                table: "Entries",
                column: "JockeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_RaceId_GateNumber",
                table: "Entries",
                columns: new[] { "RaceId", "GateNumber" },
                unique: true,
                filter: "\"GateNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Entries_RaceId_HorseId",
                table: "Entries",
                columns: new[] { "RaceId", "HorseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Entries_RaceId_JockeyId",
                table: "Entries",
                columns: new[] { "RaceId", "JockeyId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Horses_ApprovedBy",
                table: "Horses",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Horses_OwnerId",
                table: "Horses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_JockeyInvitations_HorseId",
                table: "JockeyInvitations",
                column: "HorseId");

            migrationBuilder.CreateIndex(
                name: "IX_JockeyInvitations_HorseOwnerId_JockeyId_HorseId_RaceId",
                table: "JockeyInvitations",
                columns: new[] { "HorseOwnerId", "JockeyId", "HorseId", "RaceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JockeyInvitations_JockeyId",
                table: "JockeyInvitations",
                column: "JockeyId");

            migrationBuilder.CreateIndex(
                name: "IX_JockeyInvitations_RaceId",
                table: "JockeyInvitations",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_LegOfficialResults_EntryId",
                table: "LegOfficialResults",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_LegRefereeEntries_EntryId",
                table: "LegRefereeEntries",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_LegRefereeEntries_RaceId_LegNumber_EntryId_RefereeUserId",
                table: "LegRefereeEntries",
                columns: new[] { "RaceId", "LegNumber", "EntryId", "RefereeUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LegRefereeEntries_RefereeUserId",
                table: "LegRefereeEntries",
                column: "RefereeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_UserId",
                table: "PasswordResetOtps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PointWallets_SpectatorId",
                table: "PointWallets",
                column: "SpectatorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_FirstEntryId",
                table: "Predictions",
                column: "FirstEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_RaceId",
                table: "Predictions",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_RaceId_SpectatorId_Status",
                table: "Predictions",
                columns: new[] { "RaceId", "SpectatorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_SecondEntryId",
                table: "Predictions",
                column: "SecondEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_SpectatorId",
                table: "Predictions",
                column: "SpectatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_Status",
                table: "Predictions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_ThirdEntryId",
                table: "Predictions",
                column: "ThirdEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionSettlements_Outcome",
                table: "PredictionSettlements",
                column: "Outcome");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionSettlements_PayoutTransactionId",
                table: "PredictionSettlements",
                column: "PayoutTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionSettlements_PredictionId",
                table: "PredictionSettlements",
                column: "PredictionId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionSettlements_RaceId",
                table: "PredictionSettlements",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionSettlements_RollbackOfSettlementId",
                table: "PredictionSettlements",
                column: "RollbackOfSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionSettlements_SettlementRunId",
                table: "PredictionSettlements",
                column: "SettlementRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionSettlements_SettlementRunId_PredictionId",
                table: "PredictionSettlements",
                columns: new[] { "SettlementRunId", "PredictionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PredictionSettlements_SpectatorId",
                table: "PredictionSettlements",
                column: "SpectatorId");

            migrationBuilder.CreateIndex(
                name: "IX_PrizePointTransactions_EntityType",
                table: "PrizePointTransactions",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_PrizePointTransactions_EntryId",
                table: "PrizePointTransactions",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PrizePointTransactions_RaceId",
                table: "PrizePointTransactions",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PrizePointTransactions_RaceId_EntryId_UserId_Type",
                table: "PrizePointTransactions",
                columns: new[] { "RaceId", "EntryId", "UserId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_PrizePointTransactions_RollbackOfId",
                table: "PrizePointTransactions",
                column: "RollbackOfId");

            migrationBuilder.CreateIndex(
                name: "IX_PrizePointTransactions_TournamentId",
                table: "PrizePointTransactions",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_PrizePointTransactions_Type",
                table: "PrizePointTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PrizePointTransactions_UserId",
                table: "PrizePointTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceResults_EntryId",
                table: "RaceResults",
                column: "EntryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Races_Referee1Id",
                table: "Races",
                column: "Referee1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Races_Referee2Id",
                table: "Races",
                column: "Referee2Id");

            migrationBuilder.CreateIndex(
                name: "IX_Races_TournamentId",
                table: "Races",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRuns_CreatedAt",
                table: "SettlementRuns",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRuns_RaceId",
                table: "SettlementRuns",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRuns_Status",
                table: "SettlementRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRuns_TriggeredByAdminId",
                table: "SettlementRuns",
                column: "TriggeredByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_SettlementRuns_Type",
                table: "SettlementRuns",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_LicenseNumber",
                table: "Users",
                column: "LicenseNumber",
                unique: true,
                filter: "\"LicenseNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_EntryId",
                table: "Violations",
                column: "EntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Violations_RaceId_LegNumber",
                table: "Violations",
                columns: new[] { "RaceId", "LegNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Violations_ReportedByRefereeId",
                table: "Violations",
                column: "ReportedByRefereeId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_AdminId",
                table: "WalletTransactions",
                column: "AdminId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_CreatedAt",
                table: "WalletTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_PredictionId",
                table: "WalletTransactions",
                column: "PredictionId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_RollbackOfTransactionId",
                table: "WalletTransactions",
                column: "RollbackOfTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_SettlementRunId",
                table: "WalletTransactions",
                column: "SettlementRunId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_SpectatorId",
                table: "WalletTransactions",
                column: "SpectatorId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_Type",
                table: "WalletTransactions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletId",
                table: "WalletTransactions",
                column: "WalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JockeyInvitations");

            migrationBuilder.DropTable(
                name: "JockeyProfiles");

            migrationBuilder.DropTable(
                name: "LegOfficialResults");

            migrationBuilder.DropTable(
                name: "LegRefereeEntries");

            migrationBuilder.DropTable(
                name: "PasswordResetOtps");

            migrationBuilder.DropTable(
                name: "PredictionSettlements");

            migrationBuilder.DropTable(
                name: "PrizePointTransactions");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Violations");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "RaceResults");

            migrationBuilder.DropTable(
                name: "Legs");

            migrationBuilder.DropTable(
                name: "PointWallets");

            migrationBuilder.DropTable(
                name: "Predictions");

            migrationBuilder.DropTable(
                name: "SettlementRuns");

            migrationBuilder.DropTable(
                name: "Entries");

            migrationBuilder.DropTable(
                name: "Spectators");

            migrationBuilder.DropTable(
                name: "Horses");

            migrationBuilder.DropTable(
                name: "Races");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Tournaments");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
