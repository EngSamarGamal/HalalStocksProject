# Kasheif (HalalStocks) — Engineering & Architecture Reference

> Grounded entirely in the project zip reviewed during this engagement (namespace `HalalStocks`,
> solution `Kasheif`). No assumptions beyond what's present in the codebase.

## 1. Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8.0 MVC (`net8.0`, nullable enabled, implicit usings enabled) |
| Data access | EF Core 8 (`Microsoft.EntityFrameworkCore.SqlServer` 8.0.*) + SQL Server, code-first migrations |
| Auth | ASP.NET Identity (`AddIdentity<AppUser, IdentityRole>`), custom `AppUserClaimsPrincipalFactory` |
| PDF generation | iText7 8.0.5 + `itext7.bouncy-castle-adapter` (used by the purification calculator) |
| Object storage | `AWSSDK.S3` 4.0.23.4 — talks to Cloudflare R2 (S3-compatible), with a local-disk fallback |
| Frontend | Server-rendered Razor views, Bootstrap 5 (RTL build), Tajawal font, jQuery, Select2, vanilla JS — no SPA framework |
| Testing | **None.** No test project exists in the solution. |
| CI/CD | **None.** `.github/` exists but contains no workflow files. |

## 2. Architectural Pattern

Every feature follows the same layering, with one known exception (see §9):

```
Controller  →  Service Interface (Services/Interfaces)  →  Service Implementation (Services/Implementations)  →  AppDbContext (EF Core)
```

- Services are registered in `Program.cs` via `builder.Services.AddScoped<IXService, XService>()`.
- Controllers depend on interfaces, never on `AppDbContext` or concrete implementations directly (except the one documented exception).
- ViewModels (`ViewModels/`, `ViewModels/Admin/`) sit between controllers and views; domain models (`Models/`) are not annotated with `[Required]`/`[MaxLength]`/etc. — **all `DataAnnotations` validation lives on ViewModels, not domain models.** This is deliberate and consistent across the codebase; new features should follow it.

## 3. Routing Conventions

- **No ASP.NET Core "Areas."** Attribute routing is used everywhere instead of conventional `{controller}/{action}` routing (only a single fallback default route exists in `Program.cs`).
- Admin controllers live under `Controllers/Admin/*`, each with `[Route("admin/...")]` and `[Authorize(Roles = "SuperAdmin,Admin,Editor")]` (Editor is excluded only on `SiteSettingsController`).
- Public controllers use root-level attribute routes, e.g. `[Route("recommendations")]`, `[Route("zakat")]`.
- Admin login is a separate route/view (`/admin/login`) from the public account system (`/account/login`), though both authenticate against the same `AppUser`/Identity tables.
- Error handling: `app.UseExceptionHandler("/error/500")` and `app.UseStatusCodePagesWithReExecute("/error/{0}")`, handled by a `[Route("error/{code:int}")]` action in `HomeController`.

## 4. Domain Model

### Core entities
- **Stock** — ticker (`TickerSymbol`), Arabic name (`NameAr`), category, `ShariahStatus` enum (`Halal, MustPurify, Prohibited, NotReviewed, AwaitingFinancialStatements, StatusBlocked`), a `[Flags] PurityGrade` enum for nuanced multi-grade classification, four Shariah screening ratios, plus two company-level figures added for the capital purification calculator: `TotalCompanyShares` (`long?` → bigint) and `TotalCompanyAssets` (`decimal?`, `HasPrecision(18,2)` — *not* the `(5,2)` used by the percentage ratios). Both nullable and `NULL` for every existing row; admins fill them in on the stock form under a بيانات الشركة المالية fieldset. Belongs to one `StockCategory`, many-to-many with `StockIndex`.
- **StockFinancialReview** — versioned per-period reviews per stock, cascades on delete from `Stock`.
- **CalculationSession / CalculationEntry** — the purification calculator's persisted "saved sessions" feature (separate from the ephemeral, non-persisted `/calculator` PDF tool — see §7).
- **ZakatRecord** — zakat-on-portfolio calculations, tied to `GoldPriceSnapshot` for the nisab threshold.
- **GoldPriceSnapshot** — cached gold price, unique index on `PriceDate`, three-tier fallback (in-memory cache → latest DB row → external GoldAPI call).
- **PortfolioRequest** — managed-portfolio lead-gen form with an admin pipeline (`New → UnderReview → Contacted → ActiveClient/Rejected`).
- **RecommendationRequest** — a user's request to access the Recommendations feature. One row per user (unique index on `UserId`), `RecommendationRequestStatus` enum (`Pending, Approved, Rejected`). Rejection/deletion resets the user to the same gated state as a first-time visitor.
- **StockRecommendation** — admin-curated stock recommendations. FK to `Stock` (unique index — one active recommendation per stock), all analytical fields (support/resistance/stop-loss/etc.) manually entered by admins, `StockRecommendationType` enum (`Trading, BuyOnDips, Hold, Expired`).
- **RecommendationBase** — abstract, unmapped C# base class (not its own `DbSet`, never configured via `builder.Entity<RecommendationBase>()`) holding the field set shared by `StockRecommendation` and `UserStockRecommendation` (stock FK, purity grade, trend, price levels, `RecommendationTypeId` FK, notes, timestamps). Each concrete subtype still gets its own table with no EF inheritance mapping involved — see §13.
- **UserStockRecommendation** — per-user "private recommendations" list (`RecommendationBase` + `UserId` FK to `AppUser`, cascade on delete). Unique index on `(UserId, StockId)` — a user can't add the same stock twice, but the same stock can exist in both this table and the general `StockRecommendations` table simultaneously with independent values. See §13.
- **CapitalPurificationRecord** — one saved capital-purification calculation per user (`UserId` cascade). Stores the inputs *and* a snapshot of every stock figure used (`Ticker`, `NameAr`, `TotalCompanyShares`, `TotalCompanyAssets`, `RibaActivityRatio`) alongside the derived `OwnershipPercent` (`18,8` — ownership can be a tiny fraction of a percent), `CapitalShare` and `PurificationAmount` (`18,2`), plus an `IsPaid` flag. `StockId` is **nullable with `DeleteBehavior.SetNull`** — deliberately, so deleting a stock leaves users' saved records fully readable instead of cascade-deleting their financial history; the UI hides the "تعديل" action on an orphaned row since it can no longer be reloaded into the calculator.
- **Article, PortfolioInfoSection, SiteSettings** — admin-editable CMS content.

### Relationships worth noting
- `Stock` ↔ `StockIndex` is the only many-to-many (plain join table, no extra columns).
- `Stock → StockFinancialReview` cascades on delete; every other FK relationship uses EF's default (restrict) behavior, **except** `StockRecommendation → Stock` (explicitly `DeleteBehavior.Restrict`) and `CapitalPurificationRecord → Stock` (explicitly `DeleteBehavior.SetNull`, with a nullable FK). EF's *default* for a required FK is cascade — worth checking on any new user-owned record that references `Stock`, since accepting the default there silently deletes user data when an admin removes a stock.
- `AppUser` (extends `IdentityUser`) already carries `FirstName`, `LastName`, and inherits `PhoneNumber`/`Email` — new features needing user contact info should reference these directly rather than duplicating them.

## 5. Data Layer Conventions

- Decimal precision is set explicitly via Fluent API (`HasPrecision(18, 2)`) in `AppDbContext.OnModelCreating` for all monetary/ratio fields — this is **not** inferred by convention and must be added for any new decimal columns.
- Non-nullable C# string properties become `NOT NULL` DB columns by EF convention — this is a database constraint, not a friendly validation message; friendly messages come only from ViewModel `DataAnnotations`.
- Uniqueness is enforced via explicit `HasIndex(...).IsUnique()` calls (e.g. `RecommendationRequest.UserId`, `StockRecommendation.StockId`, `GoldPriceSnapshot.PriceDate`) — there is no naming convention that implies uniqueness automatically; it must be configured per-entity.
- 32 migrations exist as of this writing, from `init_db` through `AddCapitalPurificationRecord`. **Migration hygiene matters on this project**: during this engagement, a migration was reported as applied but its file was missing from a delivered build, while the database and `__EFMigrationsHistory` disagreed with the checked-in `AppDbContextModelSnapshot.cs`. Always verify all four of these agree before considering a schema change complete: the migration `.cs` file, `AppDbContextModelSnapshot.cs`, the `__EFMigrationsHistory` table, and the actual tables in the database.

## 6. Authentication & Authorization

- ASP.NET Identity with a custom `AppUser : IdentityUser`, a `UserRole` enum (`SuperAdmin, Admin, Editor, User`), and a custom `AppUserClaimsPrincipalFactory`.
- Password policy (`Program.cs`): min length 8, requires digit, uppercase, and non-alphanumeric character; `RequireConfirmedAccount = false`.
- Public self-registration (`/account/register`) always creates role `User`; admin/editor accounts are created only by SuperAdmins via `/admin/users` — there is no self-serve path to elevated roles.
- Anti-forgery: `AddAntiforgery(o => o.HeaderName = "RequestVerificationToken")`; all state-changing POST actions use `[ValidateAntiForgeryToken]` + `@Html.AntiForgeryToken()` in forms.
- **One scheme, one cookie, two login pages.** `AddIdentity` registers the only scheme (`Identity.Application`); there is no separate admin scheme — `/admin/login` and `/account/login` both post to the same `SignInManager`, and "admin" is purely a role claim. Because one `LoginPath` must serve both, `ConfigureApplicationCookie` sets `LoginPath = "/account/login"` plus an `OnRedirectToLogin` handler that routes challenges under `/admin/*` to `/admin/login` and everything else to the public login. Previously `LoginPath` was `/admin/login` unconditionally, which sent every public unauthenticated challenge to the admin panel's form.
- **`returnUrl` travels in the query string, not a form field.** `AccountController.Login`'s simple-type `returnUrl` parameter does not bind from posted form data on this project — the login view carries it via `asp-route-returnUrl` on the `<form>` action instead, and the POST re-stashes it in `ViewBag` so it survives a failed attempt.
- `SeedData.cs` creates a default `SuperAdmin` account with a hardcoded password committed to source — confirm with the client whether this was rotated post-launch.

## 7. Notable Feature-Specific Behavior

- **Two distinct "calculation" features share vocabulary but not architecture:** `/calculator` (`CalculatorController`) is stateless — math happens client-side in JS, the controller only generates a downloadable PDF via iText7 and never touches the database. `/calculations` (`CalculationsController` → `ICalculationService`) is the persisted, full-CRUD "saved sessions" feature, fully service-backed.
- **Recommendations module** (`/recommendations`, `Admin/RecommendationRequestsController`) follows the `PortfolioRequest` precedent for its approval workflow (one status-tracked row per user) and reuses the existing Select2 pattern (already used in `Calculator/Index.cshtml`, `Calculations/Edit.cshtml`) for searchable stock selection rather than introducing a new library.

## 8. External Integrations

| Integration | Purpose | Notes |
|---|---|---|
| Cloudflare R2 (via `AWSSDK.S3`) | File storage — article images, portfolio section images, financial review PDFs | Abstracted behind `IStorageService`; falls back to `LocalStorageService` (disk) automatically when `CloudflareR2:AccountId` is unset in config — this is the built-in mechanism for safe local development |
| GoldAPI (goldapi.io) | Gold price for zakat nisab calculation | Cached in `GoldPriceSnapshot`; manual admin overrides are protected from being overwritten by the scheduled/on-demand API refresh |
| iText7 | PDF generation | Uses an embedded Arabic font (`wwwroot/fonts/arabtype.ttf`) for RTL PDF rendering in the calculator |

Storage upload limits by feature: article images (2 MB, default extensions), portfolio section images (20 MB), financial review PDFs (`.pdf` only, 30 MB). Kestrel/IIS request body size is capped at 30 MB project-wide.

## 9. Known Architectural Inconsistencies (technical debt to be aware of)

- **`PortfolioRequestController` bypasses the service layer** and talks to `AppDbContext` directly — the only controller in the codebase that does this.
- **`ArticleService.DeleteAsync` doesn't clean up the associated storage file** (`FeaturedImagePath`) — deleting an article orphans its image in R2/local storage. `PortfolioSectionsController` and `StockFinancialReviewsController` both correctly call `_storage.DeleteAsync` on replace/delete; `ArticleService` does not.
- **Validation coverage is inconsistent across ViewModels.** `CategoryFormViewModel` has no `DataAnnotations` at all; `UserFormViewModel` only validates email format; `StockFormViewModel` is partly annotated (`[Range]` on `TotalCompanyShares`/`TotalCompanyAssets` only — its older fields are still bare). `ArticleFormViewModel`, the auth forms, and the newer recommendation/financial-review forms are fully annotated. The *messages* are no longer a gap even where annotations are missing — see the global Arabic-validation infrastructure in `context/coding-standards.md`.
- **No automated tests, no CI/CD pipeline.** Manual verification (build + local run) is currently the only quality gate.
- **`appsettings.json` is not excluded by `.gitignore` pattern** (only `secrets.json` and `.env` are) — it must be kept out of version control by discipline alone, or moved to `dotnet user-secrets` for local development. This was found to actually contain live production credentials in a delivered build during this engagement; treat this as an active risk, not a hypothetical one.

## 10. Frontend / Styling Conventions

- Bootstrap 5 **RTL build** (`bootstrap.rtl.min.css`) throughout; Tajawal Google Font for Arabic typography.
- `wwwroot/css/site.css` is small and additive — global tweaks (focus rings, floating-label RTL placeholder alignment) plus feature-specific utility classes appended as features are built (e.g. `.recommendation-expired`, `.value-negative`, `.value-stoploss`).
- Select2 (v4.1.0-rc.0, `select2-bootstrap-5-theme`, RTL) is the established pattern for searchable dropdowns bound to server data — usually preloading a full list via a small JSON endpoint and filtering client-side, though `RealPortfolio/Index.cshtml` queries the server per keystroke via `ajax:` + `/api/stocks/search`. **The theme CSS path is `.../dist/select2-bootstrap-5-theme.rtl.min.css`; the `dist/css/...` variant 404s and Select2 silently renders unstyled and borderless.** `Calculator/CapitalPurification.cshtml` deliberately uses no Select2 at all — a dependency-free debounced autocomplete over a plain `form-control`, because a single-select Select2 puts its search box inside the dropdown, which reads as a confusing second field.
- The home page's filter row uses `form-select` on the three multi-select dropdown *buttons* (not `btn btn-outline-secondary`), so all five search controls share one border treatment; `dropdown-toggle` is deliberately omitted there since `form-select` already draws a caret.
- Admin views share `_AdminLayout.cshtml` (sidebar navigation, dark teal theme); public views share `_Layout.cshtml` (top nav, conditional on `_currentUser`).
- No CSS build pipeline (no Sass/PostCSS/bundler config found) — `site.css` is authored and served directly.

## 11. Build & Deployment

- `web.config` configures IIS hosting (ASP.NET Core Module) with a matching 30 MB upload limit to Kestrel's.
- No Dockerfile, no containerization present.
- Deployment/config secrets (connection string, GoldAPI key, R2 credentials) are expected to live in an untracked `appsettings.json` or environment-specific configuration on the server — never in source control.

## 12. Recent Wallet Feature Changes (summary)

This project recently received an Investment Wallet feature and several related fixes. Summary of the implemented changes:

- **Feature added:** Investment wallet with per-user `InvestmentWallet` and `WalletItem` entities, a service implementation, controller, and dedicated Razor views for summary and full wallet management.
- **Model additions:** `WalletItem` now persists the choice of price source for each numeric price field instead of inferring it on edit. New columns:
	- `PurchasePriceSource` (string: e.g. `ClosePrice` | `Custom`)
	- `TargetPriceSource` (string: e.g. `Resistance1` | `Resistance2` | `Custom`)
	- `StopLossSource` (string: e.g. `Support1` | `Support2` | `Custom`)
- **ViewModel additions:** `WalletItemFormViewModel` now includes `PurchasePriceSource`, `TargetPriceSource`, and `StopLossSource` to round-trip the selection to/from the UI.
- **Controller & Server changes:** `Controllers/InvestmentWalletController.cs` was updated to store these source values on Create/Edit and to use them (rather than value-comparison) when populating the edit form.
- **Client-side fixes:** `Views/Recommendations/Wallet.cshtml` JavaScript now:
	- Builds dropdown option labels from the same live recommendation data used by calculations (so labels show the current numeric values immediately on Create);
	- Rebuilds preset options when the selected stock changes;
	- Restores the stored source selection on Edit and shows/hides custom inputs accordingly (no longer inferring by numeric equality).
- **Summary partial:** `Views/Recommendations/_WalletSummary.cshtml` now uses a toggleable cash-edit form and renders safely when rendered as a partial.
- **EF & DB migration:** Added a migration `Migrations/20260708122933_AddWalletItemPriceSources.cs` which adds the three new columns. The migration was generated and applied to the configured database during development.
- **EF configuration fix:** a small syntax issue in `Data/AppDbContext.cs` (the decimal `HasPrecision` loop) was corrected while adding `HasMaxLength(20)` for the new source columns.
- **Build status:** Solution builds successfully (`dotnet build HalalStocks.sln`) after these changes; there are a small number of nullable-value warnings in `Controllers/InvestmentWalletController.cs` that should be addressed at some point.

Files touched (high level): [Models/WalletItem.cs](Models/WalletItem.cs), [ViewModels/WalletItemFormViewModel.cs](ViewModels/WalletItemFormViewModel.cs), [Controllers/InvestmentWalletController.cs](Controllers/InvestmentWalletController.cs), [Views/Recommendations/Wallet.cshtml](Views/Recommendations/Wallet.cshtml), [Views/Recommendations/_WalletSummary.cshtml](Views/Recommendations/_WalletSummary.cshtml), [Data/AppDbContext.cs](Data/AppDbContext.cs), [Migrations/20260708122933_AddWalletItemPriceSources.cs](Migrations/20260708122933_AddWalletItemPriceSources.cs).

If you want the project.md entry expanded into a short onboarding guide (how to exercise the wallet flow locally: set cash, add preset/custom items, edit entries, rollback migration), I can add a reproducible step-by-step section next.

## 13. Private Recommendations + Tabbed Recommendations/Wallet Pages (summary)

`/recommendations` was split into two independently-gated, tab-linked pages, and a per-user "private
recommendations" list was added alongside the existing admin-curated one.

- **Navigation:** both `/recommendations` and `/recommendations/wallet` now independently compute
	`RecommendationsPageState` (Gate/Pending/List) and render the *same* gate/pending screen in place
	(no redirect) when the user isn't approved. When approved, both pages render a shared
	`Views/Recommendations/_NavTabs.cshtml` (Bootstrap `nav-tabs`) linking between the two, with the
	current page marked active. The gate/pending markup itself was extracted into
	`Views/Recommendations/_AccessGate.cshtml` so both pages share one source of truth.
- **Gotcha (cost real debugging time):** `Wallet.cshtml` is rendered by `InvestmentWalletController`,
	not `RecommendationsController`. Razor's `<partial name="...">` tag helper resolves short partial
	names relative to the *acting controller's* view folder (`Views/InvestmentWallet/`, then
	`Views/Shared/`) — **not** relative to the `.cshtml` file's own physical folder. Any partial shared
	between `Wallet.cshtml` and the `RecommendationsController`-rendered pages must be referenced by
	full path, e.g. `<partial name="~/Views/Recommendations/_NavTabs.cshtml" ...>` — this is the same
	reason `_WalletSummary.cshtml` was already referenced by full path before this change. If a future
	edit to `Wallet.cshtml` swaps back to a short partial name and it 500s with
	`InvalidOperationException: The partial view '...' was not found`, this is why.
- **Model additions:** `RecommendationBase` (abstract, unmapped) now holds the field set previously
	duplicated on `StockRecommendation` directly; `StockRecommendation` was refactored to inherit it.
	New `UserStockRecommendation : RecommendationBase` + `UserId` — see §4. New
	`Services/Interfaces/IUserRecommendationService.cs` /
	`Services/Implementations/UserRecommendationService.cs` mirror `RecommendationService` but every
	mutating/single-read method takes/enforces `userId` ownership (defense in depth beyond the
	controller-level checks).
- **Controller changes:** `RecommendationsController` gained `my/create`, `my/{id}/edit`,
	`my/{id}/delete` routes (any approved user, no role restriction — admins keep both their curated
	list and their own private list) plus an `IsApprovedAsync` helper. `InvestmentWalletController` no
	longer redirects unapproved users to `/recommendations` — it renders the gate/pending state at its
	own URL. Its `BuildViewModelAsync` now unions general (`StockRecommendation`) and the current user's
	private (`UserStockRecommendation`) rows for the wallet's stock picker: general wins on overlap
	(shown once, unprefixed), and a stock is only shown once. The picker always lists *every* non-hidden
	general + private recommendation stock — stocks already held are **not** filtered out, since the same
	stock may be bought in several tranches (see the duplicate-stocks bullet below).
	`WalletItemFormViewModel.RecommendationStocks` is typed `List<RecommendationBase>` — the
	runtime type of each entry (`StockRecommendation` vs `UserStockRecommendation`) is what the view
	uses to decide the `"خاص-"` prefix, no separate source-tag field needed.
- **Shared table partial:** `Views/Recommendations/_RecommendationTable.cshtml` (replaced the old
	`_RecommendationList.cshtml`) renders both the general and private recommendation tables from one
	parameterized `RecommendationTableViewModel` (data, `CanManage` admin-vs-owner flag, edit/delete URL
	prefixes, a `SectionKey` used to scope element IDs/filter JS so both sections can render on the same
	page without collisions). No new columns were added to either recommendation table.
- **Wallet's new derived columns:** `WalletItem` gained two new *stored* decimal columns,
	`TargetProfitPercent` and `ExpectedLossPercent` — `(TargetPrice - PurchasePrice) / PurchasePrice ×
	100` and `(PurchasePrice - StopLossPrice) / PurchasePrice × 100`, computed and persisted server-side
	in `InvestmentWalletController.SaveItem` on every create/update (client-side JS shows a live preview
	only, matching the existing pattern for `TotalPurchase`/`ExpectedProfit`/etc. — nothing client-computed
	is trusted as submitted). Not included in the wallet table's totals row (summing percentages across
	differently-sized positions isn't meaningful the way currency sums are — flagged as an assumption,
	confirm with the client if a rollup is actually wanted).
- **Duplicate stocks are allowed (changed 2026-08-08):** a wallet may hold the *same stock more than
	once* — users buy in tranches on different dates/prices, and each purchase is its own `WalletItem`
	row with its own quantity, prices and derived percentages. `WalletItem(WalletId, StockId)` therefore
	carries a **plain, non-unique** index on `WalletId` only, matching the `RealPortfolioItem` precedent
	(same rule, same reasoning, comment in `Data/AppDbContext.cs`). The "هذا السهم مضاف بالفعل" guard was
	removed from `InvestmentWalletController.SaveItem` *and*
	`Controllers/Admin/WalletManagementController.SaveItem` (the logic is duplicated across those two —
	change both or the user page and the admin-on-behalf page disagree). What actually bounds repeated
	buys now is the pre-existing available-cash and `MaxRiskPercent` validation, both of which already
	aggregate across rows.
- **EF & DB migrations:** `Migrations/20260710235921_AddUserRecommendationsAndWalletExtras.cs` — new
	`UserStockRecommendations` table, unique index on `(UserId, StockId)`, unique index on
	`WalletItems(WalletId, StockId)`, and the two new `WalletItem` percent columns.
	`Migrations/20260808201507_AllowDuplicateWalletStocks.cs` then dropped that
	`WalletItems(WalletId, StockId)` unique index in favour of a non-unique `WalletId` index, per the
	bullet above. Both generated and applied to the local dev database; no existing migration was
	modified. Note `dotnet ef database update --no-build` silently reports "already up to date" when the
	compiled assembly predates the migration you just added — rebuild first.
- **Build status:** Solution builds clean (`dotnet build HalalStocks.sln`, 0 warnings, 0 errors) after
	these changes.

Files touched (high level): [Models/RecommendationBase.cs](Models/RecommendationBase.cs),
[Models/StockRecommendation.cs](Models/StockRecommendation.cs),
[Models/UserStockRecommendation.cs](Models/UserStockRecommendation.cs),
[Models/WalletItem.cs](Models/WalletItem.cs),
[Services/Interfaces/IUserRecommendationService.cs](Services/Interfaces/IUserRecommendationService.cs),
[Services/Implementations/UserRecommendationService.cs](Services/Implementations/UserRecommendationService.cs),
[Controllers/RecommendationsController.cs](Controllers/RecommendationsController.cs),
[Controllers/InvestmentWalletController.cs](Controllers/InvestmentWalletController.cs),
[ViewModels/RecommendationTableViewModel.cs](ViewModels/RecommendationTableViewModel.cs),
[ViewModels/RecommendationsIndexViewModel.cs](ViewModels/RecommendationsIndexViewModel.cs),
[ViewModels/WalletItemFormViewModel.cs](ViewModels/WalletItemFormViewModel.cs),
[Views/Recommendations/Index.cshtml](Views/Recommendations/Index.cshtml),
[Views/Recommendations/Wallet.cshtml](Views/Recommendations/Wallet.cshtml),
[Views/Recommendations/_RecommendationTable.cshtml](Views/Recommendations/_RecommendationTable.cshtml),
[Views/Recommendations/_AccessGate.cshtml](Views/Recommendations/_AccessGate.cshtml),
[Views/Recommendations/_NavTabs.cshtml](Views/Recommendations/_NavTabs.cshtml),
[Views/Recommendations/MyCreate.cshtml](Views/Recommendations/MyCreate.cshtml),
[Views/Recommendations/MyEdit.cshtml](Views/Recommendations/MyEdit.cshtml),
[Data/AppDbContext.cs](Data/AppDbContext.cs),
[Migrations/20260710235921_AddUserRecommendationsAndWalletExtras.cs](Migrations/20260710235921_AddUserRecommendationsAndWalletExtras.cs).

## 14. Wallet Management Assistance — three-tab admin-on-behalf mode (summary)

A user can ask the team to assign a specialist to help manage their wallets; an admin approves the
request and then works inside a **confined mode** scoped to that one user.

- **Request entity:** `WalletManagementRequest` (`Pending/Approved/Rejected`, unique index on `UserId`, so
	one row per user, upserted on re-request) + `IWalletManagementRequestService`, mirroring the older
	`RecommendationRequest` pattern method-for-method. Admin queue lives at
	`/admin/wallet-management-requests`, deliberately its own page rather than a third tab on
	`/admin/recommendation-requests` (one screen per request type, as
	`RecommendationRequestsController`/`PortfolioRequestsController` already do).
- **One request covers all three tabs** (`التوصيات`, `المستشار الآلي`, `المحفظة الفعلية`). Because of
	that, the user-facing status banner (teaser → `قيد المراجعة` → approved) is a single shared partial,
	[Views/Recommendations/_ManagementRequestBanner.cshtml](Views/Recommendations/_ManagementRequestBanner.cshtml),
	rendered **beneath the page title and above the nav tabs** on all three user pages. Every controller
	rendering one of those pages must set `ViewBag.ManagementStatus` — the partial reads it from ViewBag
	rather than taking a model, because a nullable-enum partial model interacts badly with the
	`<partial model="...">` tag helper when the value is null. The partial carries its **own inline
	`<script>`** (inside `DOMContentLoaded`) since partials cannot contribute to `@section Scripts`.
- **Consent checkbox ordering (cost real debugging time):** the modal posts `consent` as a checkbox plus
	a hidden `false` fallback. The **checkbox must come first, hidden second** — the model binder takes
	`ValueProviderResult.FirstValue`, i.e. the first value in DOM order, so reversing them makes consent
	bind `false` no matter what the user ticks. This matches what the built-in `asp-for` checkbox tag
	helper emits.
- **Admin scoped mode:** `Controllers/Admin/WalletManagementController.cs`, routed
	`/admin/wallet-management/{userId}`, renders under
	[Views/Shared/_WalletManagementLayout.cshtml](Views/Shared/_WalletManagementLayout.cshtml) — a
	deliberately minimal layout with **no public nav and no admin sidebar**; the absent chrome is what
	enforces "nothing else reachable". Every action re-checks `IsApprovedForUserAsync` via
	`ResolveTargetAsync`, so revoking approval takes effect immediately, mid-session.
	Tabs: `التوصيات` is **read-only context**; `المستشار الآلي` and `المحفظة الفعلية` are **fully
	editable** on the target user's behalf. All service calls pass the route's `userId`, never the
	signed-in admin's.
- **`Views/RealPortfolio/Index.cshtml` is rendered by two controllers** (`RealPortfolioController` and
	`Admin/WalletManagementController`) rather than being copied, because of its ~300 lines of Select2 /
	live-quote / calculation JS. It is parameterized by `RealPortfolioIndexViewModel.BasePath` +
	`IsManagementMode`. **When editing it: every URL must go through `Model.BasePath`, and every
	`<partial>` reference must use a full `~/Views/...` path** — short names resolve against the *acting
	controller's* view folder, so `_PortfolioTable` would 500 under the admin route (same gotcha as §13).
	The `المستشار الآلي` admin screen predates this and is still a **copy**
	(`Views/Admin/WalletManagement/Wallet.cshtml`) — worth folding into the same pattern if it is
	touched substantially.
- **Shared view-model builder:** `IRealPortfolioViewModelBuilder` /
	[Services/Implementations/RealPortfolioViewModelBuilder.cs](Services/Implementations/RealPortfolioViewModelBuilder.cs)
	holds the portfolio page's row mapping, live-price fetch and `SaveMarketPricesAsync` write-back, so
	that side-effecting path has exactly one implementation across both controllers. The `SaveItem`
	*validation* rules are intentionally restated per controller (each owns its own `[Bind(Prefix=...)]`
	and `ModelState` keys), matching how `WalletManagementController.SaveItem` mirrors
	`InvestmentWalletController.SaveItem`.
- **No schema change** was needed to extend the feature to the real wallet — the existing request row
	already means "this user may be helped", which now simply covers a third surface.

## 15. Capital Purification Calculator, Kasheif tools & public التوصيات (summary)

A batch of changes centred on a new calculator, plus the home-page and auth work it surfaced.

**New feature — capital purification (`/capital-purification`).**
- Answers a different question from `/calculator`: how much of the user's *capital* (not realised
	income) sits behind riba-bearing activity. Three chained sections —
	`ownership% = ownedShares ÷ Stock.TotalCompanyShares` → `capitalShare = Stock.TotalCompanyAssets ×
	ownership%` → `purification = capitalShare × Stock.NonShariahInvestmentRatio`. The riba input is the
	**existing** `NonShariahInvestmentRatio` screening ratio; only the two company-level figures on
	`Stock` are new (see §4).
- `CapitalPurificationController` owns the feature (its own controller, like `ZakatController`, rather
	than growing `CalculatorController`) and follows the house auth style: no class-level `[Authorize]`,
	manual `IsAuth`/`UserId` helpers, explicit `~/Views/...` paths.
- **The server recomputes everything.** The save/update payload is only `{ stockId, ownedShares }`;
	the controller reloads the stock and derives every figure, returning `BadRequest(new { error })`
	with an Arabic message when data is missing or the share count exceeds the company's total. Client
	totals are never trusted — same principle as `CalculationsController`'s edit POST.
- All four mutating endpoints are `[ValidateAntiForgeryToken]` and called by `fetch` with the
	`RequestVerificationToken` header. The two record tables (لم يتم سدادها / تم سدادها) re-render from
	`/capital-purification/records` after every change, so a row visibly moves between them without a
	page reload.
- The picker needed the two new figures, so it has its own endpoint
	(`/api/stocks/capital-search`) rather than reusing `/api/stocks/search` — that one is gated behind
	the recommendations access check and omits the fields. It still reuses
	`StockService.SearchActiveStocksAsync`.

**Home page.**
- New **أدوات كاشف** card grid beneath the search results linking all five user tools (three
	calculators, فتح محفظة استثمارية, التوصيات), using Bootstrap Icons and a `.tool-card` hover lift in
	`site.css`.
- The two legal disclaimers are now one collapsed-by-default **تنبيهات هامة** accordion whose
	open/closed state is remembered in `localStorage` (`kasheif.homeAlerts.open`), applied as classes on
	load so a remembered-open panel doesn't animate. Every storage access is `try`-wrapped — it throws
	outright in some privacy modes. The disclaimer wording is legal text and was preserved
	character-for-character.
- The three filter dropdowns were restyled from outlined buttons to `form-select` (§10).

**التوصيات became publicly reachable.**
- `/recommendations`, `/recommendations/wallet` and `/recommendations/portfolio` keep their class-level
	`[Authorize]` but add `[AllowAnonymous]` on `Index` alone, which then returns a guest gate instead of
	`Challenge()`. One attribute per controller — no re-annotating a dozen sibling actions, so no risk of
	leaving a write action open.
- The header nav no longer hides التوصيات from logged-out visitors, and the three calculators are
	grouped under an **أدوات التطهير** hover dropdown.

**Two auth bugs fixed along the way** (both pre-existing, both in the path of the above): the global
`LoginPath` sending public challenges to the admin login, and `returnUrl` never binding from the login
form — see §6.

**Arabic validation messages** are now handled globally in three places (jQuery Validate defaults,
`ModelBindingMessageProvider`, and an `IValidationMetadataProvider` for implicit `[Required]`); the
per-field `data-val-required` workaround previously documented is obsolete. Details in
`context/coding-standards.md`.
