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
| `Horse` | `Status`: Pending\|Approved\|Rejected, `RejectionReason`, `ApprovedBy/At`. **`Stamina`** int 0..3 (mặc định 3) + **`HealthStatus`** computed (`3=Healthy·2=Fair·1=Weak·0=Exhausted`) — migration `AddHorseStamina`; Publish trừ 1 cho ngựa có đua, reset 3 cho ngựa `Approved` nghỉ. (Flow 1) |
| `JockeyProfile` | Hồ sơ nài để Owner tìm kiếm. (Flow 2) |
| `JockeyInvitation` | Lời mời nài cho (Race+Horse). (Flow 2) |
| `Tournament` | Giải đấu (tên, venue, logo, ngày). (Flow 3) |
| `Race` | `NumberOfLegs` (1–10), `MaxHorses`, `Referee1Id`/`Referee2Id`, `Status` (Scheduled→InProgress→Paused→PendingResult→Finished→Cancelled), `ScheduledStartTime`/`ScheduledEndTime` (khung giờ để chống trùng lịch), `RegistrationOpen/CloseAt`, `OddsComputedAt`, `PublishedAt`. (Flow 3) |
| `Entry` | Cặp Horse+Jockey nộp vào Race. `Status`: Pending\|Approved\|Rejected\|Withdrawn, `GateNumber`, `Odds` (khóa khi đóng ĐK). (Flow 2) |
| `Leg` | PK ghép `(RaceId, LegNumber)`. `Status` (blind): Pending\|AwaitingSecondReferee\|Confirmed\|Conflicted\|Resolved; **`ExecutionStatus`** (vòng đời per-leg, hằng số ở `Domain/Aggregates/Constants/LegExecutionStatuses.cs`): Pending\|PredictionOpen\|InProgress\|AwaitingResult\|Completed\|Cancelled; `StartedAt`/`FinishedAt`/`ConfirmedAt`, `PredictionOpenedAt`/`PredictionClosedAt`; `ConfirmationType` (AutoMatched\|AdminOverride), `AdminOverrideReason`. (Flow 4-5) |
| `LegRefereeEntry` | Bản ghi blind của từng Referee/Leg (append-only). (Flow 4) |
| `LegRefereeDraft` | Nháp thứ hạng của referee (upsert, KHÔNG append-only) — để khôi phục khi quay lại (migration `AddLegRefereeDraft`). (Flow 4) |
| `LegOfficialResult` | Kết quả Leg chính thức sau confirm. (Flow 4-5) |
| `Violation` | `ViolationType`, `Penalty` (Warning\|Demote\|DQ\|None), `Status` Pending/Approved/Rejected, `AdminNote`. (Flow 6) |
| `RaceResult` | Vị trí & điểm chung cuộc của Entry. (Flow 8) |
| `PointWallet` / `WalletTransaction` | Ví điểm Spectator & lịch sử giao dịch. (Flow 7) |
| `Prediction` | Cược **1 Entry về 1st của MỘT Leg** (per-leg): `RaceId` + **`LegNumber`** (NOT NULL, **FK ghép `(RaceId, LegNumber)` → `Legs`**, `OnDelete: Restrict` — migration `AddLegPredictionRelationship`), `FirstEntryId`, `BetAmount`, `OddsLocked1`, `Status` (Pending/Locked/Settled/Cancelled), `CancelledAt`. Các cột `SecondEntryId/ThirdEntryId/OddsLocked2/3` vẫn còn nhưng **không dùng** (nullable, di sản multi-entry). (Flow 7) |
| `SettlementRun` / `PredictionSettlement` | Quá trình quyết toán cược. (Flow 8) |
| `PrizePointTransaction` | Cộng/trừ Prize Points cho Owner/Jockey. (Flow 8) |
| `Discrepancy` | Bản ghi tranh chấp Admin xử lý. (Flow 5) |
| `ReviewHistory` | **Audit trail append-only** (✅ 2026-06-27, mở rộng 2026-07-14): `EntityType` (User/Horse/Entry/Race/Violation), `EntityId`, `Action` (Approved/Rejected/Revoked/Published/Unpublished/PenaltyChanged/Updated), `Reason`, `BeforeData`/`AfterData` (jsonb snapshots), `AdminId`, `CreatedAt`. Ghi khi Admin duyệt hồ sơ, publish/unpublish race, approve/reject/update violation. (Flow 1-2, 6, 8) |
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
- **Phân quyền (Authorization)** ✅ **đã phủ toàn bộ** (2026-06-25): mọi controller có class-level `[Authorize]` trừ `AuthController` (cố ý public cho register/login/forgot/reset/refresh; `[Authorize]` ở logout/profile). Role-locked: `Admin`/`Roles`/`PrizePointTransactions`/`SettlementRuns`/`PredictionSettlements` = ADMIN; `Legs`/`LegRefereeEntries`/`LegOfficialResults`/`Violations` = REFEREE,ADMIN. Controller đọc-đa-vai-trò (GET cho mọi user đăng nhập, **gate write theo role**): Races/Tournaments/PointWallets/WalletTransactions (write ADMIN), Horses (write HORSE_OWNER), Predictions (write SPECTATOR), RaceResults (write REFEREE,ADMIN), Users (**create/update/delete ADMIN**), `pause` execution ADMIN-only.
- **Lưu ý `ICurrentUser`**: file `Application/Common/Interfaces/ICurrentUser.cs` **đã có nhưng toàn bộ bị comment** (chưa wire DI, chưa implement) → effectively chưa dùng được. Hiện identity vẫn resolve thủ công trong controller bằng `User.FindFirst("userId")`/`NameIdentifier` (xem `AdminController`, `RaceExecutionController`, `PredictionsController`, `AuthController`). Cần uncomment + implement + đăng ký DI để dùng thật.
- **Admin duyệt user** (`AdminController`, `[Authorize(Roles="ADMIN")]`):
  - `GET /api/admin/users/pending`, `POST /api/admin/users/{id}/approve`, `POST /api/admin/users/{id}/reject`.
- **Flow 1 — Đăng ký & duyệt ngựa** (`Api/Controllers/HorsesController.cs` + `Application/Usecases/Horses/`): toàn bộ vòng đời ngựa đã hiện thực đầy đủ.
  - `POST /api/horses` [HORSE_OWNER] — tạo ngựa, Owner lấy từ JWT, `Status=Pending`.
  - `GET /api/horses?status=` — Owner thấy ngựa của mình, Admin thấy tất cả (lọc theo status).
  - `GET /api/horses/{id}`, `PUT /api/horses/{id}` [HORSE_OWNER, kiểm ownership, **chỉ chặn khi `Approved`**], `DELETE /api/horses/{id}` [HORSE_OWNER, kiểm ownership].
  - `GET /api/horses/{id}/statistics` — thống kê ngựa + `Stamina` hiện tại (✅ thêm 2026-07-25, `Horses/GetHorseStatistics`).
  - `POST /api/horses/{id}/resubmit` [HORSE_OWNER] — ngựa `Rejected` → `Pending`, xóa `RejectionReason`; check ownership (✅ thêm 2026-07-25, `Horses/ResubmitHorse`).
  - `POST /api/horses/{id}/approve` [ADMIN] → `Approved`; `POST /api/horses/{id}/reject` [ADMIN] (lý do bắt buộc) → `Rejected`; `POST /api/horses/{id}/revoke` [ADMIN] → `Revoked` + auto-cancel các Entry `Pending` dùng ngựa đó.
  - Status ngựa: `Pending → Approved | Rejected`, `Rejected → Pending` (resubmit), và revoke (`Approved →` hủy). Hằng số: `Domain/Aggregates/Constants/HorseStatus.cs`, `EntryStatus.cs`. Repo: `IHorseRepository`, `IEntryRepository`. `RevokeHorse` dùng transaction tường minh (`_context.Database.BeginTransactionAsync`).
  - **Thể lực:** `Horse.Stamina` (0..3) do `PublishRaceResult` cập nhật, không phải Owner nhập.
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

> Cập nhật: **2026-07-25** (đọc lại toàn bộ code base + verify trực tiếp file nguồn). Build **0 lỗi / 10 warning**. Việc cần làm: [.claude/TASKS.md](../.claude/TASKS.md).
> Lưu ý lịch sử: docs cũ (≤2026-06-25) mô tả Flow 3–8 "chỉ CRUD generic, chưa có orchestration". **Điều đó đã lỗi thời** — toàn bộ nghiệp vụ lõi Flow 3–8 nay nằm ở `Application/Usecases/RaceExecution/*` (use case đặc thù, không phải CRUD).
>
> ### 🆕 Thay đổi nghiệp vụ lớn — đợt 2026-07-20 → 07-25
>
> **① Thang điểm TUYẾN TÍNH theo sĩ số** (`RaceExecutionConstants`) — **breaking change**
> ```csharp
> // fieldSize (N) = số Entry Approved của race
> LegPointsFor(pos, resultStatus, fieldSize)  => N - p + 1        // DNF/DQ = 0; p > N = 0
> PrizePointsFor(finalPosition, fieldSize)    => (N - p + 1) * 200  // PrizePointUnit = 200
> ```
> Thay thang cứng cũ (Leg 6/5/4/3/2/1, Prize 1000/600/400/200/100). Neo: race 5 ngựa → 1st = 5×200 = 1000, bằng mức cũ. Lý do: race đông ngựa thì hạng chót không còn bị 0 điểm oan.
> **6 caller đã cập nhật:** `SubmitLegResult`, `OverrideLegResult`, `PublishRaceResult`, `UnpublishRaceResult`, `ApproveViolation`, `UpdateViolation`. `UnpublishRaceResult` tính lại `fieldSize` = số Entry `Approved` để rollback **đúng bằng** lúc cộng.
>
> **② `ValidatePositions(positions, fieldSize)`** — dùng chung `SubmitLegResult` + `OverrideLegResult`. Vị trí phải ∈ `1..N`, **không trùng**, và **liên tục từ 1** (bỏ `-1` DNF / `-2` DQ ra thì đúng `1..k`). Trả `string?` — `null` là hợp lệ, khác `null` là message lỗi tiếng Anh.
>
> **③ Chặn Publish khi còn Violation `Pending`** — `PublishRaceResult.cs:60-66` ném `InvalidOperationException("There are still {n} unresolved violation(s)…")`. Test: `PublishBlockedByViolationTests`.
> ⚠️ `GET /api/admin/races/{id}/publication-review` **chưa** trả số vi phạm Pending → FE không khóa nút Publish trước được (T-04).
>
> **④ Thể lực ngựa (`Horse.Stamina`)** — `PublishRaceResult.cs:310-333`: ngựa **có đua** → `Stamina = max(0, Stamina-1)`; ngựa `Approved` **không đua race này** → reset `Stamina = 3`. Lộ ra ở `GetRaceLive`, response odds, `GET /api/horses/{id}/statistics`. Hiện chỉ là chỉ số hiển thị, chưa ảnh hưởng odds/kết quả.
> ⚠️ **`UnpublishRaceResult` KHÔNG rollback Stamina** → publish/unpublish/publish lại trừ 2 lần (T-03).
>
> **⑤ `LegExecutionStatuses`** (`Domain/Aggregates/Constants/`) — nguồn chân lý duy nhất cho cửa cược:
> `BettingOpen = {Pending, PredictionOpen, AwaitingResult}`; khóa khi ∈ `{InProgress, Completed, Cancelled}`. Dùng ở `GetRaceLive` (cờ `IsBettingOpen` mỗi leg) và `GetLegPredictionOdds`.
>
> **⑥ `DELETE /api/races/{id}` nay là SOFT-CANCEL** — `DeleteRaceCommandHandler` chỉ cho khi `Status == Scheduled`, set `Status = Cancelled` (không xóa row). **Không có** cột `IsDeleted` trong domain. Kèm theo, các handler chặn race `Cancelled`: `UpdateRace`, `PublishRaceOdds`, `RaceLifecycleCoordinator.CloseRegistration`.
>
> **⑦ Chuẩn hóa & chống trùng số điện thoại** — `Domain/Aggregates/Constants/PhoneNumberNormalizer.cs` (VN mobile: đưa về `84` + 9 số; chấp nhận `0xxxxxxxxx` / `84xxxxxxxxx` / `0084…` / 9 số; prefix hợp lệ `843/845/847/848/849`), `InvalidPhoneNumberException`, `Infrastructure/Services/NormalizeUserPhoneNumberInterceptor.cs` (SaveChanges interceptor), `User.NormalizedPhoneNumber` + unique index có filter.
> 🔴 **BUG: interceptor KHÔNG chạy.** `ServiceCollectionExtensions.AddInfrastructure` gọi `AddDbContext<ApplicationDbContext>` **2 lần** (dòng 25 và dòng 55). EF Core đăng ký `DbContextOptions<T>` bằng **`TryAdd`** → lần đầu (`DefaultConnection`, **không** interceptor) thắng. Lần 2 còn trỏ vào connection string `"PostgreSQL"` **không tồn tại** trong `appsettings.json`. Hậu quả: `NormalizedPhoneNumber` luôn null → unique index (filter `IS NOT NULL`) vô hiệu → nhánh 409 "phone đã tồn tại" là code chết. Xem [T-01](../.claude/TASKS.md).
>
> **⑧ `GlobalExceptionHandler` viết lại** — thêm `traceId` vào `ProblemDetails.Extensions`; map `InvalidPhoneNumberException` → 400; unique-violation trên `UX_Users_NormalizedPhoneNumber` → 409 riêng (**phải đặt trước** case `DbUpdateException` chung); map thêm PG error code `ForeignKeyViolation`/`NotNullViolation`/`CheckViolation`; 500 nay trả message chung, **không** lộ `exception.Message`.
> ⚠️ 3 message mới bằng **tiếng Việt** (`"Số điện thoại không hợp lệ."`, `"Số điện thoại đã tồn tại."`, `"Đã xảy ra lỗi trong hệ thống."`) — trái quy ước "BE dùng tiếng Anh" (T-05).
>
> **⑨ Endpoint mới**
> | Route | Use case | Ghi chú |
> |---|---|---|
> | `GET /api/horses/{id}/statistics` | `Horses/GetHorseStatistics` | thống kê + `Stamina` hiện tại |
> | `POST /api/horses/{id}/resubmit` | `Horses/ResubmitHorse` | `Rejected → Pending`, xóa `RejectionReason`; check ownership |
> | `POST /api/admin/points/daily-topup` | `Admin/PointsManagement/RunDailyTopUp` | nạp bù ví `Balance < 10` **lên đúng 10**; idempotent theo ngày (`Type="DailyTopUp"`, mốc `now.Date` UTC). ⚠️ **Không có background service** — chỉ chạy khi Admin bấm (T-06) |
> | `GET /api/admin/violations?search=&sort=&sortDirection=` | `Admin/GetAdminViolations` | search khớp loại vi phạm/mô tả/tên race/nài/ngựa; sort `violationType\|status\|penalty\|legNumber\|racename\|createdAt`, tie-break `ViolationId` |
>
> **⑩ Khác**
> - `OverrideLegResult` nay set `leg.ExecutionStatus = Completed` (trước đó leg admin-resolve báo `InProgress` trong khi `IsConfirmed = true`).
> - `DeletePrediction` **bỏ** check race `Scheduled` — chỉ cần prediction `Pending`. Đúng với per-leg: `StartLeg` chuyển toàn bộ prediction `Pending` của leg → `Locked`, nên cửa hủy tự đóng theo từng leg.
> - `UpdateHorse` nay chỉ chặn khi ngựa `Approved` (cho sửa ngựa `Rejected` để resubmit). Ownership vẫn được check ở cả Update/Delete/Resubmit.
>
> **⚠️ Nợ kỹ thuật còn lại:** `ICurrentUser` **vẫn bị comment toàn bộ** (`Application/Common/Interfaces/ICurrentUser.cs`, `Infrastructure/Services/CurrentUser.cs`, DI dòng 67) → identity resolve thủ công; **endpoint trùng ở 2 controller** (đăng ký RaceExecution vs Races; publish RaceExecution vs Admin); gán referee chưa validate role; tie-break cuối thủ công; **migration rác** (`AddHorseStamina1`, `AddLegPredictionRelationship1`, `Persistence/Migrations/HorseStamina` — cả 3 rỗng) và `AddLegPredictionRelationship` **thiếu `Down()`**.

**✅ Orchestration vận hành đua** — `Application/Usecases/RaceExecution/*` (~16 use case + `RaceLifecycleCoordinator`/`RaceRankingCalculator`/`RaceExecutionConstants`) + `Api/Controllers/RaceExecutionController.cs` (prefix `api/races`, role-locked). Build pass, MediatR tự đăng ký:
- **Flow 3:** `POST {id}/open-registration`, `POST {id}/close-registration` (auto-reject Entry Pending + **khóa Odds per-Entry** từ win rate + gán **GateNumber** + set OddsComputedAt; transaction). `Entry.Odds` (cột mới + migration `AddEntryOdds`). UpdateRace khóa NumberOfLegs khi rời Scheduled; CreatePrediction ưu tiên `Entry.Odds` đã khóa.
  - ⚠️ **TRÙNG LẶP:** ngoài bộ trên (ở `RaceExecutionController` + `Usecases/RaceExecution/*`), nay có thêm bộ song song ở `RacesController` + `Usecases/Races/*`: `POST {id}/registration/open` (`OpenRaceRegistrationCommand`), `POST {id}/registration/close` (`CloseRaceRegistrationCommand` + `IRegistrationService`), `POST {id}/odds/publish` (`PublishRaceOddsCommand` — tách bước khóa Odds riêng). Hai luồng đang cùng tồn tại, cần thống nhất chọn một.
- **Flow 3-4 (per-leg + auto-start):** khởi động đua theo **từng Leg** — `POST {id}/legs/{n}/start` (`StartLegCommand`, dùng `Leg.ExecutionStatus`); **worker `RaceAutoStartBackgroundService`** (quét mỗi ~15s, gửi `ProcessDueRaceStartsCommand` → `RaceLifecycleCoordinator.StartRaceAsync`) tự khởi động Race khi qua `ScheduledStartTime`. `StartRaceCommand`/`StartRace.cs` (khởi động cả Race) vẫn còn nhưng **hiện không wire vào controller** — chỉ coordinator/worker dùng đường coordinator. `POST {id}/resume` (Paused→InProgress), `GET {id}/execution`, `GET {id}/standings` (tổng Leg Points), `GET {id}/pause` (**ADMIN-only**, so sánh 2 submission). `Leg.ExecutionStatus`: `Pending → PredictionOpen → InProgress → AwaitingResult → Completed | Cancelled`.
- **Flow 4 blind:** `GET {id}/legs/{i}/referee-view` (ẩn input referee kia đến khi cả hai submit), `PUT {i}/draft` (**persist thật** — upsert vào `LegRefereeDraft`, có `MyDraftData` để khôi phục nháp), `POST {i}/submit` (append-only, **`ValidatePositions`** trước, so khớp → Confirmed/AutoMatched + tính **Leg Points tuyến tính `N−p+1`** & LegOfficialResult, hoặc Conflicted + Paused; hết leg → PendingResult). **Khi Leg Confirmed → quyết toán prediction của Leg đó ngay** (`SubmitLegResult.SettleLegPredictions`, payout bet×odds → ví, `Locked→Settled`).
- **Flow 4 — theo dõi trực tiếp (Spectator, ✅ thêm 2026-07-15):** `GET {id}/live` (`GetRaceLive`) trả snapshot gộp sẵn: tên ngựa/nài + GateNumber, trạng thái & vị trí từng leg (**chỉ leg Confirmed/Resolved mới có `Results`**), standings tạm tính, `CurrentLegIndex`, `SnapshotAt`. **Cố ý KHÔNG có `Referee1Submitted`/`Referee2Submitted`** (khác `GetRaceExecution`) và handler **không `Include(RefereeEntries)`** — giữ Blind Double-Entry.
  - **Push realtime qua SignalR:** hub `Api/Hubs/RaceLiveHub.cs` tại **`/api/hubs/race-live`** (đặt dưới `/api` để thừa hưởng proxy Vite → same-origin trong dev), group `race-{raceId}`, method `JoinRace`/`LeaveRace`, `[Authorize]` mọi role đăng nhập. Event đẩy về client: **`RaceLiveChanged`** mang đúng `RaceLiveResponse`.
  - **Kiến trúc đẩy:** handler gọi `IRaceLiveChangeTracker.MarkChanged(raceId)`; `RaceLiveBroadcastBehavior` (pipeline) drain sau `next()` rồi `Send(GetRaceLiveQuery)` → `IRaceLiveNotifier.PushAsync`. **⚠️ Behavior PHẢI đăng ký TRƯỚC `UnitOfWorkBehavior`** trong `AddApplication()` (đăng ký trước = nằm ngoài) để chỉ đẩy **sau commit**; đăng ký sau → client refetch trúng data cũ. `Drain()` phải chạy **trước** `Send` (chống đệ quy, vì query lồng đi lại chính behavior).
  - **MarkChanged ở 6 chỗ:** `RaceLifecycleCoordinator.StartRaceAsync` (nhánh `Started` — phủ **cả** `POST /start` lẫn worker auto-start), `SubmitLegResult` (matched + conflicted), `OverrideLegResult`, `ResumeRace`, `PublishRaceResult`, `UnpublishRaceResult` (2 cái cuối phủ luôn route trùng ở `AdminController`). **KHÔNG mark ở nhánh `AwaitingSecondReferee`** — đẩy lúc đó là báo "1 trọng tài vừa nộp", tức kênh phụ phá blind entry.
  - **JWT cho WebSocket:** `Program.cs` thêm `OnMessageReceived` đọc `access_token` từ query string, **có guard `path.StartsWithSegments(RaceLiveHub.Path)`** — thiếu guard thì `?access_token=` thành auth hợp lệ trên mọi endpoint. Thêm **vào trong** khối `options.Events` sẵn có (đừng gán `new JwtBearerEvents`, sẽ mất `OnChallenge`/`OnForbidden`).
  - **CORS:** policy thêm `.AllowCredentials()` (SignalR negotiate gửi `withCredentials: true`) và `Cors:AllowedOrigins` nay có `http://localhost:5173` (trước là **rỗng** → chặn mọi cross-origin). ⚠️ Không đổi sang `AllowAnyOrigin()`: kèm `AllowCredentials()` sẽ ném `InvalidOperationException` lúc khởi động.
  - **Hạn chế:** group lưu trong RAM tiến trình → chỉ đúng với 1 instance; chạy ≥2 replica cần Redis backplane.
- **Flow 5:** `POST {id}/legs/{i}/override` (AdminOverride + lý do bắt buộc, **`ValidatePositions`**, set `ExecutionStatus=Completed`, resume).
- **Flow 8:** `POST {id}/publish` & `unpublish` — **atomic** (transaction tường minh): **chặn nếu còn Violation `Pending`**, rồi RaceResult + xếp hạng (tie-break tổng điểm→1st→2nd→leg cuối; Race DQ xuống đáy/0đ), **Prize Points `(N−p+1)×200`** cho Owner/Jockey, cập nhật Career stats Jockey (`JockeyProfile`), **cập nhật `Horse.Stamina`** (−1 cho ngựa đua, reset 3 cho ngựa `Approved` nghỉ). **Lưu ý: quyết toán prediction nay chạy PER-LEG** (khi mỗi Leg Confirmed, xem Flow 4) — khối settle prediction trong `PublishRaceResult` đã bị vô hiệu hóa; Publish chỉ tạo `SettlementRun` khung. Unpublish rollback đối xứng Prize Points/leaderboard/Career stats (migration `AddRollbackFieldsForFlow8`) — **nhưng KHÔNG rollback `Horse.Stamina`** (T-03). **Unpublish body `{ reason }` bắt buộc** — ghi `ReviewHistory` (`Race`/`Unpublished` + before/after snapshot); Publish ghi `Race`/`Published`. Publish/unpublish có ở **cả** `RaceExecutionController` (`/api/races/{id}/publish|unpublish`) **lẫn** `AdminController` (`/api/admin/races/{id}/publish|unpublish`) — **trùng lặp, cần thống nhất**. Màn review trước publish: `GET /api/admin/races/{id}/publication-review` (`Admin/ResultPublication`). **Leaderboard** `GET /api/leaderboards/career` & `/tournament/{id}` (`?role=`) tính on-read từ `PrizePointTransaction`.
- **Flow 6:** referee report (`POST /api/violations`, ReportedByRefereeId từ JWT, Status Pending, LegNumber mặc định leg hiện hành); admin `POST /api/admin/violations/{id}/approve` (Warning / Demote tụt 1 hạng leg + recompute Leg Points / Race DQ zero điểm toàn bộ leg) & `/reject` (lý do bắt buộc, **set `Penalty="None"`**). `PUT /api/violations/{id}` **ADMIN-only**, ActorAdminId từ JWT, cho phép `Penalty="None"` + **rollback standings** khi sửa Approved→khác. `GET /api/admin/violations` trả `AdminNote` + `ViolatorRole` từ `Entry.Jockey.Role.Code`. Approve/Reject/Update đều ghi `ReviewHistory` (`Violation` + before/after jsonb). Publish loại entry Race DQ → xếp cuối, `IsRaceDQ`, 0 Prize.
- **Flow 7 (cược PER-LEG):** mỗi prediction cược **1 Entry về 1st của MỘT Leg cụ thể** (`Prediction.LegNumber`, có FK thật tới `Legs`). Routes ở `PredictionsController` (prefix `api/predictions`): `GET races/{raceId}/legs/{n}/odds` (`GetLegPredictionOddsQuery`, mọi role trừ jockey-only), `POST races/{raceId}/legs/{n}` body `{EntryId, BetAmount}` (`CreatePrediction`, SPECTATOR), `DELETE {id}/cancel` (`DeletePrediction`, SPECTATOR). `CreatePrediction` hardened — **khóa odds server-side** per-(race,leg,entry) qua `PredictionOddsCalculator.CalculateEntryLegOddsAsync`, trừ ví (transaction), validate min10/50%/**1-active-per-(race+leg)**/leg chưa `InProgress`, **SpectatorId từ JWT**; `DeletePrediction` → hủy + hoàn 100% + **chống hủy hộ** (nay **không** còn check race `Scheduled` — cửa hủy đóng theo leg vì `StartLeg` chuyển `Pending→Locked`). `PredictionStatus`: `Pending → Locked` (khi Leg start) `→ Settled | Cancelled`. Migrations: `UpdatePredictionSingleEntryBet`, `UpdatePredictionDelete`, `CompleteFlow7PredictionBetting`, `AddLegPredictionRelationship`.
  - **Odds ĐỘNG theo pool** (`Application/Usecases/Predictions/Common/PredictionOddsCalculator.cs`) — đây **không phải** odds tĩnh:
    - `BaseOdds` = `Entry.Odds` (khóa lúc đóng đăng ký, từ win rate ngựa).
    - `pressure = entryPool / max(avgPool, 1)` (`0.5` nếu entry chưa ai cược); `CurrentOdds = BaseOdds / √pressure`.
    - Pool tính **chỉ trong Leg đó**, bỏ prediction `Cancelled`. **Clamp `[1.10, 25.00]`**, làm tròn 2 chữ số (`AwayFromZero`).
    - Lúc đặt cược, `CalculateEntryLegOddsAsync` cộng **cả số tiền đang đặt** vào pool trước khi tính → odds khóa vào prediction đã phản ánh chính lệnh cược đó.
    - Response odds trả cả `baseOdds`, `currentOdds`, `entryPool`, `totalPool`, `horseStamina`, `horseHealthStatus`, và cờ **`isBettingOpen`** (theo `LegExecutionStatuses.IsBettingOpen` — leg `InProgress` vẫn **xem** được odds nhưng không đặt được).
  - **Nạp điểm:** **`RunWeeklyTopUp`** (+100, idempotent) chạy qua **`WeeklyTopUpBackgroundService`** (mỗi giờ, catch-up) + trigger admin `POST /api/admin/points/weekly-topup`. **`RunDailyTopUp`** (nạp bù ví `< 10` lên đúng 10, idempotent theo ngày UTC) chỉ có trigger admin `POST /api/admin/points/daily-topup` — **không có worker**.
  - **`LockUser`/`UnlockUser`** (`POST /api/admin/users/{id}/lock|unlock`) → khóa account, Spectator thì **hoàn cược Pending + đóng băng ví**. Admin points thêm `GET`+`PUT /api/admin/points/{userId}`.
- **Lấy danh tính từ JWT claims** (`userId`/NameIdentifier) trong controller — không tin body cho RefereeUserId/AdminUserId.
- **✅ Leaderboard đã có:** `LeaderboardsController` — `GET /api/leaderboards/career` & `GET /api/leaderboards/tournament/{id}` (`?role=`), tính on-read từ `PrizePointTransaction`.
- **Còn lại (xem [.claude/TASKS.md](../.claude/TASKS.md) để có cách sửa):** 🔴 **bug DI khiến interceptor chuẩn hóa SĐT không chạy** (T-01); abstraction `ICurrentUser` (file có nhưng **bị comment**, identity vẫn resolve thủ công — T-07); tie-break cuối "quyết định Admin" thủ công (T-10); **trùng lặp endpoint đăng ký/khóa Odds (RaceExecution vs Races) và publish (RaceExecution vs Admin)** — cần thống nhất (T-08); gán referee chưa validate đúng role (T-09); `StartRaceCommand` (khởi động cả Race) tồn tại nhưng không wire vào controller (T-11); Unpublish không rollback Stamina (T-03); migration rác + thiếu `Down()` (T-02); message tiếng Việt trong `GlobalExceptionHandler` (T-05).

**Map use case ↔ route (Flow 3–8, đều là nghiệp vụ thật, không phải CRUD generic):**

| Use case (`Usecases/RaceExecution/`) | Route | Flow |
|---|---|---|
| `OpenRegistration` / `CloseRegistration` | `POST /api/races/{id}/open-registration` · `close-registration` (auto-reject Entry Pending + khóa Odds + gán GateNumber) | 3 |
| `StartLeg` | `POST /api/races/{id}/legs/{n}/start` (khởi động **từng Leg**; `Leg.ExecutionStatus`) | 3-4 |
| `StartRace` / `ProcessDueRaceStarts` / `RaceLifecycleCoordinator` | (không route trực tiếp) — **worker auto-start** `RaceAutoStartBackgroundService` khi qua `ScheduledStartTime` | 3-4 |
| `ResumeRace` | `POST /api/races/{id}/resume` | 5 |
| `GetRaceExecution` / `GetRaceStandings` / `GetRacePause` | `GET /api/races/{id}/execution` · `standings` · `pause` (**pause ADMIN-only**) | 4 |
| `GetRaceLive` | `GET /api/races/{id}/live` (snapshot cho Spectator theo dõi trực tiếp; **cũng là payload push SignalR**) | 4 |
| `GetRefereeLegView` | `GET .../legs/{i}/referee-view` (ẩn input referee kia đến khi cả hai submit) | 4 |
| `SaveLegDraft` | `PUT .../legs/{i}/draft` (**persist thật** → `LegRefereeDraft`) | 4 |
| `SubmitLegResult` | `POST .../legs/{i}/submit` (append-only, so khớp → AutoMatched/Conflicted + Leg Points; **settle prediction của Leg**) | 4 |
| `OverrideLegResult` | `POST .../legs/{i}/override` (AdminOverride + lý do bắt buộc) | 5 |
| `PublishRaceResult` / `UnpublishRaceResult` | `POST /api/races/{id}/publish` · `unpublish` (atomic) | 8 |

Ngoài ra: Flow 6 (Violations approve/reject → áp standings) ở `AdminController`; Flow 7 (`CreatePrediction` khóa odds server-side + trừ ví, **cược per-leg**, weekly top-up, lock/unlock) ở `Predictions`/`Admin`. Tất cả lấy danh tính từ JWT claims, **không tin body**.

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
- **Background services** (đăng ký ở `Program.cs`): `WeeklyTopUpBackgroundService` (top-up ví thứ Hai) + `RaceAutoStartBackgroundService` (auto-start Race khi qua `ScheduledStartTime`, quét mỗi ~15s). **SignalR** hub `/api/hubs/race-live` (push diễn biến đua).

### ⛔ Quy ước kiểm thử: dự án KHÔNG có test tự động

Solution chỉ có **5 project sản phẩm** (`Api`, `Application`, `Domain`, `Infrastructure`, `Sharekernel`) — **không có test project**. `Application.Tests` đã bị **gỡ khỏi solution và xóa hẳn** (2026-07-25) theo quyết định của nhóm.

Nhóm **test thủ công** theo [.claude/TEST_PLAN_2026-07-22.md](../.claude/TEST_PLAN_2026-07-22.md). Khi làm việc trên repo này:

- **Không tạo file test**, không dựng lại test project, không thêm package test (xUnit / NUnit / Moq / InMemory provider…).
- **Không đề xuất "nên bổ sung test"** như một hạng mục cần làm, và **không coi việc thiếu test là điểm chặn** khi review code hay review plan.
- Kiểm chứng thay đổi bằng **`dotnet build HorseRace.sln`** (mục tiêu 0 lỗi) + **mô tả rõ cách test tay**: vào trang nào, bấm gì, kỳ vọng thấy gì. Nếu thay đổi ảnh hưởng luồng nào trong TEST_PLAN thì chỉ ra đúng case ID.

## 8. Cấu hình & tài khoản test

`Api/appsettings.json`:
- `ConnectionStrings:DefaultConnection` → PostgreSQL `localhost:5433/HorseRaceDB` (user/pass `postgres`).
- `JwtSettings`: SecretKey, Issuer `HorseRaceAPI`, Audience `HorseRaceClient`, access 60 phút, refresh 7 ngày.
- `RaceAutoStart:IntervalSeconds` (mặc định 15) — chu kỳ worker auto-start; `Cors:AllowedOrigins` (có `http://localhost:5173` cho SignalR + `.AllowCredentials()`).

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
