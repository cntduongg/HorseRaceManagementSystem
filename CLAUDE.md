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

Mỗi use case là 1 thư mục `Application/Usecases/<Feature>/<Action>/` chứa:
- `XxxCommand.cs` / `XxxQuery.cs` — input (record), implement `ICommand`/`IQuery`.
- `XxxCommandHandler.cs` / `XxxQueryHandler.cs` — logic, implement `IRequestHandler<,>`.
- `XxxResponse.cs` — output DTO (cho Query/một số Command).

Mỗi Feature scaffold sẵn 5 action chuẩn: **Create / Update / Delete / GetList / GetDetail**. Controller tương ứng trong `Api/Controllers/<Feature>Controller.cs` chỉ mỏng, forward sang MediatR.

## 4. Domain entities (Domain/Aggregates/Entities/)

Schema phủ đủ 8 flow. Các entity chính và trường trạng thái:

| Entity | Vai trò / trường trạng thái đáng chú ý |
|--------|----------------------------------------|
| `User` | Tài khoản chung mọi role. `RoleId`, `Status` (Active/Pending...), `LockedUntil`. Trường riêng Jockey: `LicenseNumber`, `Weight`, `Bio`, `IsProfileComplete`. |
| `Role` | `ADMIN/REFEREE/HORSE_OWNER/JOCKEY/SPECTATOR` (Code, Name). |
| `Horse` | `Status`: Pending\|Approved\|Rejected, `RejectionReason`, `ApprovedBy/At`. (Flow 1) |
| `JockeyProfile` | Hồ sơ nài để Owner tìm kiếm. (Flow 2) |
| `JockeyInvitation` | Lời mời nài cho (Race+Horse). (Flow 2) |
| `Tournament` | Giải đấu (tên, venue, logo, ngày). (Flow 3) |
| `Race` | `NumberOfLegs` (1–10), `MaxHorses`, `Referee1Id`/`Referee2Id`, `Status` (Scheduled→InProgress→Paused→PendingResult→Finished→Cancelled), `RegistrationOpen/CloseAt`, `OddsComputedAt`, `PublishedAt`. (Flow 3) |
| `Entry` | Cặp Horse+Jockey nộp vào Race. `Status`: Pending\|Approved\|Rejected\|Withdrawn, `GateNumber`. (Flow 2) |
| `Leg` | PK ghép `(RaceId, LegNumber)`. `Status`: Pending\|AwaitingSecondReferee\|Confirmed\|Conflicted\|Resolved; `ConfirmationType` (AutoMatched\|AdminOverride), `AdminOverrideReason`. (Flow 4-5) |
| `LegRefereeEntry` | Bản ghi blind của từng Referee/Leg (append-only). (Flow 4) |
| `LegOfficialResult` | Kết quả Leg chính thức sau confirm. (Flow 4-5) |
| `Violation` | `ViolationType`, `Penalty` (Warning\|Demote\|DQ), `Status` Pending/Approved/Rejected. (Flow 6) |
| `RaceResult` | Vị trí & điểm chung cuộc của Entry. (Flow 8) |
| `PointWallet` / `WalletTransaction` | Ví điểm Spectator & lịch sử giao dịch. (Flow 7) |
| `Prediction` | Cược dự đoán **1 Entry** (refactor 2026-06-28): `FirstEntryId` (Entry chọn), `BetAmount`, `OddsLocked1`, `Status` (Pending/Settled/Cancelled), `CancelledAt`. Các cột `SecondEntryId/ThirdEntryId/OddsLocked2/3` vẫn còn nhưng **không dùng** (nullable, di sản multi-entry). (Flow 7) |
| `SettlementRun` / `PredictionSettlement` | Quá trình quyết toán cược. (Flow 8) |
| `PrizePointTransaction` | Cộng/trừ Prize Points cho Owner/Jockey. (Flow 8) |
| `Discrepancy` | Bản ghi tranh chấp Admin xử lý. (Flow 5) |
| `ReviewHistory` | **Audit trail duyệt hồ sơ** (✅ 2026-06-27): `EntityType` (User/Horse/Entry), `EntityId`, `Action` (Approved/Rejected), `Reason`, `AdminId`, `CreatedAt`. Ghi tự động khi Admin approve/reject User·Horse·Entry. (Flow 1-2) |
| `RefreshToken`, `PasswordResetOtp` | Hỗ trợ auth. |

EF mapping: `Infrastructure/Data/Configurations/*Configuration.cs` (mỗi entity một file). DbSet khai báo trong `Infrastructure/Data/ApplicationDbContext.cs` (`ApplicationDbContext` cũng là `IUnitOfWork`).

## 5. Tính năng đã hiện thực (chạy thật)

- **Auth** (`Api/Controllers/AuthController.cs` + `Application/Usecases/Auth/`):
  - `POST /api/auth/register/{spectator|horse-owner|jockey}` — Spectator active ngay; Horse Owner & Jockey chờ Admin duyệt.
  - `POST /api/auth/login` — trả access + refresh token (JWT, claim role).
  - `GET /api/auth/profile` [Authorize] — hồ sơ user hiện tại, resolve UserId từ JWT claims (✅ thêm 2026-06-25, `Auth/GetProfile`).
  - `POST /api/auth/logout` — revoke refresh token (yêu cầu Bearer).
  - `POST /api/auth/refresh-token` — token rotation.
  - `POST /api/auth/forgot-password` + `reset-password` — qua `PasswordResetOtp` (✅ thêm 2026-06-25, `Auth/ForgotPassword`+`ResetPassword`; DEV trả OTP trong response vì chưa có email service).
  - `PUT /api/users/{id}/change-password` [Authorize] — đổi mật khẩu, verify mật khẩu cũ (✅ thêm 2026-06-25, `Users/ChangePassword`; UserId lấy từ claims, bỏ qua route id).
- **Admin tiện ích** (`AdminController`, ADMIN — ✅ thêm 2026-06-25): `GET /api/admin/violations` (`GetAdminViolations`), `GET /api/admin/points/balances|transactions` + `POST /api/admin/points/adjust` (`PointsManagement`), `GET /api/admin/discrepancies` + `POST /{id}/resolve` (`Discrepancies` — **entity + migration `AddDiscrepancy`**).
- **Audit trail duyệt hồ sơ** (`ReviewHistory`, ✅ **thêm 2026-06-27** — migration `AddReviewHistory`): mỗi lần Admin approve/reject **User · Horse · Entry** đều ghi 1 bản ghi `ReviewHistory` (entity, id, action, lý do, AdminId, thời điểm) trong cùng giao dịch với handler (`Admin/ApproveUser|ApproveHorse|ApproveEntry|RejectEntry`…). Đọc qua `GET /api/admin/review-history?entity=&entityId=` (`Admin/GetReviewHistory`). Repo `IReviewHistoryRepository`.
- **Phân quyền (Authorization)** ✅ **đã phủ toàn bộ** (2026-06-25): mọi controller có class-level `[Authorize]` trừ `AuthController` (cố ý public cho register/login/forgot/reset/refresh; `[Authorize]` ở logout/profile). Role-locked: `Admin`/`Roles`/`PrizePointTransactions`/`SettlementRuns`/`PredictionSettlements` = ADMIN; `Legs`/`LegRefereeEntries`/`LegOfficialResults`/`Violations` = REFEREE,ADMIN. Controller đọc-đa-vai-trò (GET cho mọi user đăng nhập, **gate write theo role**): Races/Tournaments/PointWallets/WalletTransactions (write ADMIN), Horses (write HORSE_OWNER), Predictions (write SPECTATOR), RaceResults (write REFEREE,ADMIN), Users (create/delete ADMIN).
- **Lưu ý `ICurrentUser`**: file `Application/Common/Interfaces/ICurrentUser.cs` **đã có nhưng toàn bộ bị comment** (chưa wire DI, chưa implement) → effectively chưa dùng được. Hiện identity vẫn resolve thủ công trong controller bằng `User.FindFirst("userId")`/`NameIdentifier` (xem `AdminController`, `RaceExecutionController`, `PredictionsController`, `AuthController`). Cần uncomment + implement + đăng ký DI để dùng thật.
- **Admin duyệt user** (`AdminController`, `[Authorize(Roles="ADMIN")]`):
  - `GET /api/admin/users/pending`, `POST /api/admin/users/{id}/approve`, `POST /api/admin/users/{id}/reject`.
- **Flow 1 — Đăng ký & duyệt ngựa** (`Api/Controllers/HorsesController.cs` + `Application/Usecases/Horses/`): toàn bộ vòng đời ngựa đã hiện thực đầy đủ.
  - `POST /api/horses` [HORSE_OWNER] — tạo ngựa, Owner lấy từ JWT, `Status=Pending`.
  - `GET /api/horses?status=` — Owner thấy ngựa của mình, Admin thấy tất cả (lọc theo status).
  - `GET /api/horses/{id}`, `PUT /api/horses/{id}` [HORSE_OWNER, kiểm ownership], `DELETE /api/horses/{id}` [HORSE_OWNER].
  - `POST /api/horses/{id}/approve` [ADMIN] → `Approved`; `POST /api/horses/{id}/reject` [ADMIN] (lý do bắt buộc) → `Rejected`; `POST /api/horses/{id}/revoke` [ADMIN] → `Revoked` + auto-cancel các Entry `Pending` dùng ngựa đó.
  - Status ngựa: `Pending → Approved | Rejected`, và revoke (`Approved →` hủy). Hằng số: `Domain/Aggregates/Constants/HorseStatus.cs`, `EntryStatus.cs`. Repo: `IHorseRepository`, `IEntryRepository`. `RevokeHorse` dùng transaction tường minh (`_context.Database.BeginTransactionAsync`).
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

> Cập nhật: **2026-07-08** (đối chiếu code với báo cáo FE `.claude/BE_MISSING_APIS (1).md`; verify trực tiếp file nguồn). Danh sách việc + tiến độ: [.claude/TASKS.md](../.claude/TASKS.md).
> Lưu ý lịch sử: docs cũ (≤2026-06-25) mô tả Flow 3–8 "chỉ CRUD generic, chưa có orchestration". **Điều đó đã lỗi thời** — toàn bộ nghiệp vụ lõi Flow 3–8 nay nằm ở `Application/Usecases/RaceExecution/*` (use case đặc thù, không phải CRUD).
>
> **⚠️ Lỗi/nợ đã verify 2026-07-08 (chi tiết + cách sửa ở [.claude/TASKS.md](../.claude/TASKS.md)):**
> - 🔴 **User Management:** `CreateUserCommandHandler` lưu **password plaintext** (không BCrypt); `GetUserList` không phân trang/filter, response 5 field; `PUT /api/users/{id}` thiếu gate ADMIN.
> - 🔴 **`GET /api/races/{id}/pause` đang `[Authorize(Roles="REFEREE,ADMIN")]`** → referee phá Blind Double-Entry, phải đổi ADMIN-only.
> - 🟠 **Flow 6:** `RejectViolation` không set `Penalty="None"`; `GetAdminViolations` thiếu `AdminNote` + `violatorRole` hardcode "JOCKEY"; `UpdateViolation` `validPenalties` chặn `"None"` + không rollback standings. (Báo cáo FE ghi các fix này "đã fix" nhưng **code chưa có**.)

**✅ Orchestration vận hành đua** — `Application/Usecases/RaceExecution/*` (14 use case) + `Api/Controllers/RaceExecutionController.cs` (prefix `api/races`, role-locked). Build pass, MediatR tự đăng ký:
- **Flow 3:** `POST {id}/open-registration`, `POST {id}/close-registration` (auto-reject Entry Pending + **khóa Odds per-Entry** từ win rate + gán **GateNumber** + set OddsComputedAt; transaction). `Entry.Odds` (cột mới + migration `AddEntryOdds`). UpdateRace khóa NumberOfLegs khi rời Scheduled; CreatePrediction ưu tiên `Entry.Odds` đã khóa.
  - ⚠️ **TRÙNG LẶP:** ngoài bộ trên (ở `RaceExecutionController` + `Usecases/RaceExecution/*`), nay có thêm bộ song song ở `RacesController` + `Usecases/Races/*`: `POST {id}/registration/open` (`OpenRaceRegistrationCommand`), `POST {id}/registration/close` (`CloseRaceRegistrationCommand` + `IRegistrationService`), `POST {id}/odds/publish` (`PublishRaceOddsCommand` — tách bước khóa Odds riêng). Hai luồng đang cùng tồn tại, cần thống nhất chọn một.
- **Flow 3-4:** `POST {id}/start` (Scheduled→InProgress, **yêu cầu đã đóng ĐK**, tạo Legs, khóa cược), `POST {id}/resume` (Paused→InProgress), `GET {id}/execution`, `GET {id}/standings` (tổng Leg Points), `GET {id}/pause` (so sánh 2 submission).
- **Flow 4 blind:** `GET {id}/legs/{i}/referee-view` (ẩn input referee kia đến khi cả hai submit), `PUT {i}/draft` (validate-only — chưa có bảng draft), `POST {i}/submit` (append-only, so khớp → Confirmed/AutoMatched + tính Leg Points & LegOfficialResult, hoặc Conflicted + Paused; hết leg → PendingResult).
- **Flow 5:** `POST {id}/legs/{i}/override` (AdminOverride + lý do bắt buộc, resume).
- **Flow 8:** `POST {id}/publish` & `unpublish` — **atomic** (transaction tường minh): RaceResult + xếp hạng (tie-break tổng điểm→1st→2nd→leg cuối; Race DQ xuống đáy/0đ), Prize Points Owner/Jockey, cập nhật Career stats Jockey (`JockeyProfile`), quyết toán prediction (payout = bet × odds), cộng ví, SettlementRun/PredictionSettlement; unpublish rollback đối xứng (migration `AddRollbackFieldsForFlow8` lưu các trường để rollback). Publish/unpublish có ở **cả** `RaceExecutionController` (`/api/races/{id}/publish|unpublish`) **lẫn** `AdminController` (`/api/admin/races/{id}/publish|unpublish`) — **trùng lặp, cần thống nhất**. Màn review trước khi publish: `GET /api/admin/races/{id}/publication-review` (use case `Admin/ResultPublication`). **Leaderboard** `GET /api/leaderboards/career` & `/tournament/{id}` (`?role=`) tính on-read từ `PrizePointTransaction`.
- **Flow 6:** referee report (`POST /api/violations`, ReportedByRefereeId từ JWT, Status Pending, LegNumber mặc định leg hiện hành); admin `POST /api/admin/violations/{id}/approve` (Warning / Demote tụt 1 hạng leg + recompute Leg Points / Race DQ zero điểm toàn bộ leg) & `/reject` (lý do bắt buộc). Publish loại entry Race DQ → xếp cuối, `IsRaceDQ`, 0 Prize. **⚠️ Còn lỗi (2026-07-08):** reject không set `Penalty="None"` (UI vẫn hiện "Cảnh cáo"); `GetAdminViolations` không trả `AdminNote` + `violatorRole` hardcode; `UpdateViolation` chặn `Penalty="None"` (bấm Sửa record đã reject → 400) & không rollback standings khi đổi Approved→Pending/Rejected. `Violation` entity **đã có** cột `AdminNote` → không cần migration.
- **Flow 7 (đã refactor sang cược 1-Entry, 2026-06-28/29):** mỗi prediction chỉ chọn **1 Entry** về 1st (bỏ multi-entry Trifecta). Routes ở `PredictionsController` (prefix `api/predictions`): `GET races/{raceId}/odds` (`GetRacePredictionOdds`, mọi role đăng nhập trừ jockey-only), `POST races/{raceId}` body `{EntryId, BetAmount}` (`CreatePrediction`, SPECTATOR), `DELETE {id}/cancel` (`DeletePrediction`, SPECTATOR). `CreatePrediction` hardened — **khóa odds server-side** (ưu tiên `Entry.Odds`, fallback `PredictionOddsCalculator`), trừ ví (transaction), validate min10/50%/1-active/Scheduled, **SpectatorId từ JWT**; `DeletePrediction` → hủy + hoàn 100% + **chống hủy hộ** (ownership). Có use case `PlacePrediction` (alt, hiện **chưa wire vào controller** — controller dùng `CreatePrediction`). Migrations: `UpdatePredictionSingleEntryBet`, `UpdatePredictionDelete`, `CompleteFlow7PredictionBetting`. **`RunWeeklyTopUp`** (idempotent theo marker giao dịch) chạy qua **`WeeklyTopUpBackgroundService`** (mỗi giờ, catch-up) + trigger admin `POST /api/admin/points/weekly-topup`. **`LockUser`/`UnlockUser`** (`POST /api/admin/users/{id}/lock|unlock`) → khóa account, Spectator thì **hoàn cược Pending + đóng băng ví**.
- **Lấy danh tính từ JWT claims** (`userId`/NameIdentifier) trong controller — không tin body cho RefereeUserId/AdminUserId.
- **✅ Leaderboard đã có:** `LeaderboardsController` — `GET /api/leaderboards/career` & `GET /api/leaderboards/tournament/{id}` (`?role=`), tính on-read từ `PrizePointTransaction`.
- **Còn lại (nợ kỹ thuật + lỗi 2026-07-08 — xem [.claude/TASKS.md](../.claude/TASKS.md)):** 🔴 User Management (password plaintext ở `CreateUser`, `GetUserList` không phân trang/thiếu field, `PUT /api/users/{id}` thiếu gate ADMIN, `pause` cho referee); 🟠 Flow 6 (reject penalty / AdminNote / rollback standings); bảng draft persist (`SaveLegDraft` validate-only); abstraction `ICurrentUser` (file có nhưng bị comment, identity vẫn resolve thủ công); tie-break cuối "quyết định Admin" thủ công; **trùng lặp endpoint đăng ký/khóa Odds (RaceExecution vs Races) và publish (RaceExecution vs Admin)** — cần thống nhất; use case `PlacePrediction` chưa wire vào controller.

**Map use case ↔ route (Flow 3–8, đều là nghiệp vụ thật, không phải CRUD generic):**

| Use case (`Usecases/RaceExecution/`) | Route | Flow |
|---|---|---|
| `OpenRegistration` / `CloseRegistration` | `POST /api/races/{id}/open-registration` · `close-registration` (auto-reject Entry Pending + khóa Odds + gán GateNumber) | 3 |
| `StartRace` | `POST /api/races/{id}/start` (yêu cầu đã đóng ĐK, tạo Legs, khóa cược) | 3-4 |
| `ResumeRace` | `POST /api/races/{id}/resume` | 5 |
| `GetRaceExecution` / `GetRaceStandings` / `GetRacePause` | `GET /api/races/{id}/execution` · `standings` · `pause` | 4 |
| `GetRefereeLegView` | `GET .../legs/{i}/referee-view` (ẩn input referee kia đến khi cả hai submit) | 4 |
| `SaveLegDraft` | `PUT .../legs/{i}/draft` (validate-only — chưa persist) | 4 |
| `SubmitLegResult` | `POST .../legs/{i}/submit` (append-only, so khớp → AutoMatched/Conflicted + Leg Points) | 4 |
| `OverrideLegResult` | `POST .../legs/{i}/override` (AdminOverride + lý do bắt buộc) | 5 |
| `PublishRaceResult` / `UnpublishRaceResult` | `POST /api/races/{id}/publish` · `unpublish` (atomic) | 8 |

Ngoài ra: Flow 6 (Violations approve/reject → áp standings) ở `AdminController`; Flow 7 (`CreatePrediction` khóa odds server-side + trừ ví, weekly top-up, lock/unlock) ở `Predictions`/`Admin`. Tất cả lấy danh tính từ JWT claims, **không tin body**.

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

## 8. Cấu hình & tài khoản test

`Api/appsettings.json`:
- `ConnectionStrings:DefaultConnection` → PostgreSQL `localhost:5433/HorseRaceDB` (user/pass `postgres`).
- `JwtSettings`: SecretKey, Issuer `HorseRaceAPI`, Audience `HorseRaceClient`, access 60 phút, refresh 7 ngày.

Tài khoản seed (password trong DatabaseSeeder):
| Email | Mật khẩu | Role |
|-------|----------|------|
| admin@hrs.com | Admin@123 | ADMIN |
| ref1@hrs.com / ref2@hrs.com | Ref@123 | REFEREE |
| owner@hrs.com | Owner@123 | HORSE_OWNER |
| jockey@hrs.com | Jockey@123 | JOCKEY |
| spectator@hrs.com | Spectator@123 | SPECTATOR |
| pending.referee@hrs.com | Pending@123 | REFEREE (Pending) |

## 9. Lưu ý khi làm việc

- **Không** đưa logic nghiệp vụ vào Controller — chỉ forward sang MediatR.
- Tôn trọng ranh giới: interface I/O khai báo ở `Application/Common/`, hiện thực ở `Infrastructure/`.
- Command đổi dữ liệu nên dựa vào `UnitOfWorkBehavior` để commit; với thao tác atomic nhiều bước (Flow 8) dùng transaction tường minh qua `IUnitOfWork`.
- Thêm entity mới: tạo Entity → Configuration → DbSet → migration.
- Endpoint cần phân quyền: dùng `[Authorize(Roles = "...")]`.
