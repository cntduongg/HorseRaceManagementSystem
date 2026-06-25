# HRS Backend — CLAUDE.md

> Backend của Horse Race Management System. Xem [tổng quan dự án & 8 Main Flows](../CLAUDE.md) và [Frontend](../HorseRace.FE/CLAUDE.md).

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
| `Prediction` | Cược dự đoán: `BetAmount`, `OddsLocked1/2/3`, `Status`. (Flow 7) |
| `SettlementRun` / `PredictionSettlement` | Quá trình quyết toán cược. (Flow 8) |
| `PrizePointTransaction` | Cộng/trừ Prize Points cho Owner/Jockey. (Flow 8) |
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
- **Admin tiện ích** (`AdminController`, ADMIN — ✅ thêm 2026-06-25): `GET /api/admin/violations` (`GetAdminViolations`), `GET /api/admin/points/balances|transactions` + `POST /api/admin/points/adjust` (`PointsManagement`), `GET /api/admin/discrepancies` + `POST /{id}/resolve` (`Discrepancies` — **entity + migration `AddDiscrepancy` mới**).
- **Phân quyền (Authorization)** ✅ **đã phủ toàn bộ** (2026-06-25): mọi controller có class-level `[Authorize]` trừ `AuthController` (cố ý public cho register/login/forgot/reset/refresh; `[Authorize]` ở logout/profile). Role-locked: `Admin`/`Roles`/`PrizePointTransactions`/`SettlementRuns`/`PredictionSettlements` = ADMIN; `Legs`/`LegRefereeEntries`/`LegOfficialResults`/`Violations` = REFEREE,ADMIN. Controller đọc-đa-vai-trò (GET cho mọi user đăng nhập, **gate write theo role**): Races/Tournaments/PointWallets/WalletTransactions (write ADMIN), Horses (write HORSE_OWNER), Predictions (write SPECTATOR), RaceResults (write REFEREE,ADMIN), Users (create/delete ADMIN).
- **Lưu ý `ICurrentUser`**: interface này **KHÔNG tồn tại** trong code (docs cũ bịa). Hiện identity resolve thủ công trong controller bằng `User.FindFirst("userId")`/`NameIdentifier` (xem `AdminController`, `RaceExecutionController`, `AuthController`). Nên tạo abstraction dùng chung.
- **Admin duyệt user** (`AdminController`, `[Authorize(Roles="ADMIN")]`):
  - `GET /api/admin/users/pending`, `POST /api/admin/users/{id}/approve`, `POST /api/admin/users/{id}/reject`.
- **Flow 1 — Đăng ký & duyệt ngựa** (`Api/Controllers/HorsesController.cs` + `Application/Usecases/Horses/`): toàn bộ vòng đời ngựa đã chạy thật trên DB.
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
  - **Lưu ý:** không dùng `ICurrentUser` (chưa tồn tại) — identity resolve trong controller bằng `User.FindFirst("userId")`; status là string literal; handler dùng `IApplicationDbContext` trực tiếp.
- **Read services** (`Infrastructure/Services/`): `RaceReadService`, `TournamentReadService`, `EntryReadService`, `RaceResultReadService` — truy vấn đọc.
- **Hạ tầng**: JWT config + custom 401/403 ProblemDetails (Program.cs), CORS mở (`FrontendPolicy`), `GlobalExceptionHandler`, `ApiResponseFilter`, auto `MigrateAsync()` + seed khi Development.
- **Seeder** (`Infrastructure/Data/Seed/DatabaseSeeder.cs`): tạo 5 role + tài khoản test (xem mục 8).

## 6. Tình trạng Flow 3–8 — CRUD đã lưu DB, CHƯA có orchestration ⚠️ đọc kỹ

> Cập nhật: 2026-06-25 (đối chiếu lại code; merge nhánh `duongcnt` đã thay nhiều handler đặc thù bằng CRUD generic).

**✅ Đã bổ sung orchestration vận hành đua (2026-06-25)** — `Application/Usecases/RaceExecution/*` + `Api/Controllers/RaceExecutionController.cs` (prefix `api/races`, role-locked). Build pass, MediatR tự đăng ký:
- **Flow 3-4:** `POST {id}/start` (Scheduled→InProgress, tạo Legs, khóa cược), `POST {id}/resume` (Paused→InProgress), `GET {id}/execution`, `GET {id}/standings` (tổng Leg Points), `GET {id}/pause` (so sánh 2 submission).
- **Flow 4 blind:** `GET {id}/legs/{i}/referee-view` (ẩn input referee kia đến khi cả hai submit), `PUT {i}/draft` (validate-only — chưa có bảng draft), `POST {i}/submit` (append-only, so khớp → Confirmed/AutoMatched + tính Leg Points & LegOfficialResult, hoặc Conflicted + Paused; hết leg → PendingResult).
- **Flow 5:** `POST {id}/legs/{i}/override` (AdminOverride + lý do bắt buộc, resume).
- **Flow 8:** `POST {id}/publish` & `unpublish` — **atomic** (transaction tường minh): RaceResult + xếp hạng, Prize Points Owner/Jockey, quyết toán prediction (payout = bet × odds), cộng ví, SettlementRun/PredictionSettlement; unpublish rollback.
- **Flow 7:** `CreatePredictionCommandHandler` đã hardening — **khóa odds server-side** (bỏ giá trị client), trừ ví ngay (transaction), validate min 10 / 50% số dư / 1 dự đoán active mỗi race / race Scheduled; `DeletePrediction` → hủy + hoàn 100% (giữ audit, status `Cancelled`).
- **Lấy danh tính từ JWT claims** (`userId`/NameIdentifier) trong controller — không tin body cho RefereeUserId/AdminUserId.
- **Còn lại (chưa làm):** odds chưa lưu per-Entry (tính deterministic lúc đặt cược); chưa có bảng draft; chưa tích hợp Violation vào standings/publish; top-up ví +100 mỗi thứ Hai; Flow 2 lõi (xem §5); chuyển `SpectatorId` sang claims ở PredictionsController.

**Đã hết stub:** ~116 handler hiện đều **persist DB thật + validate input/FK** (không còn `Random.Shared` hay "Tạm thời chưa lưu DB"). Ví dụ `CreateRace`, `CreatePrediction`, `CreateLegRefereeEntry`, `CreateSettlementRun`… đều `_context.<Set>.Add(...)` + `SaveChangesAsync`.

**Nhưng đây mới là CRUD generic, CHƯA phải nghiệp vụ lõi.** Các feature Flow 3–8 (`Races`, `Legs`, `LegRefereeEntries`, `LegOfficialResults`, `Violations`, `Predictions`, `PredictionSettlements`, `SettlementRuns`, `PointWallets`, `WalletTransactions`, `PrizePointTransactions`, `Tournaments`, `RaceResults`) chỉ có 5 action chuẩn **Create/Update/Delete/GetList/GetDetail** — controller cũng chỉ phơi CRUD + `{id}` (chưa có route hành động đặc thù như close-registration, compute-odds, start, submit, resolve, publish). Cụ thể còn thiếu:
- **Flow 3:** mở/đóng đăng ký → auto-reject Entry `Pending` + **tính & khóa Odds** (win rate / avg finish), gán `GateNumber`, khóa Legs khi `In Progress`.
- **Flow 4 (blind double-entry):** `CreateLegRefereeEntry` mới chèn bản ghi + chống trùng (1 Referee/Leg/Entry); **chưa** ẩn input Referee A khỏi B đến khi cả hai submit, **chưa** so khớp 2 submission → `Confirmed (AutoMatched)` / `Conflicted` + `Paused`, **chưa** tính Leg Points & xếp hạng khi hết Legs.
- **Flow 5:** chưa có use case resolve conflict (`Confirmed (AdminOverride)` + lý do bắt buộc, audit trail).
- **Flow 6:** `Violation` mới CRUD; chưa Approve→áp dụng vào standings (Demote/Race DQ/per-Leg DQ).
- **Flow 7:** `CreatePrediction` đang **nhận odds từ client** (phải khóa server-side), cho chọn 1/2/3 (spec là dự đoán 1st), **chưa** trừ ví ngay, **chưa** validate min 10 / max 50% số dư / 1 dự đoán active mỗi Race, **chưa** cancel hoàn tiền; **chưa** có top-up +100 mỗi thứ Hai.
- **Flow 8:** `CreateSettlementRun` mới lưu 1 record từ số liệu client; **chưa** tính payout (`BetAmount × OddsLocked`), cộng Prize Points, recalc leaderboard, chuyển Prediction → `Settled`; **chưa** Publish/Unpublish **atomic**.

**Contract đã được FE định nghĩa sẵn (dùng làm spec khi hiện thực hóa).** FE (`HorseRace.FE/src/api/referee.js`, `admin.js`, `spectator.js`) đã build UI và gọi sẵn các endpoint nghiệp vụ sau — **BE cần tạo đúng các route này:**
- **Flow 3-4 (Race execution):** `POST /api/races/{id}/start`, `POST /api/races/{id}/resume`, `GET /api/races/{id}/execution`, `GET /api/races/{id}/standings`, `GET /api/races/{id}/pause`.
- **Flow 4 (blind double-entry):** `GET /api/races/{id}/legs/{legIndex}/referee-view` (ẩn input referee kia đến khi cả hai submit), `PUT /api/races/{id}/legs/{legIndex}/draft`, `POST /api/races/{id}/legs/{legIndex}/submit` (trả `{ status: 'Matched'|'Conflicted', ... }`).
- **Flow 5 (resolve):** `POST /api/races/{id}/legs/{legIndex}/override` (body `{ decisions:[{entryId, officialPosition}], overrideReason }`).
- **Flow 6 (violations):** approve/reject → áp standings (FE `AdminViolationsPage` đang chờ).
- **Flow 7-8 (betting/settlement):** `POST /api/predictions` phải khóa odds server-side + trừ ví; cần Publish/Unpublish + settlement (FE `AdminPointManagementPage`, `RacesBettingPage` đang chờ).

**Khi hiện thực hóa Flow 3–8:** theo pattern Flow 1 (RevokeHorse) — thêm **use case đặc thù** (vd `Races/CloseRegistration`, `Legs/SubmitRefereeResult`, `Races/PublishResult`) thay vì nhét vào CRUD; tạo repository interface ở `Application/Common/`, impl ở `Infrastructure/Repositories/`, đăng ký DI; Command dựa `UnitOfWorkBehavior` để tự commit (hoặc `IUnitOfWork.SaveChangesAsync` tường minh khi cần ID vừa sinh); lấy danh tính từ `ICurrentUser`, **không tin body** (vd `SpectatorId`, `RefereeUserId`, `TriggeredByAdminId` hiện đang lấy từ request — cần chuyển sang `ICurrentUser`). Flow 8 phải atomic (transaction tường minh qua `IUnitOfWork`).

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
