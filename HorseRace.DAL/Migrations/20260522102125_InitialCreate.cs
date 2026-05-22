using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HorseRace.DAL.Migrations
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
                    Name = table.Column<string>(type: "text", nullable: false),
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
                name: "PointWallets",
                columns: table => new
                {
                    WalletId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<int>(type: "integer", nullable: false),
                    IsFrozen = table.Column<bool>(type: "boolean", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointWallets", x => x.WalletId);
                    table.CheckConstraint("CK_PointWallets_Balance", "\"Balance\" >= 0");
                    table.ForeignKey(
                        name: "FK_PointWallets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
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
                name: "WalletTransactions",
                columns: table => new
                {
                    WalletTransactionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WalletId = table.Column<int>(type: "integer", nullable: false),
                    TransactionType = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    ReferenceType = table.Column<string>(type: "text", nullable: true),
                    ReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    PerformedBy = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.WalletTransactionId);
                    table.CheckConstraint("CK_WalletTransactions_BalanceAfter", "\"BalanceAfter\" >= 0");
                    table.ForeignKey(
                        name: "FK_WalletTransactions_PointWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "PointWallets",
                        principalColumn: "WalletId",
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
                    Status = table.Column<string>(type: "text", nullable: false),
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
                    Status = table.Column<string>(type: "text", nullable: false),
                    ConfirmationType = table.Column<string>(type: "text", nullable: true),
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
                name: "Predictions",
                columns: table => new
                {
                    PredictionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RaceId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    BetAmount = table.Column<int>(type: "integer", nullable: false),
                    FirstEntryId = table.Column<int>(type: "integer", nullable: false),
                    SecondEntryId = table.Column<int>(type: "integer", nullable: false),
                    ThirdEntryId = table.Column<int>(type: "integer", nullable: false),
                    OddsLocked1 = table.Column<decimal>(type: "numeric", nullable: false),
                    OddsLocked2 = table.Column<decimal>(type: "numeric", nullable: false),
                    OddsLocked3 = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Payout = table.Column<int>(type: "integer", nullable: true),
                    CorrectMatches = table.Column<int>(type: "integer", nullable: true),
                    PlacedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SettledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Predictions", x => x.PredictionId);
                    table.CheckConstraint("CK_Predictions_BetAmount", "\"BetAmount\" >= 1");
                    table.ForeignKey(
                        name: "FK_Predictions_Entry1",
                        column: x => x.FirstEntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId");
                    table.ForeignKey(
                        name: "FK_Predictions_Entry2",
                        column: x => x.SecondEntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId");
                    table.ForeignKey(
                        name: "FK_Predictions_Entry3",
                        column: x => x.ThirdEntryId,
                        principalTable: "Entries",
                        principalColumn: "EntryId");
                    table.ForeignKey(
                        name: "FK_Predictions_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "RaceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Predictions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
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
                    ResultStatus = table.Column<string>(type: "text", nullable: false),
                    LegPoints = table.Column<int>(type: "integer", nullable: false),
                    ConfirmationType = table.Column<string>(type: "text", nullable: false),
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
                    ResultStatus = table.Column<string>(type: "text", nullable: false),
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
                values: new object[] { 1, null, null, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@horserace.com", "System Admin", true, false, null, null, "$2a$11$nBZal3z4rIa2vWJW427jmOF/ExTYwIZmKB6AZzcu4SlcKdv6MXyNq", null, 5, null, null });

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
                name: "IX_PointWallets_UserId",
                table: "PointWallets",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_FirstEntryId",
                table: "Predictions",
                column: "FirstEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_RaceId_UserId",
                table: "Predictions",
                columns: new[] { "RaceId", "UserId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_SecondEntryId",
                table: "Predictions",
                column: "SecondEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_ThirdEntryId",
                table: "Predictions",
                column: "ThirdEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_UserId",
                table: "Predictions",
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
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

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
                name: "LegOfficialResults");

            migrationBuilder.DropTable(
                name: "LegRefereeEntries");

            migrationBuilder.DropTable(
                name: "PasswordResetOtps");

            migrationBuilder.DropTable(
                name: "Predictions");

            migrationBuilder.DropTable(
                name: "RaceResults");

            migrationBuilder.DropTable(
                name: "Violations");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "Entries");

            migrationBuilder.DropTable(
                name: "Legs");

            migrationBuilder.DropTable(
                name: "PointWallets");

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
