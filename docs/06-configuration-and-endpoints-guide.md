# Eduflex — Configuration & Endpoints Guide

> Practical companion to [01-system-architecture.md](01-system-architecture.md) §5. That doc
> explains what each config key *is*; this one is a checklist for **what to change and where**
> when the system moves — new backend URL, new frontend host, new database, rotated secrets — plus
> a from-scratch explanation of Docker/`docker-compose`/YAML as used in this repo.
>
> Companion documents: [01](01-system-architecture.md) · [02](02-database-design.md) ·
> [03](03-backend-design.md) · [04](04-frontend-design.md) · [05](05-findings-and-recommendations.md) ·
> [07-frontend-onboarding-walkthrough.md](07-frontend-onboarding-walkthrough.md)

## 1. "I changed the backend's URL" (new deploy, new domain, new port)

The backend's own address is never hardcoded in the backend itself — every place that needs to
know it is on the **frontend** side, plus one CORS entry pointed the other way.

| File | Key | What to set it to |
|---|---|---|
| `eduflex-Angular/src/app/environments/environment.ts` | `apiClientUrl`, `publicApiUrl` | The backend's **local dev** URL, e.g. `https://localhost:5083` (must match whichever `launchSettings.json` profile you actually run — see §4) |
| `eduflex-Angular/src/app/environments/environment.prod.ts` | `apiClientUrl`, `publicApiUrl` | The backend's **deployed** URL, e.g. `https://eduflex-api.<region>.azurecontainerapps.io` |
| `Eduflex/appsettings.json` (or environment-specific override) | `Cors:AllowedOrigins` | Must include the frontend's origin(s) that will call this backend — see §2 |

Both `apiClientUrl` and `publicApiUrl` are currently set to the **same value** in both
environment files — there's no separate host for the `app`/`public`/`auth` Swagger groups today,
just one backend origin fanned out to two DI tokens
(`APPLICATION_API_BASE_URL`/`AUTH_API_BASE_URL`, see
[04-frontend-design.md](04-frontend-design.md) §2). Change both together unless you deliberately
split the API onto separate hosts.

⚠ Remember: `environment.prod.ts` only takes effect because of the `fileReplacements` swap in
`angular.json`'s `production` build configuration (§5 below in
[01-system-architecture.md](01-system-architecture.md)). If you ever add a *third* environment
(e.g. `staging`), you must add both a new `environment.staging.ts` file **and** a matching
`configurations.staging` block with its own `fileReplacements` entry in `angular.json` — simply
creating the file does nothing on its own.

## 2. "I changed the frontend's URL" (new deploy, new domain, new port)

| File | Key | What to set it to |
|---|---|---|
| `Eduflex/appsettings.json` | `Cors:AllowedOrigins` (array) | Add the frontend's exact origin (scheme + host + port, no trailing slash), e.g. `"https://lively-dune-0ce1c8f00.7.azurestaticapps.net"` |
| `Eduflex/appsettings.json` | `WebURLSettings:FrontendBaseUrl` | Used server-side to build links back to the SPA (e.g. inside welcome emails) — update to the same new frontend origin |

CORS in ASP.NET Core matches **exact origins**, not prefixes/wildcards (the policy here is
`AllowAnyHeader().AllowAnyMethod().AllowCredentials()` restricted to the configured origin list —
see [03-backend-design.md](03-backend-design.md) §2). A trailing slash or `http` vs `https`
mismatch will silently fail CORS with no useful client-side error beyond "CORS policy blocked".

## 3. "I changed the database" (new Atlas cluster, new local Mongo, new DB name)

| File | Key | Notes |
|---|---|---|
| `Eduflex/appsettings.json` | `ConnectionStrings:MongoDBConnection` | Full connection string, e.g. `mongodb+srv://user:pass@cluster.mongodb.net` |
| `Eduflex/appsettings.json` | `MongoDBSettings:DatabaseName` | Just the DB name, e.g. `EduflexDB` — appended by the driver via `client.GetDatabase(name)`, not part of the connection string itself |
| `DBMigration/appsettings.json` or gitignored `DBMigration/appsettings.local.json` | `MongoDBEnvironments:{Dev,Test,Pro}:{ConnectionString,DatabaseName}` | The migration console tool has its **own**, separate per-environment settings — updating the API's connection string does *not* update what `DBMigration` points at, and vice versa. Deliberately decoupled (see [02-database-design.md](02-database-design.md) §3.1) so a developer can point the migration tool at `Test` while the API runs against `Dev`. |

After pointing at a new/empty database, **run the migrations before starting the API** — nothing
creates `Roles`/`Permissions`/`Modules`/indexes automatically, and login will fail (no roles to
resolve) or list screens will error (missing indexes/collections) until migrations `001`–`013`
have run. See [02-database-design.md](02-database-design.md) §3.5 for the exact steps.

If moving to Azure Container Apps specifically, the connection string should go in as a **secret**
(`az containerapp secret set --name mongo-conn ...`), referenced from the app's env vars, not
baked into the image via `appsettings.json` — see the security note in
[05-findings-and-recommendations.md](05-findings-and-recommendations.md) item 1. Also note:
`az containerapp secret set` does **not** auto-restart the app (unlike
`az containerapp update --set-env-vars`, which does) — you need an explicit
`az containerapp revision restart` for a changed secret to actually take effect.

## 4. "I changed a port" (local dev only)

| Port | Where it's defined | Where it must also be updated |
|---|---|---|
| Backend HTTPS (`5083`) / HTTP (`5246`) | `Eduflex/Properties/launchSettings.json` | `environment.ts`'s `apiClientUrl`/`publicApiUrl`; `Cors:AllowedOrigins` if the frontend calls it cross-origin during dev (usually not needed for the backend's *own* port) |
| Frontend dev server (`9000`) | `angular.json` → `serve` target | `Cors:AllowedOrigins` in `Eduflex/appsettings.json` — must list `http://localhost:9000`, **not** Angular's usual default of `4200`, which this project's `serve` target overrides |
| Docker Compose backend (`5000`) | `Eduflex/Eduflex/docker-compose.yml` | Nothing else automatically — this is a separate port mapping only relevant when running via `docker compose up`, unrelated to the `launchSettings.json` ports used for plain `dotnet run` |

## 5. "I rotated a secret" (JWT, Azure Blob, Azure Email, reCAPTCHA)

All five secret-bearing keys live in `Eduflex/appsettings.json` today (flagged as a security issue
in [05-findings-and-recommendations.md](05-findings-and-recommendations.md) item 1 — treat this
section as "how it works now," not an endorsement):

| Key | Used by | Effect if wrong/missing |
|---|---|---|
| `JWT:Secret` | Signs and validates every JWT (`Program.cs` + `AuthController.GenerateJwtToken`) | If changed, **every previously issued token is instantly invalid** (all logged-in users get 401'd) — this is expected and fine for a rotation, just don't be surprised by a wave of "please log in again" |
| `JWT:Salt` | Input to the password hash (`AuthController.HashPassword`/`UserService`) | If changed, **every existing user's stored password hash stops matching** — this is not a safe "rotate anytime" secret like `JWT:Secret`; changing it requires re-hashing every stored password, which nothing in this codebase currently does automatically |
| `AzureBlobStorage:ConnectionString` | File uploads (`FilesController` → `AzureBlobDocStorageService`) | Upload endpoint fails; existing file *references* stored in Mongo still resolve fine as long as the storage account/container itself didn't also change |
| `AzureEmailSettings:ConnectionString` | Transactional email (new-user welcome email) | New-user creation still succeeds; the welcome email silently fails to send — the new user won't have their initial password unless someone checks logs or resends manually |
| `Recaptcha:SecretKey` | Server-side verification of the reCAPTCHA token the SPA collects | Public enquiry submissions fail verification and get rejected |

## 6. Docker & YAML, explained from scratch

### 6.1 What YAML actually is

YAML is a whitespace-indentation-based data format (like Python, indentation is meaningful, not
decorative). Three shapes you'll see repeatedly in this repo:

```yaml
# a key: value pair
name: eduflex-api

# a nested object (indent = "this belongs to the key above")
environment:
  ASPNETCORE_URLS: http://+:5000

# a list (dash = "one item")
ports:
  - "5000:5000"
  - "5001:5001"
```
Combine them and you get exactly the shape of `docker-compose.yml` and the GitHub Actions
workflow files below. There is no closing bracket/brace to match, unlike JSON — indentation is
the only structure, so a misaligned space is a real (and common) source of "why won't this file
parse" bugs.

### 6.2 `Eduflex/Dockerfile`, line by line

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build     # stage 1: full SDK image, has the compiler
WORKDIR /src
COPY . .                                            # copy the whole repo into the image
RUN dotnet restore Eduflex/Eduflex/Eduflex.sln       # download NuGet packages for every project in the solution
ENV BuildingInsideDocker=true                        # this is what skips the NSwag/Swagger-export MSBuild targets (§6 of 01-system-architecture.md)
WORKDIR /src/Eduflex/Eduflex/Eduflex
RUN dotnet publish -c Release -o /app/publish -p:BuildingInsideDocker=true

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime  # stage 2: much smaller image, runtime only, no compiler
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:80                       # ASP.NET Core listens on port 80 inside the container
COPY --from=build /app/publish .                      # copy only the *published output* from stage 1, not the SDK or source
ENTRYPOINT ["dotnet", "Eduflex.dll"]
EXPOSE 80
```
This is a **multi-stage build** — the pattern's whole point is that the final image (`runtime`
stage) never contains the SDK, source code, or NuGet cache, only the compiled output. That keeps
the deployed image small and avoids shipping source/build tools to production.

### 6.3 `Eduflex/Eduflex/docker-compose.yml`, line by line

```yaml
services:
  eduflex-api:
    build:
      context: .                                   # build context = repo root (so COPY . . in the Dockerfile grabs everything)
      dockerfile: Eduflex/Eduflex/Eduflex/Dockerfile # path to the Dockerfile, relative to the context above
    ports:
      - "5000:5000"                                 # HOST:CONTAINER — reach it at localhost:5000
    environment:
      - ConnectionStrings__MongoDb=mongodb://mongo:27017/EduflexDB   # ⚠ see the mismatch note below
    depends_on:
      - mongo                                        # start "mongo" first (does NOT wait for Mongo to be *ready*, just *started*)

  eduflex-angular:
    build:
      context: ./eduflex-Angular
      dockerfile: Dockerfile                         # ⚠ this file does not currently exist — see §7
    ports:
      - "4200:4200"
    depends_on:
      - eduflex-api

  mongo:
    image: mongo:7                                    # pull a prebuilt image instead of building one
    volumes:
      - mongo_data:/data/db                            # named volume — survives `docker compose down` (but not `down -v`)

volumes:
  mongo_data:                                          # declares the named volume referenced above
```

The double-underscore syntax (`ConnectionStrings__MongoDb`) is .NET's convention for expressing a
nested config path (`ConnectionStrings:MongoDb`) as a flat environment variable, since env vars
can't contain `:` on every OS. **This is exactly where the bug documented in
[05-findings-and-recommendations.md](05-findings-and-recommendations.md) item 11 lives**: the app
actually reads `ConnectionStrings:MongoDBConnection` (see §3 above), but this file sets
`ConnectionStrings__MongoDb` — different key entirely, so the override silently does nothing and
the app falls back to whatever `appsettings.json` has baked in. If you use `docker compose up`
for local dev, fix this line to `ConnectionStrings__MongoDBConnection` first.

### 6.4 GitHub Actions workflow YAML (`.github/workflows/*.yml`)

Shape is the same YAML, different vocabulary — a workflow is `on:` (trigger) + `jobs:` (each a
list of `steps:`):

```yaml
on:
  push:
    branches: [dev]
    paths: ['Eduflex/**']        # only run when files under Eduflex/ changed

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4                 # step 1: clone the repo into the runner
      - uses: azure/login@v2                       # step 2: authenticate using the AZURE_CREDENTIALS secret
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}
      - run: |                                      # step 3: shell commands, same as typing them yourself
          az acr build --registry eduflexacr --image eduflex-api:${{ github.sha }} .
          az containerapp update --name eduflex-api --image eduflexacr.azurecr.io/eduflex-api:${{ github.sha }}
```
(Illustrative — see the actual files for exact step names/order.) `${{ secrets.X }}` pulls from
the repo's configured GitHub Actions secrets (Settings → Secrets and variables → Actions), never
from a file in the repo — this is the correct pattern, in contrast to `appsettings.json`'s
plaintext secrets (§5). `paths: ['Eduflex/**']` / `paths: ['eduflex-Angular/**']` on the two
workflows is why a change to only the frontend doesn't trigger a backend rebuild and vice versa.

## 7. Open gap: no frontend Dockerfile exists

`docker-compose.yml`'s `eduflex-angular` service (§6.3) points at
`eduflex-Angular/Dockerfile` — **that file does not currently exist in the repo** (verified: only
`Eduflex/Dockerfile` is present anywhere in the solution). Running `docker compose up` today would
fail on the `eduflex-angular` build step. This is consistent with the fact that the frontend isn't
actually containerized in the real deployment path either — `frontend-deploy.yml` builds Angular
with plain `npm ci`/`ng build` and deploys the static output straight to Azure Static Web Apps
(§6 of [01-system-architecture.md](01-system-architecture.md)), no Docker involved. Treat the
`eduflex-angular` compose service as aspirational/incomplete rather than a working local option
until a Dockerfile is added. A minimal one, if ever needed, would be a two-stage `node` build →
static file server (e.g. `nginx` or `serve`) — analogous in spirit to the backend's
build/runtime split in §6.2, but this is not currently part of the repo.
