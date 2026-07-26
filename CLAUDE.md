# HRS Backend — CLAUDE.md

> Backend của Horse Race Management System. Xem [tổng quan dự án & 8 Main Flows](../CLAUDE.md) và [Frontend](../HorseRace.FE/CLAUDE.md). Việc cần làm + tiến độ: [.claude/TASKS.md](../.claude/TASKS.md).

## 1. Tech stack

- **.NET 10** / ASP.NET Core Web API (`net10.0`, Nullable + ImplicitUsings bật).
- **Clean Architecture**: Api → Application → Domain, với Infrastructure & Sharekernel.
- **MediatR 14** (CQRS: Command/Query + Handler).
- **EF Core 10** + **Npgsql** (PostgreSQL).
- **JWT** (Microsoft.AspNetCore.Authentication.JwtBearer) + **BCrypt.Net-Next** hash mật khẩu.
- **Swashbuckle/Swagger** cho API docs (`/swagger`).
- Deploy: **Dockerfile** sẵn, chạy trên Render.

## 2. Kiến trúc & cấu trúc dự án

Solution `HorseRace.sln` gồm 5 project:

```
Api/              ← Presentation: Controllers, Middlewares, Filters, Program.cs
Application/      ← Use cases (MediatR), interfaces (Common/), DI
Domain/           ← Entities thuần (Aggregates/Entities/)
Infrastructure/   ← EF Core: DbContext, Configurations, Migrations, Repositories, Services, Seed
Sharekernel/      ← Khối dùng chung: AggregateRoot, Entity, IRepository, UnitOfWork, DomainEvent
```

**Luồng phụ thuộc:** `Api → Application → Domain`; `Infrastructure → Application + Domain`; `Sharekernel` là nền chung. Domain không phụ thuộc project nào.

**Luồng một request:** Controller nhận DTO → `ISender.Send(Command/Query)` → MediatR route tới Handler → Handler dùng repository/service (qua interface trong `Application/Common/`) → trả Response.

**Pipeline behaviors** (`Application/Common/`): `LoggingBehavior`, `UnitOfWorkBehavior` (tự commit transaction cho Command).

## 3. Quy ước Use Case (rất quan trọng khi thêm tính năng)

> **Quy ước:** không tự ý sửa FE khi không được yêu cầu — mặc định làm phía BE, lấy FE làm gốc. **Được yêu cầu sửa FE thì cứ sửa bình thường.**

Mỗi use case là 1 thư mục `Application/Usecases/<Feature>/<Action>/` chứa:
- `XxxCommand.cs` / `XxxQuery.cs` — input (record), implement `ICommand`/`IQuery`.
- `XxxCommandHandler.cs` / `XxxQueryHandler.cs` — logic, implement `IRequestHandler<,>`.
- `XxxResponse.cs` — output DTO (cho Query/một số Command).

Mỗi Feature scaffold sẵn 5 action chuẩn: **Create / Update / Delete / GetList / GetDetail**. Controller tương ứng trong `Api/Controllers/<Feature>Controller.cs` chỉ mỏng, forward sang MediatR.

## 4. Domain entities (Domain/Aggregates/Entities/)

Schema phủ đủ 8 flow. Các entity chính và trường trạng thái:

| Entity | Vai trò / trường trạng thái đáng chú ý |
|--------|----------------------------------------|
| `User` | Tài khoản chung mọi role. `RoleId`, `Status` (Active/Pending...), `LockedUntil`. Trường riêng Jockey: `LicenseNumber`, `Weight`, `Bio`, `IsProfileComplete`. **`NormalizedPhoneNumber`** (max 20) + unique index `UX_Users_NormalizedPhoneNumber` — set bởi `NormalizeUserPhoneNumberInterceptor` + backfill startup (`UserPhoneNumberBackfill`). |
| `Role` | `ADMIN/REFEREE/HORSE_OWNER/JOCKEY/SPECTATOR` (Code, Name). |
| `Horse` | `Status`: Pending\|Approved\|Rejected\|Revoked (hằng số `HorseStatus.cs`), `RejectionReason`, `ApprovedBy/At`. (Flow 1) |
| `JockeyProfile` | Hồ sơ nài để Owner tìm kiếm. (Flow 2) |
| `JockeyInvitation` | Lời mời nài cho (Race+Horse). (Flow 2) |
| `Tournament` | Giải đấu (tên, venue, logo, ngày). (Flow 3) |
| `Race` | `NumberOfLegs` (1–10), `MaxHorses`, `Referee1Id`/`Referee2Id`, `Status` (Scheduled→InProgress→Paused→PendingResult→Finished→Cancelled), `ScheduledStartTime`/`ScheduledEndTime` (khung giờ để chống trùng lịch), `RegistrationOpen/CloseAt`, `OddsComputedAt`, `PublishedAt`. (Flow 3) |
| `Entry` | Cặp Horse+Jockey nộp vào Race. `Status`: Pending\|Approved\|Rejected\|Withdrawn, `GateNumber`, `Odds` (khóa khi đóng ĐK). (Flow 2) |
| `Leg` | PK ghép `(RaceId, LegNumber)`. `Status` (blind): Pending\|AwaitingSecondReferee\|Confirmed\|Conflicted\|Resolved; `StartedAt`/`FinishedAt`/`ConfirmedAt`/`ConflictReportedAt`; `ConfirmationType` (AutoMatched\|AdminOverride), `AdminOverrideReason`. ⚠️ **`ExecutionStatus`/`PredictionOpenedAt`/`PredictionClosedAt` ĐÃ BỊ GỠ** trong đợt revert 2026-07-25; file `Constants/LegExecutionStatuses.cs` **đã xóa** (T-21). (Flow 4-5) |
| `LegRefereeEntry` | Bản ghi blind của từng Referee/Leg (append-only). (Flow 4) |
| `LegRefereeDraft` | Nháp thứ hạng của referee (upsert, KHÔNG append-only) — để khôi phục khi quay lại (migration `AddLegRefereeDraft`). (Flow 4) |
| `LegOfficialResult` | Kết quả Leg chính thức sau confirm. (Flow 4-5) |
| `Violation` | `ViolationType`, `Penalty` (Warning\|Demote\|DQ\|None), `Status` Pending/Approved/Rejected, `AdminNote`. (Flow 6) |
| `RaceResult` | Vị trí & điểm chung cuộc của Entry. (Flow 8) |
| `PointWallet` / `WalletTransaction` | Ví điểm Spectator & lịch sử giao dịch. (Flow 7) |
| `Prediction` | Cược **1 Entry về 1st của cả RACE** (race-level): `RaceId`, `SpectatorId`, `FirstEntryId`, `BetAmount`, `OddsLocked1`, `Status` (`Pending`/`Locked`/`Won`/`Lost`/`Cancelled` — hằng số `PredictionStatus.cs`), `CreatedAt`, `CancelledAt`. Các cột `SecondEntryId/ThirdEntryId/OddsLocked2/3` vẫn còn nhưng **không dùng** (nullable, di sản multi-entry). (Flow 7) |
| `SettlementRun` / `PredictionSettlement` | Quá trình quyết toán cược. (Flow 8) |
| `PrizePointTransaction` | Cộng/trừ Prize Points cho Owner/Jockey. (Flow 8) |
| `Discrepancy` | Bản ghi tranh chấp Admin xử lý. (Flow 5) |
| `ReviewHistory` | **Audit trail append-only** (✅ 2026-06-27, mở rộng 2026-07-14 và 2026-07-26): `EntityType` (User=1/Horse=2/Entry=3/Race=4/Violation=5/**Leg=6**), `EntityId`, `Action` (Approved=1/Rejected=2/Revoked=3/Published=4/Unpublished=5/PenaltyChanged=6/Updated=7/**AdminOverride=8**), `Reason`, `BeforeData`/`AfterData` (jsonb snapshots), `AdminId`, `CreatedAt`. Ghi khi Admin duyệt hồ sơ, publish/unpublish race, approve/reject/update violation, **override kết quả Leg**. ⚠️ Với `EntityType = Leg`, `EntityId` là **`RaceId`** (không phải id của leg — leg có PK ghép). (Flow 1-2, 5, 6, 8) |
| `RefreshToken`, `PasswordResetOtp` | Hỗ trợ auth. |

EF mapping: `Infrastructure/Data/Configurations/*Configuration.cs` (mỗi entity một file). DbSet khai báo trong `Infrastructure/Data/ApplicationDbContext.cs` (`ApplicationDbContext` cũng là `IUnitOfWork`).

## 5. Tính năng đã hiện thực (chạy thật)

- **Auth** (`Api/Controllers/AuthController.cs` + `Application/Usecases/Auth/`):
  - `POST /api/auth/register/{spectator|horse-owner|jockey}` — Spectator active ngay; Horse Owner & Jockey chờ Admin duyệt.
  - `POST /api/auth/login` — trả access + refresh token (JWT, claim role).
  - `GET /api/auth/profile` [Authorize] — hồ sơ user hiện tại, resolve UserId từ JWT claims (✅ thêm 2026-06-25, `Auth/GetProfile`).
  - `PUT /api/auth/profile` [Authorize] — **user tự sửa hồ sơ của mình** (✅ thêm 2026-07-15, `Auth/UpdateProfile`; UserId từ JWT, body `{FullName, PhoneNumber}` → trả `{ user }`). Chỉ đổi FullName/PhoneNumber → không tự nâng quyền/mở khóa được. Cần thiết vì `PUT /api/users/{id}` là **ADMIN-only + full-replace** (ghi đè cả RoleId/IsActive) nên Owner/Jockey không dùng để tự sửa hồ sơ được.
  - `POST /api/auth/logout` — revoke refresh token (yêu cầu Bearer).
  - `POST /api/auth/refresh-token` — token rotation.
  - `POST /api/auth/forgot-password` + `reset-password` — qua `PasswordResetOtp` (✅ thêm 2026-06-25, `Auth/ForgotPassword`+`ResetPassword`; DEV trả OTP trong response vì chưa có email service).
  - `PUT /api/users/{id}/change-password` [Authorize] — đổi mật khẩu, verify mật khẩu cũ (✅ thêm 2026-06-25, `Users/ChangePassword`; UserId lấy từ claims, bỏ qua route id).
- **Admin tiện ích** (`AdminController`, ADMIN — ✅ thêm 2026-06-25): `GET /api/admin/violations` (`GetAdminViolations`), `GET /api/admin/points/balances|transactions` + `POST /api/admin/points/adjust` (`PointsManagement`), `GET /api/admin/discrepancies` + `POST /{id}/resolve` (`Discrepancies` — **entity + migration `AddDiscrepancy`**).
- **Audit trail** (`ReviewHistory`, ✅ **2026-06-27** — migration `AddReviewHistory`; mở rộng **2026-07-14** — migration `ExtendReviewHistoryForPublishViolationAudit`): mỗi lần Admin duyệt **User · Horse · Entry**, publish/unpublish **Race**, hoặc approve/reject/update **Violation** đều ghi 1 bản ghi append-only (`entity`, `id`, `action`, `reason`, `beforeData`/`afterData` jsonb, `AdminId`, thời điểm) trong cùng giao dịch với handler. Đọc qua `GET /api/admin/review-history?entity=&entityId=` (`Admin/GetReviewHistory`). Repo `IReviewHistoryRepository`. Unpublish **bắt buộc** `reason` (max 500).
- **Phân quyền (Authorization)** ✅ **đã phủ toàn bộ** (2026-06-25): mọi controller có class-level `[Authorize]` trừ `AuthController` (cố ý public cho register/login/forgot/reset/refresh; `[Authorize]` ở logout/profile). Role-locked: `Admin`/`Roles`/`PrizePointTransactions`/`SettlementRuns`/`PredictionSettlements` = ADMIN; `Legs`/`LegRefereeEntries`/`LegOfficialResults`/`Violations` = REFEREE,ADMIN. Controller đọc-đa-vai-trò (GET cho mọi user đăng nhập, **gate write theo role**): Races/Tournaments/PointWallets/WalletTransactions (write ADMIN), Horses (write HORSE_OWNER), Predictions (write SPECTATOR), RaceResults (write REFEREE,ADMIN), Users (**create/update/delete ADMIN**), `pause` execution **ADMIN + REFEREE** (referee bị siết ở tầng handler — chỉ leg `Resolved` của race mình phụ trách).
  🔒 **Scope dữ liệu cá nhân (T-25):** PointWallets/WalletTransactions/Predictions còn lọc theo người gọi ở tầng query, không chỉ theo role — xem cuối mục 6.
  🟠 **Ngoại lệ chưa vá:** `GET /api/users` không khóa role ⇒ mọi user đăng nhập đều liệt kê được toàn bộ user (T-26-cũ).
- **Lưu ý `ICurrentUser`**: file `Application/Common/Interfaces/ICurrentUser.cs` **đã có nhưng toàn bộ bị comment** (chưa wire DI, chưa implement) → effectively chưa dùng được. Hiện identity vẫn resolve thủ công trong controller bằng `User.FindFirst("userId")`/`NameIdentifier` (xem `AdminController`, `RaceExecutionController`, `PredictionsController`, `AuthController`). Cần uncomment + implement + đăng ký DI để dùng thật.
- **Admin duyệt user** (`AdminController`, `[Authorize(Roles="ADMIN")]`):
  - `GET /api/admin/users/pending`, `POST /api/admin/users/{id}/approve`, `POST /api/admin/users/{id}/reject`.
- **Flow 1 — Đăng ký & duyệt ngựa** (`Api/Controllers/HorsesController.cs` + `Application/Usecases/Horses/`): toàn bộ vòng đời ngựa đã hiện thực đầy đủ.
  - `POST /api/horses` [HORSE_OWNER] — tạo ngựa, Owner lấy từ JWT, `Status=Pending`.
  - `GET /api/horses?status=` — Owner thấy ngựa của mình, Admin thấy tất cả (lọc theo status).
  - `GET /api/horses/{id}`, `PUT /api/horses/{id}` [HORSE_OWNER, kiểm ownership, **chỉ chặn khi `Approved`**], `DELETE /api/horses/{id}` [HORSE_OWNER, kiểm ownership].
  - ❌ `GET /api/horses/{id}/statistics` — **đã bị gỡ** trong đợt revert 2026-07-25 (`Horses/GetHorseStatistics` bị xóa). FE đã ngừng gọi; `HorseDetailPage` tự ghép số liệu từ `/api/entries` + `/api/race-results`.
  - `POST /api/horses/{id}/resubmit` [HORSE_OWNER] — ngựa `Rejected` → `Pending`, xóa `RejectionReason`; check ownership (✅ thêm 2026-07-25, `Horses/ResubmitHorse`).
  - `POST /api/horses/{id}/approve` [ADMIN] → `Approved`; `POST /api/horses/{id}/reject` [ADMIN] (lý do bắt buộc) → `Rejected`; `POST /api/horses/{id}/revoke` [ADMIN] → `Revoked` + auto-cancel các Entry `Pending` dùng ngựa đó.
  - Status ngựa: `Pending → Approved | Rejected`, `Rejected → Pending` (resubmit), và revoke (`Approved →` hủy). Hằng số: `Domain/Aggregates/Constants/HorseStatus.cs`, `EntryStatus.cs`. Repo: `IHorseRepository`, `IEntryRepository`. `RevokeHorse` dùng transaction tường minh (`_context.Database.BeginTransactionAsync`).
  - ❌ **Thể lực ngựa:** cơ chế `Horse.Stamina`/`HealthStatus` **không còn tồn tại** (revert 2026-07-25).
- **Flow 2 — Mời nài & nộp Entry** ✅ **đã hoàn thiện orchestration** (2026-06-25).
  - **Mời nài** (`POST /api/jockey-invitations` [HORSE_OWNER]): validate horse `Approved` + thuộc owner, race `Scheduled`, nài role JOCKEY + có `LicenseNumber`+`Weight`, chống trùng invitation active `(jockey+horse+race)`. HorseOwnerId lấy từ JWT.
  - **Phản hồi/Xác nhận** (`PUT /api/jockey-invitations/{id}`, body `{status, responseReason}`): Accept/Decline chỉ chính nài; Confirm/Cancel chỉ owner; **Confirm → auto-cancel** mọi invitation active khác cùng `(horse+race)` + chặn 1 nài confirm cho 2 ngựa khác nhau trong cùng race. (Controller dựng command từ route id + claims — đã sửa bug "InvitationId mismatch".)
  - **Nộp Entry** (`POST /api/entries` [HORSE_OWNER]): **jockey LẤY TỪ invitation Confirmed** (không tin body), validate horse Approved+sở hữu, race Scheduled, chống trùng horse/jockey trong race. HorseOwnerId từ JWT.
  - **Admin duyệt Entry**: `POST /api/admin/entries/{id}/approve|reject` [ADMIN] (`Admin/ApproveEntry`, `RejectEntry`); `GET /api/admin/entries/pending`.
  - **List scope theo role:** `GET /jockey-invitations` (jockey→nhận, owner→gửi, admin→tất cả); `GET /entries?raceId=` (HORSE_OWNER→của mình, referee/spectator/admin→tất cả). `GET /jockey-profiles` chỉ hiện nài có `LicenseNumber`+`Weight`.
  - **Còn lại (thuộc Flow 3):** đóng đăng ký → auto-reject Entry `Pending` + tính/khóa Odds + gán GateNumber.
  - **Đối chiếu `FlowHorseRace.docx`:** doc mô tả "Owner đăng ký ngựa vào Race → Admin duyệt registration → mới mời nài". **Không có cổng duyệt riêng cho từng (ngựa+race)**: "duyệt registration" trong doc chính là **duyệt ngựa ở Flow 1** (ngựa phải `Approved`). Cổng Admin **duy nhất** của Flow 2 là **duyệt Entry** (sau khi đã confirm nài + nộp Entry). Code khớp spec.
  - **Lưu ý:** không dùng `ICurrentUser` (chưa tồn tại) — identity resolve trong controller bằng `User.FindFirst("userId")`; status là string literal; handler dùng `IApplicationDbContext` trực tiếp.
- **Read services** (`Infrastructure/Services/`): `RaceReadService`, `TournamentReadService`, `EntryReadService`, `RaceResultReadService` — truy vấn đọc.
- **Hạ tầng**: JWT config + custom 401/403 ProblemDetails (Program.cs), CORS mở (`FrontendPolicy`), `GlobalExceptionHandler`, `ApiResponseFilter`, auto `MigrateAsync()` + seed khi Development.
- **Seeder** (`Infrastructure/Data/Seed/DatabaseSeeder.cs`): tạo 5 role + tài khoản test (xem mục 8).

## 6. Tình trạng Flow 3–8 — orchestration ĐÃ HOÀN THIỆN ✅

> Cập nhật: **2026-07-26, HEAD `705c9b5`** (đọc lại toàn bộ code base + verify trực tiếp file nguồn). Build **0 lỗi / 0 warning**. Việc cần làm: [.claude/TASKS.md](../.claude/TASKS.md).
> Lưu ý lịch sử: docs cũ (≤2026-06-25) mô tả Flow 3–8 "chỉ CRUD generic, chưa có orchestration". **Điều đó đã lỗi thời** — toàn bộ nghiệp vụ lõi Flow 3–8 nay nằm ở `Application/Usecases/RaceExecution/*` (use case đặc thù, không phải CRUD).
>
> ### 🆕 ĐỢT 2026-07-26 (9 commit sau `26b8f6c`) — đọc trước tiên
>
> | # | Thay đổi | File chính |
> |---|---|---|
> | 1 | **Auto-cancel race quá hạn** — worker nay chạy **2 pass**: `ProcessDueRaceStartsCommand` rồi `ProcessDueRaceCancellationsCommand`. Hủy race `Scheduled` khi qua `ScheduledEndTime`, **hoặc** qua `ScheduledStartTime` mà Entry `Pending`+`Approved` < 2 (`MinEntriesToStart = 2`, batch 50). | `RaceExecution/ProcessDueRaceCancellationsCommand.cs`, `Api/Services/RaceAutoStartBackgroundService.cs` |
> | 2 | **Gom logic hủy race** vào `IRaceLifecycleCoordinator.CancelRaceAsync(raceId, reason, throwOnFailure)` — pessimistic lock + transaction; cascade Entry `Pending`/`Approved` → **`Withdrawn`** (ghi `RejectionReason`), JockeyInvitation `Pending`/`Accepted`/`Confirmed` → **`Cancelled`**. `DeleteRaceCommandHandler` nay chỉ là wrapper gọi hàm này (`throwOnFailure: true`). 🔴 **Không hoàn cược** — [T-28](../.claude/TASKS.md). | `RaceExecution/RaceLifecycleCoordinator.cs`, `Races/DeleteRace/DeleteRaceCommandHandler.cs` |
> | 3 | **`CreateRace` chặn giờ bắt đầu ở quá khứ** (*"Start Time cannot be in the past."*). | `Races/CreateRace/CreateRaceCommandHandler.cs` |
> | 4 | **`UpdateRace` chỉ cho sửa race `Scheduled`** (*"Only scheduled races can be updated."*) — trước chỉ chặn `Cancelled`. Guard "khóa NumberOfLegs sau khi start" đã bỏ vì nay thừa. | `Races/UpdateRace/UpdateRaceCommandHandler.cs` |
> | 5 | **`GET /api/races/{id}/pause` mở cho REFEREE** — `[Authorize(Roles="ADMIN,REFEREE")]`, thêm query `?legNumber=`. ADMIN: bỏ trống `legNumber` = tự tìm leg `Conflicted` (hành vi cũ), hoặc chỉ định leg bất kỳ. REFEREE: **bắt buộc** `legNumber`, phải là referee được gán cho race, và leg phải đã `Resolved` — nếu không → `UnauthorizedAccessException`. Giữ Blind Double-Entry trong lúc còn đang xử lý conflict. | `RaceExecution/GetRacePause.cs`, `Api/Controllers/RaceExecutionController.cs` |
> | 6 | **Audit cho Admin override leg** — `OverrideLegResult` ghi `ReviewHistory` (`EntityType = Leg`, `EntityId = RaceId`, `Action = AdminOverride`, before/after snapshot gồm cả `decisions`). Thêm `ReviewEntity.Leg = 6` + `ReviewAction.AdminOverride = 8`. | `RaceExecution/OverrideLegResult.cs`, `Domain/Aggregates/Enums/*` |
> | 7 | **`GetRefereeLegView` trả `AdminOverrideReason`** — chỉ khi leg đã `Resolved` (trước đó referee không thấy Admin quyết vì sao). | `RaceExecution/GetRefereeLegView.cs` |
> | 8 | **Khóa thao tác Violation sau khi Publish** — `CreateViolation` từ chối race `Finished`/`Cancelled`; `ApproveViolation`/`RejectViolation` từ chối race `Finished`; `UpdateViolation` chặn khi thay đổi **ảnh hưởng standings** (đổi status/penalty/leg/entry/race) và race nguồn **hoặc** race đích đang `Finished` — message *"Race already published — unpublish it first."* | `Violations/*` |
> | 9 | **`GET /api/admin/entries/pending`** lọc thêm `Race.Status == Scheduled`. | `Admin/GetPendingEntries/GetPendingEntriesQueryHandler.cs` |
> | 10 | **T-25 — scope dữ liệu cá nhân** + `GET /api/leaderboards/spectators` (xem cuối mục 6). | `PointWallets`/`WalletTransactions`/`Predictions`/`Leaderboards` |
>
> ### 🔄 ĐỢT REVERT 2026-07-25 — per-leg → race-level (nền của hiện trạng)
>
> Commit `6601526 "revert all back to betRace"` + merge `3d332d7` (8 file conflict) + `26b8f6c "new"` đã **gỡ toàn bộ mô hình per-leg**. Danh sách chính xác những gì **không còn tồn tại** trong code:
>
> | Đã gỡ | Hệ quả |
> |---|---|
> | `Prediction.LegNumber` + FK `(RaceId, LegNumber)` → `Legs` | Cược lại theo **cả race**. ✅ Đã drop bằng `DropOrphanPerLegColumns` |
> | `Leg.ExecutionStatus` / `PredictionOpenedAt` / `PredictionClosedAt` | Leg chỉ còn `Status` blind. `LegExecutionStatuses.cs` **đã xóa** |
> | `StartLeg.cs` + route `POST /api/races/{id}/legs/{n}/start` | Chỉ còn `POST /api/races/{id}/start` (cả race) + worker auto-start |
> | `LegPredictionSettlement.cs` (settle khi Leg Confirmed) | Settlement **quay lại** trong `PublishRaceResult` |
> | `Horse.Stamina` + `HealthStatus` computed | `HorseHealthStatus.cs` **đã xóa** |
> | `Horses/GetHorseStatistics/*` + route `GET /api/horses/{id}/statistics` | FE đã ngừng gọi (T-19 đóng) |
> | Gate "chặn Publish khi còn Violation `Pending`" | `publication-review` chỉ còn **báo** `PendingViolationCount` |
> | `stamina`/`healthStatus` trên `RaceLiveEntryDto`; `executionStatus`/`isBettingOpen` trên `RaceLiveLegDto` | FE đã ngừng đọc (T-19 đóng) |
>
> **✅ T-18…T-26 đã đóng (2026-07-25 → 26):** migration drop cột mồ côi, FE race-level, `Pending→Locked` on start, dọn code chết, bỏ `IsBettingOpen` khỏi odds response, sửa body cược `entryId`, scope dữ liệu cá nhân.
>
> ### ✅ Những thay đổi nghiệp vụ VẪN CÒN HIỆU LỰC
>
> **① Thang điểm TUYẾN TÍNH theo sĩ số** (`RaceExecutionConstants`) — **breaking change, sống sót qua revert**
> ```csharp
> // fieldSize (N) = số Entry Approved của race
> LegPointsFor(pos, resultStatus, fieldSize)  => N - p + 1        // DNF/DQ = 0; p > N = 0
> PrizePointsFor(finalPosition, fieldSize)    => (N - p + 1) * 200  // PrizePointUnit = 200
> ```
> Thay thang cứng cũ (Leg 6/5/4/3/2/1, Prize 1000/600/400/200/100). Neo: race 5 ngựa → 1st = 5×200 = 1000, bằng mức cũ. Lý do: race đông ngựa thì hạng chót không còn bị 0 điểm oan.
> **Caller:** `SubmitLegResult`, `OverrideLegResult`, `PublishRaceResult`, `UnpublishRaceResult`, `ApproveViolation`, `UpdateViolation`. `UnpublishRaceResult` tính lại `fieldSize` = số Entry `Approved` để rollback **đúng bằng** lúc cộng.
>
> **② `ValidatePositions(positions, fieldSize)`** — dùng chung `SubmitLegResult` + `OverrideLegResult`. Vị trí phải ∈ `1..N`, **không trùng**, và **liên tục từ 1** (bỏ `-1` DNF / `-2` DQ ra thì đúng `1..k`). Trả `string?` — `null` là hợp lệ, khác `null` là message lỗi tiếng Anh.
>
> **③ Cửa cược RACE-LEVEL** — không còn khái niệm cửa cược theo leg. `CreatePredictionCommandHandler`:
> - `race.Status` phải `== "Scheduled"` → *"You can only bet while the race is Scheduled."*
> - `race.OddsComputedAt` phải `!= null` → *"Registration must be closed and odds computed first."*
> ⇒ **khoảng cược thực tế = sau khi Admin đóng đăng ký, trước khi race start.** Tối đa **1 prediction active / (Race + Spectator)**.
> `DeletePrediction` (hủy cược): yêu cầu race `Scheduled` **và** prediction `Pending`, hoàn 100% + ghi `WalletTransaction` `Type="BetRefund"`.
>
> **④ `DELETE /api/races/{id}` là SOFT-CANCEL** — chỉ cho khi `Status == Scheduled`, set `Status = Cancelled` (không xóa row). **Không có** cột `IsDeleted` trong domain.
> *Cập nhật 2026-07-26:* handler nay **ủy quyền cho `IRaceLifecycleCoordinator.CancelRaceAsync`** (dùng chung với worker auto-cancel) ⇒ ngoài đổi status còn **cascade**: Entry `Pending`/`Approved` → `Withdrawn`, JockeyInvitation `Pending`/`Accepted`/`Confirmed` → `Cancelled`. `UpdateRace` nay chặn **mọi** status ≠ `Scheduled`; `CloseRegistration` vẫn chặn `Cancelled` riêng.
>
> **⑤ Chuẩn hóa & chống trùng số điện thoại** — `Domain/Aggregates/Constants/PhoneNumberNormalizer.cs` (VN mobile: đưa về `84` + 9 số; chấp nhận `0xxxxxxxxx` / `84xxxxxxxxx` / `0084…` / 9 số; prefix hợp lệ `843/845/847/848/849`), `InvalidPhoneNumberException`, `Infrastructure/Services/NormalizeUserPhoneNumberInterceptor.cs` (SaveChanges interceptor), `User.NormalizedPhoneNumber` + unique index có filter, backfill startup `UserPhoneNumberBackfill`.
> ✅ **Bug DI đã fix (T-01):** `AddInfrastructure` nay gọi `AddDbContext<ApplicationDbContext>` **đúng 1 lần** (`ServiceCollectionExtensions.cs:27`) và có `options.AddInterceptors(...)` ⇒ interceptor chạy thật.
>
> **⑥ `GlobalExceptionHandler`** — thêm `traceId` vào `ProblemDetails.Extensions`; map `InvalidPhoneNumberException` → 400; unique-violation trên `UX_Users_NormalizedPhoneNumber` → 409 riêng (**phải đặt trước** case `DbUpdateException` chung); map thêm PG error code `ForeignKeyViolation`/`NotNullViolation`/`CheckViolation`; 500 trả message chung, **không** lộ `exception.Message`. ✅ Toàn bộ message đã là **tiếng Anh** (T-05 đóng).
>
> **⑦ Endpoint còn hiệu lực từ đợt trước**
> | Route | Use case | Ghi chú |
> |---|---|---|
> | `POST /api/horses/{id}/resubmit` | `Horses/ResubmitHorse` | `Rejected → Pending`, xóa `RejectionReason`; check ownership |
> | `POST /api/races/{id}/start` | `RaceExecution/StartRace` | REFEREE/ADMIN; `enforceSchedule: false` (khớp FE), `allowAutoClose: true` |
> | `POST /api/admin/points/daily-topup` | `Admin/PointsManagement/RunDailyTopUp` | nạp bù ví `Balance < 10` **lên đúng 10**; idempotent theo ngày (`Type="DailyTopUp"`, mốc `now.Date` UTC). ⚠️ **Không có background service** — chỉ chạy khi Admin bấm (T-06) |
> | `GET /api/admin/violations?search=&sort=&sortDirection=` | `Admin/GetAdminViolations` | search khớp loại vi phạm/mô tả/tên race/nài/ngựa; sort `violationType\|status\|penalty\|legNumber\|racename\|createdAt`, tie-break `ViolationId` |
> | `GET /api/admin/races/{id}/publication-review` | `Admin/ResultPublication` | có `PendingViolationCount` + `HasUnresolvedTie` — **chỉ thông tin**, BE không còn chặn Publish theo đó |
> | `GET /api/leaderboards/spectators` | `Leaderboards/GetSpectatorBettingLeaderboard` | ✅ 2026-07-26 — xếp hạng cược, **chỉ số liệu tổng hợp** |
>
> **⑧ Khác**
> - `OverrideLegResult` set `leg.Status = Resolved` + `ConfirmationType = AdminOverride` + lý do bắt buộc + **ghi `ReviewHistory`** (`Leg`/`AdminOverride`); race quay lại `InProgress` (hoặc `PendingResult` nếu hết leg).
> - `UpdateHorse` chỉ chặn khi ngựa `Approved` (cho sửa ngựa `Rejected` để resubmit). Ownership vẫn được check ở cả Update/Delete/Resubmit.
> - `RevokeHorse` → `Status = Revoked` + hủy Entry liên quan + **hoàn cược** `Pending`/`Locked` của Entry đó (nếu race đã đóng đăng ký) + **tính lại Odds & GateNumber** cho các Entry `Approved` còn lại.
> - `RaceLegProvisioner.EnsureLegsExistAsync` tạo bù Leg `1..NumberOfLegs` khi close-registration & start; `SyncLegCountAsync` đồng bộ khi `UpdateRace` còn `Scheduled` (chặn xóa leg nếu race đã có prediction).
>
> **⑨ Race lifecycle — bảng tóm tắt ai đổi được `Race.Status`** *(thêm 2026-07-26)*
> | Từ → Đến | Ai / cái gì |
> |---|---|
> | `Scheduled → InProgress` | `POST /races/{id}/start` (REFEREE/ADMIN) hoặc worker pass 1 |
> | `Scheduled → Cancelled` | `DELETE /races/{id}` (ADMIN) hoặc worker pass 2 (quá `ScheduledEndTime`, hoặc quá `ScheduledStartTime` mà < 2 entry) |
> | `InProgress → Paused` | `SubmitLegResult` khi 2 referee lệch nhau |
> | `Paused → InProgress` | `POST /races/{id}/resume` (ADMIN) hoặc `OverrideLegResult` |
> | `InProgress → PendingResult` | submit/override leg cuối cùng |
> | `PendingResult → Finished` | `POST /races/{id}/publish` (ADMIN) |
> | `Finished → PendingResult` | `POST /races/{id}/unpublish` (ADMIN, `reason` bắt buộc) |
>
> **⚠️ Nợ kỹ thuật còn lại:** 2 migration drop trùng (T-27); hủy race không hoàn cược (T-28); `ICurrentUser` **vẫn bị comment toàn bộ** (T-07); tie-break cuối "quyết định Admin" chưa có endpoint (T-10); FluentValidation hoãn (T-12).

**✅ Orchestration vận hành đua** — `Application/Usecases/RaceExecution/*` (19 file: use case + `RaceLifecycleCoordinator`/`RaceLegProvisioner`/`RaceRankingCalculator`/`RaceExecutionConstants`) + `Api/Controllers/RaceExecutionController.cs` (prefix `api/races`, role-locked). Build pass, MediatR tự đăng ký:
- **Flow 3:** `POST {id}/open-registration`, `POST {id}/close-registration` (auto-reject Entry Pending + **khóa Odds per-Entry** từ win rate + gán **GateNumber** + set OddsComputedAt; transaction). `Entry.Odds` (cột mới + migration `AddEntryOdds`). UpdateRace khóa NumberOfLegs khi rời Scheduled; CreatePrediction ưu tiên `Entry.Odds` đã khóa làm `BaseOdds`.
  - ✅ **Đã hết trùng lặp (T-08):** bộ song song ở `RacesController` + `Usecases/Races/{OpenRegistration, CloseRaceRegistrationCommand, PublishOdds}` **đã bị xóa**. Chỉ còn 3 thư mục rỗng cần dọn ([T-21](../.claude/TASKS.md)).
- **Flow 3-4 (start cả race + auto-start/auto-cancel):** `POST {id}/start` (`StartRaceCommand`, **REFEREE/ADMIN**) → `IRaceLifecycleCoordinator.StartRaceAsync(enforceSchedule: false, allowAutoClose: true, throwOnFailure: true)`; **worker `RaceAutoStartBackgroundService`** (quét mỗi ~15s) chạy **2 pass**: `ProcessDueRaceStartsCommand` (cùng coordinator, `enforceSchedule: true`, `throwOnFailure: false`) rồi `ProcessDueRaceCancellationsCommand`. Coordinator dùng **pessimistic lock** (`SELECT … FOR UPDATE`) + bắt `DbUpdateException` để 2 worker không start trùng. `StartRaceAsync` cũng khóa prediction `Pending → Locked` của race (T-20). `POST {id}/resume` (Paused→InProgress), `GET {id}/execution`, `GET {id}/standings` (tổng Leg Points), `GET {id}/pause` (**ADMIN + REFEREE được gán**, so sánh 2 submission).
  - ❌ **Không còn `POST {id}/legs/{n}/start`** (`StartLeg` đã bị revert xóa) và **không còn `Leg.ExecutionStatus`**.
  - ⚠️ **Thứ tự 2 pass có ý nghĩa:** start chạy trước để race vừa đủ điều kiện ở tick đó không bị pass cancel bắt nhầm. Đừng đảo thứ tự.
- **Flow 4 blind:** `GET {id}/legs/{i}/referee-view` (ẩn input referee kia đến khi cả hai submit), `PUT {i}/draft` (**persist thật** — upsert vào `LegRefereeDraft`, có `MyDraftData` để khôi phục nháp), `POST {i}/submit` (append-only, **`ValidatePositions`** trước, so khớp → Confirmed/AutoMatched + tính **Leg Points tuyến tính `N−p+1`** & LegOfficialResult, hoặc Conflicted + Paused; hết leg → PendingResult). Submit đầu tiên set `leg.Status = AwaitingSecondReferee` và `leg.StartedAt` nếu còn null. ❌ **Không còn settle prediction ở đây** — đã quay về Publish.
- **Flow 4 — theo dõi trực tiếp (Spectator, ✅ thêm 2026-07-15):** `GET {id}/live` (`GetRaceLive`) trả snapshot gộp sẵn: tên ngựa/nài + GateNumber, trạng thái & vị trí từng leg (**chỉ leg Confirmed/Resolved mới có `Results`**), standings tạm tính, `CurrentLegIndex`, `SnapshotAt`. **Cố ý KHÔNG có `Referee1Submitted`/`Referee2Submitted`** (khác `GetRaceExecution`) và handler **không `Include(RefereeEntries)`** — giữ Blind Double-Entry.
  - **Push realtime qua SignalR:** hub `Api/Hubs/RaceLiveHub.cs` tại **`/api/hubs/race-live`** (đặt dưới `/api` để thừa hưởng proxy Vite → same-origin trong dev), group `race-{raceId}`, method `JoinRace`/`LeaveRace`, `[Authorize]` mọi role đăng nhập. Event đẩy về client: **`RaceLiveChanged`** mang đúng `RaceLiveResponse`.
  - **Kiến trúc đẩy:** handler gọi `IRaceLiveChangeTracker.MarkChanged(raceId)`; `RaceLiveBroadcastBehavior` (pipeline) drain sau `next()` rồi `Send(GetRaceLiveQuery)` → `IRaceLiveNotifier.PushAsync`. **⚠️ Behavior PHẢI đăng ký TRƯỚC `UnitOfWorkBehavior`** trong `AddApplication()` (đăng ký trước = nằm ngoài) để chỉ đẩy **sau commit**; đăng ký sau → client refetch trúng data cũ. `Drain()` phải chạy **trước** `Send` (chống đệ quy, vì query lồng đi lại chính behavior).
  - **MarkChanged ở 6 chỗ:** `RaceLifecycleCoordinator.StartRaceAsync` (nhánh `Started` — phủ **cả** `POST /start` lẫn worker auto-start), `SubmitLegResult` (matched + conflicted), `OverrideLegResult`, `ResumeRace`, `PublishRaceResult`, `UnpublishRaceResult` (2 cái cuối phủ luôn route trùng ở `AdminController`). **KHÔNG mark ở nhánh `AwaitingSecondReferee`** — đẩy lúc đó là báo "1 trọng tài vừa nộp", tức kênh phụ phá blind entry.
  - **JWT cho WebSocket:** `Program.cs` thêm `OnMessageReceived` đọc `access_token` từ query string, **có guard `path.StartsWithSegments(RaceLiveHub.Path)`** — thiếu guard thì `?access_token=` thành auth hợp lệ trên mọi endpoint. Thêm **vào trong** khối `options.Events` sẵn có (đừng gán `new JwtBearerEvents`, sẽ mất `OnChallenge`/`OnForbidden`).
  - **CORS:** policy thêm `.AllowCredentials()` (SignalR negotiate gửi `withCredentials: true`) và `Cors:AllowedOrigins` nay có `http://localhost:5173` (trước là **rỗng** → chặn mọi cross-origin). ⚠️ Không đổi sang `AllowAnyOrigin()`: kèm `AllowCredentials()` sẽ ném `InvalidOperationException` lúc khởi động.
  - **Hạn chế:** group lưu trong RAM tiến trình → chỉ đúng với 1 instance; chạy ≥2 replica cần Redis backplane.
- **Flow 5:** `POST {id}/legs/{i}/override` (**ADMIN**, lý do bắt buộc, **`ValidatePositions`**) → `leg.Status = Resolved`, `ConfirmationType = AdminOverride`, ghi `LegOfficialResult` + Leg Points + **`ReviewHistory`** (`Leg`/`AdminOverride`, before/after gồm `decisions`), race về `InProgress` (hoặc `PendingResult` nếu đã hết leg).
  - **Xem lại tranh chấp (✅ 2026-07-26):** `GET {id}/pause?legNumber=n`. ADMIN — bỏ trống `legNumber` = tự tìm leg `Conflicted` đầu tiên (trả `null` nếu không có), hoặc chỉ định leg bất kỳ ở mọi status. REFEREE — **bắt buộc** `legNumber`, phải là `Referee1Id`/`Referee2Id` của race, và leg phải `Resolved`; sai điều kiện → `UnauthorizedAccessException`. Đây là cách để trọng tài biết Admin đã quyết thế nào **sau khi** conflict đã xử lý xong, mà không lộ bản nhập của đồng nghiệp trong lúc còn đang xử lý.
- **Flow 8:** `POST {id}/publish` & `unpublish` — **atomic** (transaction tường minh): RaceResult + xếp hạng (tie-break tổng điểm→nhiều 1st→nhiều 2nd→vị trí leg cuối→`EntryId`; Race DQ xuống đáy/0đ), **Prize Points `(N−p+1)×200`** cho Owner/Jockey, cập nhật Career stats Jockey (`JockeyProfile`), **và quyết toán cược tại đây**: tạo `SettlementRun` + `PredictionSettlement` cho mọi prediction `Pending`/`Locked`, payout `BetAmount × OddsLocked1` vào ví, prediction → `Won`/`Lost`. Unpublish rollback đối xứng Prize Points/leaderboard/Career stats + hoàn payout (`Won`/`Lost` → `Locked`) (migration `AddRollbackFieldsForFlow8`). **Unpublish body `{ reason }` bắt buộc** — ghi `ReviewHistory` (`Race`/`Unpublished` + before/after snapshot); Publish ghi `Race`/`Published`. ✅ Route trùng ở `AdminController` **đã bị xóa** (T-08) — chỉ còn `/api/races/{id}/publish|unpublish`. Màn review trước publish: `GET /api/admin/races/{id}/publication-review` (`Admin/ResultPublication`). **Leaderboard** `GET /api/leaderboards/career` & `/tournament/{id}` (`?role=`) tính on-read từ `PrizePointTransaction`.
  - ❌ **Gate "chặn Publish nếu còn Violation `Pending`" đã bị revert gỡ.** Admin vẫn nên duyệt hết vi phạm trước, nhưng BE **không còn** chặn — `publication-review` chỉ *báo* `PendingViolationCount`.
  - ❌ Không còn cập nhật `Horse.Stamina` (cơ chế đã bị gỡ).
- **Flow 6:** referee report (`POST /api/violations`, ReportedByRefereeId từ JWT, Status Pending, LegNumber mặc định leg hiện hành); admin `POST /api/admin/violations/{id}/approve` (Warning / Demote tụt 1 hạng leg + recompute Leg Points / Race DQ zero điểm toàn bộ leg) & `/reject` (lý do bắt buộc, **set `Penalty="None"`**). `PUT /api/violations/{id}` **ADMIN-only**, ActorAdminId từ JWT, cho phép `Penalty="None"` + **rollback standings** khi sửa Approved→khác. `GET /api/admin/violations` trả `AdminNote` + `ViolatorRole` từ `Entry.Jockey.Role.Code`. Approve/Reject/Update đều ghi `ReviewHistory` (`Violation` + before/after jsonb). Publish loại entry Race DQ → xếp cuối, `IsRaceDQ`, 0 Prize.
  - **🚫 Demote không áp được thì BÁO LỖI, không im lặng (✅ 2026-07-26, `ViolationPenaltyGuard`):** trước đây cả `ApproveViolation` lẫn `UpdateViolation` bọc phần tụt hạng trong `if (official is { ResultStatus: Finished, FinishPosition: not null })` **không có else** — leg đang `DQ`/`DNF` (FinishPosition = null) hoặc chưa có `LegOfficialResult` thì block bị bỏ qua âm thầm: violation vẫn thành `Approved`/`Demote` nhìn như đã xử lý, nhưng standings không đổi gì và Admin **không có cách nào biết**. Nay throw 400 với message chỉ rõ nên dùng `Warning`/`DQ` hay chờ leg confirmed. Chiều ngược lại (`ReverseAppliedPenalty` khi Admin sửa violation) cũng vậy — kết quả leg đã bị ghi đè thì báo lỗi thay vì để lại standings sai, giống cách `DQ` đã làm từ trước. ⚠️ Chỉ `Demote` phụ thuộc `LegOfficialResult`; `DQ` đọc từ bảng `Violations` lúc Publish (`dqSet`) nên không bao giờ no-op.
  - **🔒 Scope theo người báo cáo (✅ 2026-07-26):** `GET /api/violations` (list **và** `/{id}`) trước đây trả **toàn bộ** bảng `Violations` cho bất kỳ ai gọi được ⇒ trọng tài A đọc được báo cáo của trọng tài B. Nay `GetViolationListQuery`/`GetViolationDetailQuery` nhận `int? ViewerRefereeId` (`null` = ADMIN thấy tất cả; khác null = lọc `ReportedByRefereeId`), controller cấp qua `GetViewerScope()` = `User.IsInRole("ADMIN") ? null : userId` từ JWT. Detail không thuộc mình → **404**. Cùng pattern T-25. ⚠️ `GET /api/admin/violations` (ADMIN-only) **không** đổi — Admin vẫn cần thấy hết để duyệt.
  - **🔒 Khóa sau khi Publish (✅ 2026-07-26):** `CreateViolation` từ chối race `Finished`/`Cancelled`; `ApproveViolation`/`RejectViolation` từ chối race `Finished`; `UpdateViolation` chặn khi sửa **ảnh hưởng standings** (status/penalty/legNumber/entryId/raceId đổi) mà race nguồn **hoặc** race đích đang `Finished`. Message: *"Race already published — unpublish it first."* **Lý do:** approve sau publish **không** tự chạy lại settlement ⇒ kết quả đã công bố sẽ sai âm thầm. Sửa vi phạm của race đã publish thì phải Unpublish → sửa → Publish lại.
- **Flow 7 (cược RACE-LEVEL — sau revert 2026-07-25):** mỗi prediction cược **1 Entry về 1st của cả Race**. Routes ở `PredictionsController` (prefix `api/predictions`):
  | Route | Use case | Role |
  |---|---|---|
  | `GET races/{raceId}/odds` | `GetRacePredictionOdds` | SPECTATOR, ADMIN, REFEREE, HORSE_OWNER |
  | `POST races/{raceId}` | `CreatePrediction` | SPECTATOR |
  | `GET {predictionId}` · `GET /` | `GetPredictionDetail` · `GetPredictionList` | mọi role đăng nhập — **nhưng chỉ thấy cược của CHÍNH MÌNH** (ADMIN thấy tất cả), xem T-25 |
  | `DELETE {predictionId}/cancel` | `DeletePrediction` | SPECTATOR |

  `CreatePrediction` hardened — **khóa odds server-side** per-(race, entry) qua `PredictionOddsCalculator.CalculateEntryOddsAsync`, trừ ví trong transaction, **SpectatorId từ JWT**. Validate: `race.Status == "Scheduled"` · `race.OddsComputedAt != null` · spectator `IsActive` · `BetAmount >= 10` · `BetAmount <= 50%` số dư · **tối đa 1 prediction chưa `Cancelled` / (Race + Spectator)**.
  `DeletePrediction` → hủy + hoàn 100% + **chống hủy hộ** (lọc theo `SpectatorId`); yêu cầu race `Scheduled`, prediction `Pending`, ví không `IsFrozen`.
  `PredictionStatus` (`Domain/Aggregates/Entities/PredictionStatus.cs`): `Pending` · `Locked` · `Won` · `Lost` · `Cancelled` · `Settled`.
  **Vòng đời thực tế:** `Pending` (khi đặt) → `Locked` (khi `StartRaceAsync` chạy — T-20) → `Won`/`Lost` (khi Publish); hoặc `Cancelled` (hủy tay / revoke ngựa / lock account). Unpublish rollback đưa `Won`/`Lost` về `Locked`.
  ⚠️ **`Settled` là hằng số chết** — không handler nào gán. Chỉ để lại vì đổi enum string cần migration data; đừng viết code mới dựa vào nó.
  🔴 **Hủy race KHÔNG hoàn cược** — `CancelRaceAsync` không đụng tới `Prediction`/`PointWallet` ⇒ [T-28](../.claude/TASKS.md).
  Migrations liên quan: `UpdatePredictionSingleEntryBet`, `UpdatePredictionDelete`, `CompleteFlow7PredictionBetting`, `RevertAllToOriginalState` + `DropOrphanPerLegColumns` (cùng drop cột mồ côi per-leg — xem [T-27](../.claude/TASKS.md)).
  - **Odds ĐỘNG theo pool** (`Application/Usecases/Predictions/common/PredictionOddsCalculator.cs`) — đây **không phải** odds tĩnh:
    - `BaseOdds` = `Entry.Odds`, khóa lúc đóng đăng ký bằng `CloseRegistrationCommandHandler.OddsFor(firsts, total, fieldSize)` — **Laplace smoothing**: `winRate = (firsts + 1) / (total + max(fieldSize, 2))`, `odds = 1 / max(winRate, 0.04)`, clamp `[1.10, 25.00]`.
    - `pressure = entryPool / max(avgPool, 1)` (`0.5` nếu entry chưa ai cược); `CurrentOdds = BaseOdds / √pressure`.
    - Pool tính **trên cả race**, bỏ prediction `Cancelled`. **Clamp `[1.10, 25.00]`**, làm tròn 2 chữ số (`AwayFromZero`).
    - Lúc đặt cược, `CalculateEntryOddsAsync` cộng **cả số tiền đang đặt** vào pool trước khi tính → odds khóa vào prediction đã phản ánh chính lệnh cược đó.
    - Response odds (`RacePredictionOddsResponse`) trả `baseOdds`, `currentOdds`, `entryPool`, `totalPool` + thông tin ngựa/nài/gate. ⚠️ Cờ **`isBettingOpen` hardcode `true`** — handler đã throw nếu race ≠ `Scheduled` hoặc odds chưa tính, nên cờ không bao giờ `false` ⇒ [T-22](../.claude/TASKS.md). ❌ Không còn `horseStamina`/`horseHealthStatus`.
  - **Nạp điểm:** **`RunWeeklyTopUp`** (+100, idempotent) chạy qua **`WeeklyTopUpBackgroundService`** (mỗi giờ, catch-up) + trigger admin `POST /api/admin/points/weekly-topup`. **`RunDailyTopUp`** (nạp bù ví `< 10` lên đúng 10, idempotent theo ngày UTC) chỉ có trigger admin `POST /api/admin/points/daily-topup` — **không có worker**.
  - **`LockUser`/`UnlockUser`** (`POST /api/admin/users/{id}/lock|unlock`) → khóa account, Spectator thì **hoàn cược Pending + đóng băng ví**. Admin points thêm `GET`+`PUT /api/admin/points/{userId}`.
- **Lấy danh tính từ JWT claims** (`userId`/NameIdentifier) trong controller — không tin body cho RefereeUserId/AdminUserId.
- **✅ Leaderboard đã có:** `LeaderboardsController` — `GET /api/leaderboards/career` & `GET /api/leaderboards/tournament/{id}` (`?role=`), tính on-read từ `PrizePointTransaction`; **`GET /api/leaderboards/spectators`** (✅ thêm 2026-07-26, `Leaderboards/GetSpectatorBettingLeaderboard`) — xếp hạng cược của Spectator, tính on-read từ `Prediction`, **chỉ trả số liệu tổng hợp** (rank/tên/số lệnh/thắng/win rate/tổng đặt/tổng thắng). Cố ý không trả lệnh cược lẻ: đây là nguồn thay thế cho việc FE trước đây tải cả `/api/predictions` về để tự gom (T-25).
- **🔒 Scope dữ liệu cá nhân (✅ 2026-07-26, T-25):** `GET /api/point-wallets`, `/api/wallet-transactions`, `/api/predictions` (cả list lẫn `/{id}`) trước đây trả **toàn bộ bảng** cho mọi user đã đăng nhập — lọc chỉ nằm ở FE. Nay 4 query nhận `int? ViewerSpectatorId` (`null` = ADMIN, khác null = lọc `SpectatorId`), controller cấp qua `GetViewerScope()` = `User.IsInRole("ADMIN") ? null : userId`. Detail trả `null` → **404** (không phải 403) để không lộ id có tồn tại hay không. ⚠️ Khi thêm endpoint đọc mới trên các bảng `PointWallet`/`WalletTransaction`/`Prediction`/`PredictionSettlement`/`Violation`, **phải** đi kèm scope tương tự.
  **Bổ sung 2026-07-26:** `GET /api/violations` (list + detail) cũng đã được scope theo `ReportedByRefereeId` — xem Flow 6 ở trên.
- **Còn lại (xem [.claude/TASKS.md](../.claude/TASKS.md) để có cách sửa):**
  - 🔴 **Mới 2026-07-26:** 2 migration drop trùng nhau + bản không idempotent chạy sau (**T-27**); `CancelRaceAsync` **không hoàn cược** cho spectator (**T-28**).
  - 🟠 `GET /api/users` chỉ có class-level `[Authorize]` ⇒ mọi role đăng nhập đều dump được danh sách user (T-26-cũ, cố ý hoãn).
  - ⚪ **Nợ cũ:** `ICurrentUser` vẫn bị comment, identity resolve thủ công (T-07); tie-break cuối "quyết định Admin" chưa có endpoint (T-10); FluentValidation hoãn (T-12).
  - 🧹 **Dọn nhỏ:** 2 thư mục rỗng `Usecases/Predictions/{PlacePrediction, UpdatePrediction}`; hằng số chết `PredictionStatus.Settled`.
  - ✅ **Đã đóng:** DI interceptor SĐT (T-01), migration rỗng (T-02), message EN (T-05), endpoint trùng (T-08), validate role referee (T-09), wire `StartRaceCommand` (T-11), toàn bộ T-18…T-26.

**Map use case ↔ route (Flow 3–8, đều là nghiệp vụ thật, không phải CRUD generic):**

| Use case (`Usecases/RaceExecution/`) | Route | Flow |
|---|---|---|
| `OpenRegistration` / `CloseRegistration` | `POST /api/races/{id}/open-registration` · `close-registration` (auto-reject Entry Pending + khóa Odds + gán GateNumber) | 3 |
| `StartRace` | `POST /api/races/{id}/start` (**REFEREE/ADMIN** — khởi động **cả Race**) | 3-4 |
| `ProcessDueRaceStarts` / `ProcessDueRaceCancellations` / `RaceLifecycleCoordinator` / `RaceLegProvisioner` | (không route trực tiếp) — **worker `RaceAutoStartBackgroundService`**: pass 1 auto-start khi qua `ScheduledStartTime`, pass 2 auto-cancel race quá hạn/thiếu entry; coordinator dùng chung cho start · cancel · close-registration | 3-4 |
| `ResumeRace` | `POST /api/races/{id}/resume` | 5 |
| `GetRaceExecution` / `GetRaceStandings` / `GetRacePause` | `GET /api/races/{id}/execution` · `standings` · `pause?legNumber=` (**ADMIN + REFEREE được gán; referee chỉ xem leg `Resolved`**) | 4-5 |
| `GetRaceLive` | `GET /api/races/{id}/live` (snapshot cho Spectator theo dõi trực tiếp; **cũng là payload push SignalR**) | 4 |
| `GetRefereeLegView` | `GET .../legs/{i}/referee-view` (ẩn input referee kia đến khi cả hai submit) | 4 |
| `SaveLegDraft` | `PUT .../legs/{i}/draft` (**persist thật** → `LegRefereeDraft`) | 4 |
| `SubmitLegResult` | `POST .../legs/{i}/submit` (append-only, so khớp → AutoMatched/Conflicted + Leg Points) | 4 |
| `OverrideLegResult` | `POST .../legs/{i}/override` (AdminOverride + lý do bắt buộc → leg `Resolved`) | 5 |
| `OverrideLegResult` (audit) | ghi `ReviewHistory` `Leg`/`AdminOverride` — đọc lại qua `GET /api/admin/review-history?entity=Leg` | 5 |
| `PublishRaceResult` / `UnpublishRaceResult` | `POST /api/races/{id}/publish` · `unpublish` (atomic, **kèm quyết toán cược**) | 8 |
| ~~`StartLeg`~~ | ~~`POST /api/races/{id}/legs/{n}/start`~~ — **đã bị xóa** trong đợt revert 2026-07-25 | — |

Ngoài ra: Flow 6 (Violations approve/reject → áp standings) ở `AdminController`; Flow 7 (`CreatePrediction` khóa odds server-side + trừ ví, **cược race-level**, weekly/daily top-up, lock/unlock) ở `Predictions`/`Admin`. Tất cả lấy danh tính từ JWT claims, **không tin body**.

**Khi thêm use case Flow mới:** theo pattern này — **use case đặc thù** trong `Usecases/<Feature>/<Action>/` (không nhét vào CRUD generic); repository interface ở `Application/Common/`, impl ở `Infrastructure/Repositories/`, đăng ký DI; Command dựa `UnitOfWorkBehavior` để tự commit (hoặc `IUnitOfWork.SaveChangesAsync` tường minh khi cần ID vừa sinh / thao tác atomic nhiều bước như Flow 8). Nợ kỹ thuật: danh tính nên gom về `ICurrentUser` (hiện resolve thủ công trong controller).

## 7. Chạy & phát triển

```bash
# Chạy API (từ thư mục HorseRaceManagementSystem)
dotnet run --project Api

# Build toàn solution
dotnet build HorseRace.sln

# EF migrations
dotnet ef migrations add <Name> --project Infrastructure --startup-project Api
dotnet ef database update --project Infrastructure --startup-project Api
```

- Cổng/Swagger: chạy lên là redirect `/` → `/swagger`.
- Migration tự apply khi khởi động (`db.Database.MigrateAsync()` trong Program.cs); seed test data khi `Development` hoặc cấu hình `SeedTestData=true`.
- **19 migration**, tất cả ở `Infrastructure/Migrations/` (namespace `Infrastructure.Migrations`). Hai migration cuối **cùng drop** bộ cột mồ côi per-leg (`Predictions.LegNumber`/`LegRaceId` + FK/index, `Legs.ExecutionStatus`/`PredictionOpenedAt`/`PredictionClosedAt`, `Horses.Stamina`):
  - `20260725113128_RevertAllToOriginalState` — dùng `DropForeignKey`/`DropIndex`/`DropColumn` **không** có `IF EXISTS`.
  - `20260725162506_DropOrphanPerLegColumns` — raw SQL, **có** `IF EXISTS` (an toàn, idempotent).
  🔴 **Bẫy:** DB nào đã apply `DropOrphanPerLegColumns` mà **chưa** apply `RevertAllToOriginalState` (tức đã chạy app ở khoảng commit `e490dc8`…`eec39fb`) sẽ **crash lúc khởi động** — EF thấy `RevertAllToOriginalState` còn pending, chạy nó, và `DROP CONSTRAINT` trên constraint đã biến mất → lỗi. Xem [T-27](../.claude/TASKS.md) để biết cách xử lý.
- **Background services** (đăng ký ở `Program.cs`): `WeeklyTopUpBackgroundService` (top-up ví thứ Hai) + `RaceAutoStartBackgroundService` (**auto-start + auto-cancel** Race, quét mỗi ~15s theo `RaceAutoStart:IntervalSeconds`, sàn 5s). **SignalR** hub `/api/hubs/race-live` (push diễn biến đua).

### ⛔ Quy ước kiểm thử: dự án KHÔNG có test tự động

Solution chỉ có **5 project sản phẩm** (`Api`, `Application`, `Domain`, `Infrastructure`, `Sharekernel`) — **không có test project**. `Application.Tests` đã bị **gỡ khỏi solution và xóa hẳn** (2026-07-25) theo quyết định của nhóm.

Nhóm **test thủ công** theo [.claude/TEST_PLAN_2026-07-26.md](../.claude/TEST_PLAN_2026-07-26.md). Khi làm việc trên repo này:

- **Không tạo file test**, không dựng lại test project, không thêm package test (xUnit / NUnit / Moq / InMemory provider…).
- **Không đề xuất "nên bổ sung test"** như một hạng mục cần làm, và **không coi việc thiếu test là điểm chặn** khi review code hay review plan.
- Kiểm chứng thay đổi bằng **`dotnet build HorseRace.sln`** (mục tiêu 0 lỗi) + **mô tả rõ cách test tay**: vào trang nào, bấm gì, kỳ vọng thấy gì. Nếu thay đổi ảnh hưởng luồng nào trong TEST_PLAN thì chỉ ra đúng case ID.

## 8. Cấu hình & tài khoản test

`Api/appsettings.json`:
- `ConnectionStrings:DefaultConnection` → PostgreSQL `localhost:5433/HorseRaceDB` (user/pass `postgres`).
- `JwtSettings`: SecretKey, Issuer `HorseRaceAPI`, Audience `HorseRaceClient`, access 60 phút, refresh 7 ngày.
- `RaceAutoStart:IntervalSeconds` (mặc định 15) — chu kỳ worker auto-start.
- **`Cors:AllowedOrigins`** — danh sách origin của FE. **Hỗ trợ `*` cho một nhãn tên miền** (`Api/Cors/AllowedOriginMatcher.cs`), ví dụ `https://horse-race-fe-*.vercel.app`. Cần thiết vì **Vercel sinh hostname mới mỗi lần deploy** (`<project>-git-<branch>-<scope>.vercel.app`, `<project>-<hash>-<scope>.vercel.app`) trong khi `WithOrigins()` chỉ khớp tuyệt đối ⇒ production alias chạy còn mọi bản preview bị chặn. `*` **không** nuốt `.` hay `/` nên `https://*.vercel.app` không khớp `https://a.b.vercel.app` hay `https://evil.com/x.vercel.app`.
  - Policy dùng `SetIsOriginAllowed(...)` + `.AllowCredentials()` (hợp lệ vì echo lại đúng origin) + `.SetPreflightMaxAge(1h)` (đỡ đánh thức instance Render đang ngủ chỉ để trả OPTIONS). **Không** đổi sang `AllowAnyOrigin()` — đi kèm `AllowCredentials()` sẽ ném `InvalidOperationException` lúc khởi động.
  - Nếu cấu hình bằng biến môi trường mà set `Cors__AllowedOrigins` thành **một chuỗi** `"a,b"` (thay vì `__0`/`__1`) thì bind sang `string[]` ra rỗng và policy **chặn sạch mọi origin trong im lặng** — Program.cs nay tách chuỗi theo dấu phẩy để tránh bẫy này.
  - Lúc khởi động log ra `CORS allowed origins: …` (hoặc warning nếu rỗng). CORS hỏng thì **server vẫn trả 204/200 bình thường**, chỉ browser báo lỗi — nên đối chiếu log này với origin thật trên thanh địa chỉ là cách chẩn đoán nhanh nhất. Kiểm bằng curl:
    ```bash
    curl -i -X OPTIONS "$BE/api/auth/login" -H "Origin: $FE_ORIGIN" -H "Access-Control-Request-Method: POST"
    ```
    Có `access-control-allow-origin` trong response = origin đó được phép; không có = bị chặn.

Tài khoản seed (password trong DatabaseSeeder):
| Email | Mật khẩu | Role |
|-------|----------|------|
| admin@hrs.com | Admin@123 | ADMIN |
| ref1@hrs.com / ref2@hrs.com | Ref@123 | REFEREE |
| owner@hrs.com | Owner@123 | HORSE_OWNER |
| jockey@hrs.com | Jockey@123 | JOCKEY |
| spectator@hrs.com | Spectator@123 | SPECTATOR |
| pending.referee@hrs.com | Pending@123 | REFEREE (Pending) |

## 8.1. Checklist trước khi deploy production

Rà soát dựa trên [Api/Program.cs](Api/Program.cs) (cập nhật khi thay đổi startup):

| Hạng mục | Hiện trạng | Khuyến nghị production |
|----------|------------|-------------------------|
| **Auto-migrate** | `await db.Database.MigrateAsync()` chạy **mọi lần khởi động**, mọi môi trường | Rủi ro migration tự áp khi scale nhiều instance. Cân nhắc EF migration bundle / pipeline deploy riêng thay vì migrate trong app. |
| **Seed test data** | `SeedTestData` bật ở Production → `InvalidOperationException` lúc startup (đã guard) | Giữ `SeedTestData: false` trên Render/production. |
| **OTP quên mật khẩu** | DEV có thể trả OTP trong response body khi chưa có email | Production: cấu hình `SmtpOptions` (section trong appsettings), tắt trả OTP trong response. |
| **FluentValidation** | Validate rải rác trong handler; chưa có `ValidationBehavior` | Quyết định **hoãn** — không phải bỏ sót (T-12). |
| **Claim `"userId"`** | JWT chỉ phát `NameIdentifier`/`sub`; controller đọc `"userId"` rồi fallback | **Cố ý hoãn cùng T-07** (`ICurrentUser`) — không gây lỗi runtime. |

## 9. Lưu ý khi làm việc

- **Không** đưa logic nghiệp vụ vào Controller — chỉ forward sang MediatR.
- Tôn trọng ranh giới: interface I/O khai báo ở `Application/Common/`, hiện thực ở `Infrastructure/`.
- Command đổi dữ liệu nên dựa vào `UnitOfWorkBehavior` để commit; với thao tác atomic nhiều bước (Flow 8) dùng transaction tường minh qua `IUnitOfWork`.
- Thêm entity mới: tạo Entity → Configuration → DbSet → migration.
- Endpoint cần phân quyền: dùng `[Authorize(Roles = "...")]`.
