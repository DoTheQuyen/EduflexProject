# Eduflex — Findings & Recommendations Backlog

> A consolidated, prioritized list of concrete issues surfaced while writing
> [01](01-system-architecture.md)–[04](04-frontend-design.md), [06](06-configuration-and-endpoints-guide.md),
> and [07](07-frontend-onboarding-walkthrough.md). Each item cites where it lives and what a fix
> looks like. Ordered by rough severity within each category, not by effort.

## Security

1. **Secrets committed in plaintext** — `Eduflex/appsettings.json` holds live-looking values for
   `JWT:Secret`/`JWT:Salt`, the Azure Blob Storage connection string, the Azure Communication
   Services (email) key, and the reCAPTCHA secret key, all checked into a public GitHub repo
   (`DoTheQuyen/EduflexProject`). Additionally, an `AZURE_CREDENTIALS` service principal JSON and
   a Static Web Apps deployment token were pasted into chat during a past session.
   **Fix**: rotate all of the above immediately; move to `dotnet user-secrets` for local dev
   (a `UserSecretsId` is already configured in `Eduflex.csproj` but unused) and Azure Container
   Apps secrets / Key Vault for deployed environments; add `appsettings.json`'s secret keys to
   `.gitignore`-tracked override pattern (commit only an `appsettings.json` with empty/placeholder
   values, real values in `appsettings.Development.json`-style gitignored overlays or environment
   variables).
2. **Password hashing is weak and inconsistent** — production login (`AuthController`) and
   password-change (`UserService`) both use `SHA256(password + static-app-wide-salt)`, not a
   proper password KDF. `BCrypt.Net-Next` is referenced in every backend project but only actually
   invoked by `DBMigration/Services/Services/DatabaseService.cs` when seeding sample users — those
   seeded users get BCrypt hashes that **can never verify** against the real SHA-256 login path.
   **Fix**: migrate to BCrypt (or Argon2id) with a per-user random salt; support a "verify against
   old scheme, re-hash to new scheme on next successful login" dual-path during the transition;
   fix or remove the DBMigration seed path so it hashes with whatever scheme is actually live.
3. **Swagger UI is exposed in every environment**, including Production —
   `Program.cs` calls `app.UseSwagger()`/`app.UseSwaggerUI()` unconditionally before the
   `Environment.IsDevelopment()` check (which only gates a redundant second call).
   **Fix**: move the first `UseSwagger()/UseSwaggerUI()` call inside the `IsDevelopment()` block
   (or behind an explicit config flag) if this ever runs somewhere Swagger shouldn't be public.
4. **No JWT issuer/audience validation, no refresh token, no logout revocation** —
   `ValidateIssuer`/`ValidateAudience` are both `false`; `Logout` is a stateless no-op; a leaked
   token remains valid for its full 24-hour lifetime with no revocation path.
   **Fix**: acceptable for a single-client learning app; if this becomes multi-client or handles
   real user data, add issuer/audience validation, shorter access-token lifetime + refresh tokens,
   and a revocation list (even a simple "token issued before user's `TokensValidFrom` timestamp is
   rejected" pattern).

## Correctness bugs

5. **`content.services.ts`'s NSwag base-URL token is never provided** — `app.config.ts` only binds
   `APPLICATION_API_BASE_URL` from `api.services.ts` and `AUTH_API_BASE_URL` from
   `public.services.ts`; `content.services.ts` generates its own distinct `InjectionToken` object
   (same string label, different identity), which is never provided, so its `Client` falls back to
   `baseUrl = ""`. Live effect: `EnquiryModalComponent`, `CoursePromotionCarouselComponent`, and
   `FeedbackCarouselComponent` issue relative-path requests, which resolve against the SPA's own
   origin, not the API's — broken in both documented environments since the API is cross-origin
   from the SPA in both. See [04-frontend-design.md](04-frontend-design.md) §2.4.
   **Fix**: import the token from `content.services.ts` specifically and add a third provider
   entry in `app.config.ts`.
6. **Two parallel, inconsistent auth-state services on the frontend** — `AuthHelperService`
   (`localStorage` keys `authToken`/`userData`) is the one actually wired into guards, the
   interceptor, and most components. `AuthService` (`BehaviorSubject`-based, keys
   `access_token`/`user_info`, the only one that actually decodes+validates JWT `exp`) is used only
   by `register.component.ts`/`navbar.component.ts`/`models/user.ts` and is never populated by the
   real login flow — its `isLoggedIn` effectively always reads a missing key.
   **Fix**: pick `AuthHelperService` as the single source of truth, port `AuthService`'s JWT-expiry
   check into it (currently `AuthHelperService.isTokenValid()` is a stub that always returns
   `true`, despite `jwt-decode` being a dependency), and delete or clearly mark `AuthService` as
   legacy.
7. **No global 401 handling** — `auth.interceptor.ts` attaches the bearer token but does nothing on
   a 401 response; an expired/invalid token surfaces only as a per-component error, with no
   consistent auto-logout/redirect-to-login.
   **Fix**: add an `error` handler in the interceptor (or a second interceptor) that clears stored
   auth state and navigates to `/login` on 401.
8. **SSR root-component mismatch** — `main.ts` (client) bootstraps the real `app.component.ts`
   (navbar + footer + router-outlet). `main.server.ts` bootstraps a different, leftover
   `ng new`-scaffold component (`src/app/app.ts`/`app.html`, bare router-outlet only). The
   server-rendered shell and the client-hydrated shell are different components.
   **Fix**: point `main.server.ts`/`app.config.server.ts` at the real `AppComponent`, then delete
   the orphaned scaffold (`app.ts`/`app.html`/`app.css`) and the also-orphaned
   `app-routing.module.ts` (an old `NgModule`-based routes file superseded by `app.routes.ts`,
   referenced nowhere).
9. **DBMigration retry-after-failure hazard** — `MigrationService.ExecuteMigrationAsync` inserts a
   `MigrationRecord` into `_migrations` on both success *and* failure. The collection has a unique
   index on `migrationId`, so retrying a fixed migration with the same `MigrationId` will throw a
   duplicate-key error on the second attempt rather than being treated as "still pending."
   **Fix**: on retry, either delete the prior failed record first, or track attempts as a
   sub-array/history rather than one document per `migrationId`, or `ReplaceOne`/upsert instead of
   `InsertOne`.
10. **Migration ordering relies on string-sort happening to equal numeric-sort** —
    `MigrationRegistrationService.GetMigrationTypes()` and `MigrationService` both order migrations
    with `OrderBy(t => t.Name)` (lexicographic). This only works because every migration author has
    zero-padded the numeric prefix to 3 digits; it silently breaks past `_999_...` or if anyone
    forgets to pad (e.g. `_2_Foo` sorts after `_10_Bar`).
    **Fix**: parse and sort on the numeric prefix explicitly rather than relying on string
    ordering, or enforce padding via a build-time check.
11. **`docker-compose.yml`'s Mongo connection override doesn't match the config key the app
    reads** — the compose file sets `ConnectionStrings__MongoDb=mongodb://mongo:27017/EduflexDB`,
    but `Program.cs` reads `ConnectionStrings:MongoDBConnection` (note `MongoDb` vs `MongoDBConnection`).
    The env-var override as written would not actually take effect via compose.
    **Fix**: correct the env var name to `ConnectionStrings__MongoDBConnection`.
12. **`docker-compose.yml`'s `eduflex-angular` service has no Dockerfile to build** — it declares
    `context: ./eduflex-Angular`, `dockerfile: Dockerfile`, but no `Dockerfile` exists anywhere
    under `eduflex-Angular/` (confirmed by direct listing). `docker compose up` fails on this
    service today. Consistent with the frontend not actually being containerized in the real
    deploy path either (`frontend-deploy.yml` builds via plain `npm`/`ng build` straight to Azure
    Static Web Apps — see [06-configuration-and-endpoints-guide.md](06-configuration-and-endpoints-guide.md) §7).
    **Fix**: either add a minimal two-stage `node` build → static-file-server Dockerfile if a
    local containerized frontend is wanted, or remove the `eduflex-angular` service from
    `docker-compose.yml` to stop it looking like a working option.
13. **`nswag.auth.json` outputs to a file literally named `public.services.ts`** — a naming
    mismatch (the "auth" config produces "public"-named output) that's easy to trip over when
    navigating the codebase looking for the auth client.
    **Fix**: rename for clarity (`auth.services.ts`) — low priority, purely a name-confusion risk,
    not a functional bug, since NSwag output paths are configured explicitly per file today.

## Dead code / cleanup

14. **`nswag.json`** targets `test.services.ts`, which doesn't exist and isn't wired into any
    MSBuild target — safe to delete or clearly mark as a scratch/manual-only config.
15. **Three hand-written HTTP services** (`enquiry.service.ts`, `course-promotion.service.ts`,
    `feedback.service.ts`) and their supporting model files (`models/enquiry.ts`,
    `models/course-promotion.ts`, `models/feedback.ts`) are confirmed unused by any live component
    — intentionally retained as a before/after learning reference per prior project decisions.
    Fine to keep for now; if/when this moves past the learning phase, delete together.
16. **`DBMigration.csproj`** has `<Compile Remove="Models\**" />` yet `DBMigration/Models/*.cs`
    files exist and are referenced elsewhere in the code (`MigrationRecord`, `MongoDBSettings`) —
    worth confirming at a clean build whether this actually compiles as expected, and there's a
    duplicate top-level `DBMigration/MigrationRecord.cs` byte-identical to
    `DBMigration/Models/MigrationRecord.cs` that should be deleted.
17. **Commented-out `MongoDbContext`/`IMongoDbContext`** in `ShareService/DataAccess` (an
    abandoned EF-Core-based experiment, explicitly marked "keep for future practice") — along with
    the unused `MongoDB.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.SqlServer` package
    references across multiple `.csproj` files. Harmless as-is; remove the package references if
    the experiment is truly abandoned, to shrink the dependency surface.
18. **`RegisterDto`** (`Eduflex/DTOs/Auth/RegisterDto.cs`) has no backing controller action —
    self-registration doesn't exist (by design — accounts are admin-provisioned). Either wire it up
    if self-registration is ever wanted, or delete the unused DTO.

## Architectural recommendations (not bugs, but worth planning for)

19. **No shared base entity / audit-field convention** — every model in `ShareService/Models`
    independently declares `Id`/`CreatedAt`/`UpdatedAt` with no consistent presence (see
    [02-database-design.md](02-database-design.md) §2). Introduce a `BaseDocument` with
    `Id`/`CreatedAt`/`UpdatedAt` that every model inherits, to stop the drift.
20. **No collection-name constants class** — collection names are repeated string literals across
    `ShareService/DataAccess/Service/*.cs` and `DBMigration/**`. Add a `CollectionNames` static
    class analogous to the existing `PermissionKeys.cs` pattern.
21. **No generic repository abstraction** — each aggregate hand-rolls the same
    `IMongoCollection<T>` wrapper boilerplate. A thin `MongoRepository<T>` base class (constructor
    takes `IMongoDatabase` + collection name) would remove repeated CRUD boilerplate while keeping
    the current one-class-per-aggregate organization.
22. **Mixed target frameworks across the solution** (Eduflex net9.0; ShareService/DBMigration
    net8.0; ShareService.Tests net9.0) — not broken, but confirm this is deliberate before adding
    net9-only APIs to the shared library.
23. **Thin test coverage** — `ShareService.Tests` covers only `AuthService`, `UserService`, and
    `ApplicationService` out of the many services in `ShareService/Services`. Growing this suite
    would be the highest-leverage next step before any further backend refactor, since the
    auth/authorization layers (the highest-risk area per this document) currently have the least
    safety net.
