# PROJECT CONTEXT

## 1. Project Overview

`HorseRaceManagementSystem` is a .NET 10.0 Web API implementing Clean Architecture with separate `Api`, `Application`, `Infrastructure`, `Domain`, and `Sharekernel` layers. It uses MediatR for CQRS-style request handling, Entity Framework Core with PostgreSQL for persistence, JWT for authentication, and a lightweight domain model for horse racing management.

Components:
- `Api/`: ASP.NET Core Web API controllers and startup configuration.
- `Application/`: Use cases, MediatR commands and queries, validation, behaviors.
- `Infrastructure/`: EF Core `ApplicationDbContext`, repository implementations, services, DI registration, migrations.
- `Domain/`: aggregate entities, constants, and relationships.
- `Sharekernel/`: shared repository and unit-of-work abstractions.

## 2. Architecture Overview

The solution uses Clean Architecture patterns:
- `Api` depends on `Application` and `Infrastructure`.
- `Application` contains request definitions, handlers, and shared interfaces.
- `Infrastructure` contains EF Core persistence, configuration, and service implementations.
- `Domain` contains entity definitions and navigation properties.

MediatR is configured in `Application/DependencyInjection/ServiceCollectionExtensions.cs`, with pipeline behaviors:
- `Application/Common/LoggingBehavior.cs`
- `Application/Common/UnitOfWorkBehavior.cs`

`Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` registers:
- `ApplicationDbContext` as `IApplicationDbContext` and `IUnitOfWork`
- `IRepository<,>` with `EfRepository<,>`
- domain-specific repositories and services such as `IUserRepository`, `IRefreshTokenRepository`, `IJwtTokenService`, `IPasswordHasher`, `IRaceResultReadService`, `ITournamentReadService`, `IRaceReadService`, `IEntryReadService`, `IHorseRepository`, `IEntryRepository`.

The API config in `Api/Program.cs` includes:
- CORS policy `FrontendPolicy`
- Swagger with Bearer JWT security definition
- JWT Bearer authentication
- Authorization registration
- Global exception handling via `Api/Middlewares/GlobalExceptionHandler.cs`
- EF Core migration and seeding logic

## 3. Folder Structure

Root:
- `Api/`
- `Application/`
- `Domain/`
- `Infrastructure/`
- `Sharekernel/`

Major folders:
- `Api/Controllers/`
- `Application/Usecases/`
- `Domain/Aggregates/Entities/`
- `Infrastructure/Data/`
- `Infrastructure/Data/Configurations/`
- `Infrastructure/Repositories/`
- `Infrastructure/Services/`
- `Sharekernel/Repository/`
- `Sharekernel/UnitOfWork/`

## 4. Domain Entities

Entities present in `Domain/Aggregates/Entities/`:
- `User`
- `Role`
- `Horse`
- `Tournament`
- `Race`
- `Leg`
- `JockeyInvitation`
- `Entry`
- `LegRefereeEntry`
- `LegOfficialResult`
- `Violation`
- `RaceResult`
- `Prediction`
- `PredictionSettlement`
- `SettlementRun`
- `Spectator`
- `PointWallet`
- `WalletTransaction`
- `PrizePointTransaction`
- `RefreshToken`
- `PasswordResetOtp`
- `JockeyProfile`

Key domain-specific status fields:
- `Horse.Status` = `Pending | Approved | Rejected`
- `Entry.Status` = `Pending | Approved | Rejected | Cancelled`
- `Race.Status` = `Scheduled | InProgress | Paused | PendingResult | Finished | Cancelled`
- `Leg.Status` = `Pending | AwaitingSecondReferee | Confirmed | Conflicted | Resolved`
- `Violation.Status` = `Pending | Approved | Rejected`
- `Prediction.Status` = `Pending` (as implemented)
- `SettlementRun.Type` = `Publish` by default

## 5. Entity Relationships

Relationships defined by navigation properties and EF configurations:
- `User` has many `OwnedHorses`, `SentInvitations`, `ReceivedInvitations`.
- `Horse` belongs to `Owner` and has many `Entries` and `Invitations`.
- `Race` belongs to `Tournament`, has optional `Referee1` and `Referee2`, and contains `Legs`, `Entries`, `RaceResults`, `Predictions`.
- `Leg` belongs to `Race` and has many `RefereeEntries`, `OfficialResults`, `Violations`.
- `JockeyInvitation` belongs to `HorseOwner`, `Jockey`, `Horse`, `Race`.
- `Entry` belongs to `Race`, `Horse`, `Jockey`, `HorseOwner`; has `RaceResults`, `LegRefereeEntries`, `LegOfficialResults`.
- `LegRefereeEntry` belongs to `Leg`, `Entry`, `Referee`.
- `LegOfficialResult` belongs to `Leg`, `Entry`.
- `Violation` belongs to `Leg`, `Entry`, `ReportedByReferee`.
- `Prediction` belongs to `Race`, `Spectator`, `FirstEntry`, `SecondEntry`, `ThirdEntry`.
- `RaceResult` belongs to `Race`, `Entry`.
- `SettlementRun` belongs to `Race`; has many `PredictionSettlements`.
- `PredictionSettlement` belongs to `SettlementRun` and `Prediction`.
- `Spectator` belongs to `User`; has `PointWallet`, `Predictions`, `PredictionSettlements`.

Notable EF constraints:
- Unique `Entry` indexes on `(RaceId, HorseId)` and `(RaceId, JockeyId)`.
- Unique `JockeyInvitation` index on `(HorseOwnerId, JockeyId, HorseId, RaceId)`.
- Unique `LegRefereeEntry` index on `(RaceId, LegNumber, EntryId, RefereeUserId)`.
- `Prediction` enforces distinct first/second/third entries and minimum bet amount >= 10.
- `Race` check constraint enforces distinct referees and `NumberOfLegs` between 1 and 10.

## 6. CQRS Commands

Commands are implemented under `Application/Usecases/*/*Command.cs` and handlers under `*CommandHandler.cs`.

Important command areas:
- `Admin`: `ApproveUserCommand`, `RejectUserCommand`, `GetPendingUsersQuery`.
- `Admin/Horse`: `ApproveHorseCommand`, `RejectHorseCommand`, `RevokeHorseCommand`, `GetPendingHorsesQuery`.
- `Admin/Entry`: `ApproveEntryCommand`, `RejectEntryCommand`, `GetPendingEntriesQuery`.
- `Auth`: `RegisterCommand`, `LoginCommand`, `LogoutCommand`, `RefreshTokenCommand`.
- `Horses`: `CreateHorseCommand`, `UpdateHorseCommand`, `DeleteHorseCommand`.
- `JockeyInvitations`: `CreateJockeyInvitationCommand`, `UpdateJockeyInvitationCommand`, `DeleteJockeyInvitationCommand`.
- `Entries`: `CreateEntryCommand`, `UpdateEntryCommand`, `DeleteEntryCommand`.
- `Races`: `CreateRaceCommand`, `UpdateRaceCommand`, `DeleteRaceCommand`.
- `Legs`: `CreateLegCommand`, `UpdateLegCommand`, `DeleteLegCommand`.
- `LegRefereeEntries`: `CreateLegRefereeEntryCommand`, `UpdateLegRefereeEntryCommand`, `DeleteLegRefereeEntryCommand`.
- `LegOfficialResults`: `CreateLegOfficialResultCommand`, `UpdateLegOfficialResultCommand`, `DeleteLegOfficialResultCommand`.
- `Violations`: `CreateViolationCommand`, `UpdateViolationCommand`, `DeleteViolationCommand`.
- `Predictions`: `CreatePredictionCommand`, `UpdatePredictionCommand`, `DeletePredictionCommand`.
- `SettlementRuns`: `CreateSettlementRunCommand`, `UpdateSettlementRunCommand`, `DeleteSettlementRunCommand`.
- `PredictionSettlements`: `CreatePredictionSettlementCommand`, `UpdatePredictionSettlementCommand`, `DeletePredictionSettlementCommand`.
- `RaceResults`: `CreateRaceResultCommand`, `UpdateRaceResultCommand`, `DeleteRaceResultCommand`.
- `PointWallets`, `WalletTransactions`, `PrizePointTransactions`, `Spectators`, `Users`, `Roles`, `Tournaments` all expose CRUD commands.

## 7. CQRS Queries

Queries are implemented under `Application/Usecases/*/*Query.cs` and handlers under `*QueryHandler.cs`.

Important query areas:
- `Admin`: `GetPendingUsersQuery`, `GetPendingHorsesQuery`, `GetPendingEntriesQuery`.
- `Horses`: `GetHorseListQuery`, `GetHorseDetailQuery`.
- `Entries`: `GetEntryListQuery`, `GetEntryDetailQuery`.
- `JockeyInvitations`: `GetJockeyInvitationListQuery`, `GetJockeyInvitationDetailQuery`.
- `Races`: `GetRaceListQuery`, `GetRaceDetailQuery`.
- `Legs`: `GetLegListQuery`, `GetLegDetailQuery`.
- `Predictions`: `GetPredictionListQuery`, `GetPredictionDetailQuery`.
- `Violations`: `GetViolationListQuery`, `GetViolationDetailQuery`.
- `SettlementRuns`: `GetSettlementRunListQuery`, `GetSettlementRunDetailQuery`.
- `PredictionSettlements`, `RaceResults`, `PointWallets`, `WalletTransactions`, `PrizePointTransactions`, `Spectators`, `Users`, `Roles`, `Tournaments`, `JockeyProfiles` all support list/detail queries.

## 8. MediatR Handlers

Handler registration uses `services.AddMediatR(...)` and scans the `Application` assembly.

Pipeline behaviors:
- `LoggingBehavior<TRequest,TResponse>` logs each request entry and exit.
- `UnitOfWorkBehavior<TRequest,TResponse>` calls `IUnitOfWork.SaveChangesAsync` after commands implementing `ICommand<TResponse>` or `ICommand`.

Command handlers use `IApplicationDbContext` or repositories to perform persistence.
Query handlers use `IApplicationDbContext` for read operations.

Examples:
- `Application/Usecases/Admin/ApproveHorse/ApproveHorseCommandHandler.cs`
- `Application/Usecases/Entries/CreateEntry/CreateEntryCommandHandler.cs`
- `Application/Usecases/JockeyInvitations/UpdateJockeyInvitation/UpdateJockeyInvitationCommandHandler.cs`
- `Application/Usecases/Predictions/CreatePrediction/CreatePredictionCommandHandler.cs`

## 9. API Endpoints

Routes are defined in controllers under `Api/Controllers/`.

### Auth
- `POST api/auth/register/spectator`
- `POST api/auth/register/horse-owner`
- `POST api/auth/register/jockey`
- `POST api/auth/login`
- `POST api/auth/logout` (`[Authorize]`)
- `POST api/auth/refresh-token`

### Admin (`[Authorize(Roles = "ADMIN")]`)
- `GET api/admin/users/pending`
- `POST api/admin/users/{id:int}/approve`
- `POST api/admin/users/{id:int}/reject`
- `GET api/admin/horses/pending`
- `POST api/admin/horses/{id:int}/approve`
- `POST api/admin/horses/{id:int}/reject`
- `POST api/admin/horses/{id:int}/revoke`
- `GET api/admin/entries/pending`
- `POST api/admin/entries/{id:int}/approve`
- `POST api/admin/entries/{id:int}/reject`

### Horses
- `POST api/horses`
- `GET api/horses`
- `GET api/horses/{horseId:int}`
- `PUT api/horses/{horseId:int}`
- `DELETE api/horses/{horseId:int}`

### Entries
- `POST api/entries`
- `GET api/entries/{entryId:int}`
- `GET api/entries`
- `PUT api/entries/{entryId:int}`
- `DELETE api/entries/{entryId:int}`

### Jockey Invitations
- `POST api/jockey-invitations`
- `GET api/jockey-invitations/{invitationId:int}`
- `GET api/jockey-invitations`
- `PUT api/jockey-invitations/{invitationId:int}`
- `DELETE api/jockey-invitations/{invitationId:int}`

### Races
- `POST api/races`
- `GET api/races/{raceId:int}`
- `GET api/races`
- `PUT api/races/{raceId:int}`
- `DELETE api/races/{raceId:int}`

### Legs
- `POST api/legs`
- `GET api/legs/{raceId:int}/{legNumber:int}`
- `GET api/legs`
- `PUT api/legs/{raceId:int}/{legNumber:int}`
- `DELETE api/legs/{raceId:int}/{legNumber:int}`

### Violations
- `POST api/violations`
- `GET api/violations/{violationId:int}`
- `GET api/violations`
- `PUT api/violations/{violationId:int}`
- `DELETE api/violations/{violationId:int}`

### Predictions
- `POST api/predictions`
- `GET api/predictions/{predictionId:int}`
- `GET api/predictions`
- `PUT api/predictions/{predictionId:int}`
- `DELETE api/predictions/{predictionId:int}`

### Race Results
- `POST api/race-results`
- `GET api/race-results/{raceId:int}/{entryId:int}`
- `GET api/race-results`
- `PUT api/race-results/{raceId:int}/{entryId:int}`
- `DELETE api/race-results/{raceId:int}/{entryId:int}`

### Settlement Runs
- `POST api/settlement-runs`
- `GET api/settlement-runs/{settlementRunId:int}`
- `GET api/settlement-runs`
- `PUT api/settlement-runs/{settlementRunId:int}`
- `DELETE api/settlement-runs/{settlementRunId:int}`

### Prediction Settlements
- `POST api/prediction-settlements`
- `GET api/prediction-settlements/{predictionSettlementId:int}`
- `GET api/prediction-settlements`
- `PUT api/prediction-settlements/{predictionSettlementId:int}`
- `DELETE api/prediction-settlements/{predictionSettlementId:int}`

### Wallet & Points
- `POST api/wallet-transactions`
- `GET api/wallet-transactions/{transactionId:int}`
- `GET api/wallet-transactions`
- `PUT api/wallet-transactions/{transactionId:int}`
- `DELETE api/wallet-transactions/{transactionId:int}`
- `POST api/point-wallets`
- `GET api/point-wallets/{walletId:int}`
- `GET api/point-wallets`
- `PUT api/point-wallets/{walletId:int}`
- `DELETE api/point-wallets/{walletId:int}`

### Other CRUD endpoints
- `api/users`, `api/roles`, `api/tournaments`, `api/spectators`, `api/jockey-profiles`, `api/prize-point-transactions`

## 10. Authentication & Authorization

Authentication is JWT Bearer token-based, configured in `Api/Program.cs` with:
- issuer, audience, secret key from configuration section `JwtSettings`
- token validation for issuer, audience, lifetime, signing key
- custom challenge and forbidden responses using `ProblemDetails`

Authorization is enabled globally, but only specific controllers require it:
- `Api/Controllers/AdminController.cs` requires `[Authorize(Roles = "ADMIN")]`
- `AuthController.Logout` requires `[Authorize]`

Other controllers have no `[Authorize]` attributes in the source, so they are currently accessible without role-based enforcement by default.

User registration and login use:
- `Application/Usecases/Auth/Register/RegisterCommandHandler.cs`
- `Application/Usecases/Auth/Login/LoginCommandHandler.cs`
- `Application/Usecases/Auth/RefreshToken/RefreshTokenCommandHandler.cs`
- `Application/Usecases/Auth/Logout/LogoutCommandHandler.cs`

Self-registration supports:
- `SPECTATOR` (active immediately)
- `HORSE_OWNER` (pending admin approval)
- `JOCKEY` (pending admin approval, requires license number and weight)

## 11. Database Schema

`ApplicationDbContext` exposes DbSets for all entities.

Key tables and columns derived from entity definitions and EF configuration:
- `Users`: identity fields, `RoleId`, `IsActive`, `Status`, `LockedUntil`, jockey-specific `LicenseNumber`, `Weight`, `Bio`.
- `Roles`: role codes and metadata.
- `Horses`: owner reference, `Status`, `RejectionReason`, approval metadata.
- `Tournaments`: dates, status, description.
- `Races`: tournament reference, `ScheduledStartTime`, `NumberOfLegs`, `MaxHorses`, `RoundType`, `Status`, `Referee1Id`, `Referee2Id`, `RegistrationOpenAt`, `RegistrationCloseAt`, `OddsComputedAt`, `PublishedAt`.
- `Legs`: composite PK `(RaceId, LegNumber)`, status, conflict/admin override metadata.
- `JockeyInvitations`: required unique invitation per `(HorseOwnerId, JockeyId, HorseId, RaceId)`.
- `Entries`: unique per `(RaceId, HorseId)` and `(RaceId, JockeyId)`.
- `LegRefereeEntries`: unique per `(RaceId, LegNumber, EntryId, RefereeUserId)`.
- `LegOfficialResults`: composite key `(RaceId, LegNumber, EntryId)`.
- `Predictions`: bet amount, locked odds, distinct entry validation.
- `RaceResults`: composite key `(RaceId, EntryId)`.
- `SettlementRuns`: run metadata and settlement totals.
- `PredictionSettlements`: settlement metadata for predictions.
- `Violations`: referee report details and admin review fields.

Migrations are present in `Infrastructure/Migrations/20260624052923_InitialCreate.*`.

## 12. Business Flows Mapping

### Horse Registration & Approval

Related entities:
- `Horse`
- `User`
- `Entry`

Related endpoints:
- `POST api/horses`
- `GET api/admin/horses/pending`
- `POST api/admin/horses/{id}/approve`
- `POST api/admin/horses/{id}/reject`
- `POST api/admin/horses/{id}/revoke`

Handlers:
- `Application/Usecases/Horses/CreateHorse/CreateHorseCommandHandler.cs`
- `Application/Usecases/Admin/ApproveHorse/ApproveHorseCommandHandler.cs`
- `Application/Usecases/Admin/RejectHorse/RejectHorseCommandHandler.cs`
- `Application/Usecases/Admin/RevokeHorse/RevokeHorseCommandHandler.cs`
- `Application/Usecases/Admin/GetPendingHorses/GetPendingHorsesQueryHandler.cs`

Implementation details:
- Horse creation sets `Status = Pending`.
- Approval sets `Status = Approved`, `ApprovedAt`, `ApprovedBy`.
- Rejection sets `Status = Rejected` and stores `RejectionReason`.
- Revocation requires an approved horse and sets status to `Rejected` while cancelling all related entries by setting `Entry.Status = Cancelled`.

Missing or deviated steps:
- There is no endpoint or handler for horse owner search or owner-side approval flows beyond the admin endpoints.
- The intended approval/rejection reason flow is implemented for horses, but owner-specific review and explicit revocation workflow is admin-only.

### Jockey Invitation & Entry Submission

Related entities:
- `JockeyInvitation`
- `Entry`
- `Horse`
- `Race`
- `User`

Related endpoints:
- `POST api/jockey-invitations`
- `PUT api/jockey-invitations/{id}`
- `DELETE api/jockey-invitations/{id}`
- `POST api/entries`
- `PUT api/entries/{id}`
- `DELETE api/entries/{id}`

Handlers:
- `Application/Usecases/JockeyInvitations/CreateJockeyInvitation/CreateJockeyInvitationCommandHandler.cs`
- `Application/Usecases/JockeyInvitations/UpdateJockeyInvitation/UpdateJockeyInvitationCommandHandler.cs`
- `Application/Usecases/JockeyInvitations/DeleteJockeyInvitation/DeleteJockeyInvitationCommandHandler.cs`
- `Application/Usecases/Entries/CreateEntry/CreateEntryCommandHandler.cs`
- `Application/Usecases/Entries/UpdateEntry/UpdateEntryCommandHandler.cs`
- `Application/Usecases/Entries/DeleteEntry/DeleteEntryCommandHandler.cs`

Implementation details:
- Invitation creation is possible and stored with `Status = Pending`.
- Invitation status transitions are validated in `UpdateJockeyInvitationCommandHandler`, including allowed transitions from `Pending` to `Accepted`/`Declined`/`Cancelled` and from `Accepted` to `Confirmed`/`Cancelled`.
- Entry creation validates required IDs and stores `Status = Pending`, `SubmittedAt`, `CreatedAt`.
- Admin approval assigns a gate number and sets `Status = Approved`.
- Admin rejection sets `Status = Rejected` and stores `RejectionReason`.

Missing or deviated steps:
- There is no dedicated "search jockeys" endpoint.
- There is no automatic workflow to confirm exactly one jockey and cancel others.
- There is no system logic for auto-rejecting pending entries when registration closes.
- Odds calculation and odds locking are not implemented.

### Tournament & Race Setup

Related entities:
- `Tournament`
- `Race`
- `User` (referees)
- `Leg`

Related endpoints:
- `POST api/tournaments`
- `PUT api/tournaments/{id}`
- `POST api/races`
- `PUT api/races/{id}`
- `POST api/legs`
- `PUT api/legs/{raceId}/{legNumber}`

Handlers:
- `Application/Usecases/Tournaments/CreateTournament/CreateTournamentCommandHandler.cs`
- `Application/Usecases/Tournaments/UpdateTournament/UpdateTournamentCommandHandler.cs`
- `Application/Usecases/Races/CreateRace/CreateRaceCommandHandler.cs`
- `Application/Usecases/Races/UpdateRace/UpdateRaceCommandHandler.cs`
- `Application/Usecases/Legs/CreateLeg/CreateLegCommandHandler.cs`
- `Application/Usecases/Legs/UpdateLeg/UpdateLegCommandHandler.cs`

Implementation details:
- Race creation requires two distinct referees and validates this rule.
- Race fields include `RegistrationOpenAt`, `RegistrationCloseAt`, `OddsComputedAt`, and `PublishedAt` on the entity, but these are not set by any dedicated business commands.
- Leg creation supports composite key `(RaceId, LegNumber)`.

Missing or deviated steps:
- No explicit registration open/close operations.
- No dedicated workflow for opening or closing race registration.
- `RegistrationOpenAt`, `RegistrationCloseAt`, and `OddsComputedAt` are fields only; no command updates these timestamps automatically.

### Race Execution

Related entities:
- `Race`
- `Leg`
- `LegRefereeEntry`
- `LegOfficialResult`
- `Entry`

Related endpoints:
- `POST api/leg-referee-entries`
- `POST api/leg-official-results`
- `PUT api/legs/{raceId}/{legNumber}`

Handlers:
- `Application/Usecases/LegRefereeEntries/CreateLegRefereeEntry/CreateLegRefereeEntryCommandHandler.cs`
- `Application/Usecases/LegOfficialResults/CreateLegOfficialResult/CreateLegOfficialResultCommandHandler.cs`
- `Application/Usecases/LegOfficialResults/UpdateLegOfficialResult/UpdateLegOfficialResultCommandHandler.cs`
- `Application/Usecases/Legs/UpdateLeg/UpdateLegCommandHandler.cs`

Implementation details:
- Referee result submissions are supported by `LegRefereeEntry` creation with validation against assigned referees.
- Duplicate referee result submissions are prevented.
- Official results can be created and updated.

Missing or deviated steps:
- There is no `StartRace` or race status transition logic for blocking new bets and moving from `Scheduled` to `InProgress`.
- No automatic comparison of referee entries or conflict detection logic.
- No race execution orchestration.

### Discrepancy Resolution

Related entities:
- `Leg`
- `LegRefereeEntry`
- `LegOfficialResult`
- `Violation`

Related endpoints:
- `PUT api/legs/{raceId}/{legNumber}`
- `PUT api/leg-official-results/{raceId}/{legNumber}/{entryId}`
- `POST api/violations`
- `PUT api/violations/{violationId}`

Handlers:
- `Application/Usecases/Legs/UpdateLeg/UpdateLegCommandHandler.cs`
- `Application/Usecases/LegOfficialResults/UpdateLegOfficialResult/UpdateLegOfficialResultCommandHandler.cs`
- `Application/Usecases/Violations/CreateViolation/CreateViolationCommandHandler.cs`
- `Application/Usecases/Violations/UpdateViolation/UpdateViolationCommandHandler.cs`

Implementation details:
- Leg admin override reason and status fields exist.
- Violation admin review is implemented through update commands storing `ReviewedByAdminId`, `ReviewedAt`, `Status`, and `AdminNote`.

Missing or deviated steps:
- No automatic audit log or explicit conflict-resolution workflow.
- No system-level resume/pause semantics tied to verified results.

### Violation Handling

Related entities:
- `Violation`
- `Leg`
- `Entry`

Related endpoints:
- `POST api/violations`
- `GET api/violations/{violationId:int}`
- `GET api/violations`
- `PUT api/violations/{violationId:int}`
- `DELETE api/violations/{violationId:int}`

Handlers:
- `Application/Usecases/Violations/CreateViolation/CreateViolationCommandHandler.cs`
- `Application/Usecases/Violations/UpdateViolation/UpdateViolationCommandHandler.cs`
- `Application/Usecases/Violations/GetViolationDetail/GetViolationDetailQueryHandler.cs`
- `Application/Usecases/Violations/GetViolationList/GetViolationListQueryHandler.cs`
- `Application/Usecases/Violations/DeleteViolation/DeleteViolationCommandHandler.cs`

Implementation details:
- Referee reports are supported with required `ViolationType`, `Penalty`, and review fields.
- Admin review updates the status and optional note.

Missing or deviated steps:
- There is no explicit penalty application logic beyond storing penalty metadata.

### Spectator Betting

Related entities:
- `Prediction`
- `Spectator`
- `PointWallet`
- `WalletTransaction`
- `Entry`
- `Race`

Related endpoints:
- `POST api/predictions`
- `GET api/predictions/{predictionId:int}`
- `GET api/predictions`
- `PUT api/predictions/{predictionId:int}`
- `DELETE api/predictions/{predictionId:int}`
- `POST api/point-wallets`
- `POST api/wallet-transactions`

Handlers:
- `Application/Usecases/Predictions/CreatePrediction/CreatePredictionCommandHandler.cs`
- `Application/Usecases/Predictions/UpdatePrediction/UpdatePredictionCommandHandler.cs`
- `Application/Usecases/Predictions/DeletePrediction/DeletePredictionCommandHandler.cs`
- `Application/Usecases/PointWallets/CreatePointWallet/CreatePointWalletCommandHandler.cs`
- `Application/Usecases/WalletTransactions/CreateWalletTransaction/CreateWalletTransactionCommandHandler.cs`

Implementation details:
- Prediction creation validates race presence, spectator activity, distinct entries, and minimum bet amount.
- Locked odds are stored as `OddsLocked1`, `OddsLocked2`, `OddsLocked3`.
- Wallet and points entities are available, but betting-specific balance deduction is not linked in the prediction flow.

Missing or deviated steps:
- No wallet balance deduction or validation at bet creation.
- No odds lock behavior tied to race start.
- No bet cancellation before race start workflow.
- No weekly bonus point calculation.

### Result Publication & Settlement

Related entities:
- `RaceResult`
- `SettlementRun`
- `PredictionSettlement`
- `WalletTransaction`
- `PrizePointTransaction`
- `Race`

Related endpoints:
- `POST api/race-results`
- `POST api/settlement-runs`
- `POST api/prediction-settlements`
- `POST api/wallet-transactions`

Handlers:
- `Application/Usecases/RaceResults/CreateRaceResult/CreateRaceResultCommandHandler.cs`
- `Application/Usecases/SettlementRuns/CreateSettlementRun/CreateSettlementRunCommandHandler.cs`
- `Application/Usecases/PredictionSettlements/CreatePredictionSettlement/CreatePredictionSettlementCommandHandler.cs`
- `Application/Usecases/WalletTransactions/CreateWalletTransaction/CreateWalletTransactionCommandHandler.cs`
- `Application/Usecases/PrizePointTransactions/CreatePrizePointTransaction/CreatePrizePointTransactionCommandHandler.cs`

Implementation details:
- Race result entities exist and racing points can be stored.
- Settlement runs and prediction settlements exist as persistent records.

Missing or deviated steps:
- No dedicated race publication endpoint or business logic to publish/unpublish race results.
- No automatic settlement execution or wallet credit rollback logic.
- Race result publication is only represented by entities, not orchestration.

## Feature Completion Checklist

Implemented features:
- Horse registration, admin approval/rejection, and revocation with pending entry cancellation.
- Jockey invitation creation and status management.
- Entry creation and admin approve/reject with gate assignment.
- Tournament and race creation with referee assignment.
- Referee result submission storage via `LegRefereeEntry`.
- Violation creation and admin review workflow.
- Spectator prediction creation with locked odds and validation of distinct entries.
- CRUD endpoints for core entities.
- JWT authentication, role-based admin authorization, and refresh token support.

Partially implemented features:
- Race setup fields exist (`RegistrationOpenAt`, `RegistrationCloseAt`, `OddsComputedAt`, `PublishedAt`) but lack dedicated open/close commands.
- Leg administration and discrepancy fields exist without full conflict-resolution automation.
- Settlement entities exist without complete publish/settlement orchestration.
- Wallet and point entities exist, but betting wallet deduction and crediting not enforced automatically.

Missing features:
- Jockey search / owner-side invitation selection and automatic cancellation of alternate invitations.
- System auto-rejection of pending entries when registration closes.
- Odds calculation and locking at registration close or bet time.
- Race start workflow, bet locking at race start, and prevention of new bets.
- Automatic referee comparison and conflict detection logic.
- Discrepancy resolve audit log and resumption after admin review.
- Spectator wallet debit on bet placement and cancellation before race start.
- Result publication endpoint and rollback/unpublish behavior.
- Automatic settlement payout, leaderboard update, and spectator wallet credit logic.
- Weekly spectator bonus point logic.

## Developer Guide

To add a new feature following existing conventions:

1. Define a request type in `Application/Usecases/<Domain>/<Feature>/*Command.cs` for writes or `*Query.cs` for reads.
2. Implement a handler in the same folder with `IRequestHandler<...>`.
3. Use `IApplicationDbContext` or repository abstractions for data access.
4. Apply validation inside the handler and throw `InvalidOperationException` or `KeyNotFoundException` for missing data.
5. Add any new response DTO in the same use case folder if needed.
6. Expose the handler through an API controller in `Api/Controllers/` using `ISender`.
7. If persistence is required, rely on `UnitOfWorkBehavior` or explicit transaction logic for multi-step operations.
8. Register any new infrastructure dependencies in `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`.
9. If the feature requires new database schema, add EF configuration in `Infrastructure/Data/Configurations/` and update migrations.
10. Keep business rules inside the application layer rather than controller layer.

### Example flow for new commands

- Add command/handler.
- Add controller route and return `CreatedAtAction` or `Ok` as the API style.
- Use the same naming conventions: `CreateXCommand`, `UpdateXCommand`, `GetXDetailQuery`, `GetXListQuery`.
- Use `IApplicationDbContext` for direct entity access, or repository interfaces for aggregate roots.

### Testing and behavior

No dedicated tests are present in the workspace. New behavior should be validated by hitting the API routes.

---

### Notes

This document is based on actual source files in the repository. Missing flows are marked where source code does not provide the intended business automation.
