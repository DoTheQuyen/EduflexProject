# Eduflex — Frontend Design

> Companion documents: [01-system-architecture.md](01-system-architecture.md) ·
> [02-database-design.md](02-database-design.md) · [03-backend-design.md](03-backend-design.md) ·
> [06-configuration-and-endpoints-guide.md](06-configuration-and-endpoints-guide.md) ·
> [07-frontend-onboarding-walkthrough.md](07-frontend-onboarding-walkthrough.md) (narrative
> walkthrough of the bootstrap sequence this doc describes as reference)

Angular 20.3, standalone components (no `NgModule`s), esbuild-based `@angular/build:application`
builder, SSR-capable via `@angular/ssr` with `outputMode: "static"` (prerendered/SSG output).

## 1. App bootstrap & providers — `src/app/app.config.ts`

```ts
export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor]), withFetch()),
    provideAnimationsAsync(),
    { provide: APPLICATION_API_BASE_URL, useValue: environment.apiClientUrl },
    { provide: AUTH_API_BASE_URL, useValue: environment.publicApiUrl }
  ]
};
```

- `provideRouter(routes)` — standalone router bootstrap, `routes` from `app.routes.ts` (§3).
- `provideHttpClient(withInterceptors([authInterceptor]), withFetch())` — the functional
  interceptor chain (one interceptor, §4) plus the `fetch`-based HTTP backend instead of
  `XhrBackend`.
- `provideAnimationsAsync()` — lazy-loaded Angular animations.
- Two DI tokens bind environment URLs to the NSwag-generated clients' expected injection tokens —
  see the base-URL wiring gap in §2.4, which is the most important thing to understand before
  touching this file.

## 2. NSwag — generating and consuming typed API clients

### 2.1 The three-client split, and why it exists

The backend (`Program.cs`) publishes **three separate Swagger documents** — `auth`, `public`,
`app` — because each controller/action is tagged with a Swagger group name
(`[ApiExplorerSettings(GroupName = "...")]`). NSwag then generates **three independent Angular
client files**, one per group, driven by three separate config files that live in the backend
project (`Eduflex/`):

| NSwag config | Swagger source (static snapshot) | Generated file | Base-URL token | Used by |
|---|---|---|---|---|
| `nswag.auth.json` | `swagger-auth.json` | `src/app/services/public.services.ts` *(name doesn't match "auth" — likely a copy/paste artifact)* | `AUTH_API_BASE_URL` | `LoginComponent` |
| `nswag.app.json` | `swagger-app.json` | `src/app/services/api.services.ts` | `APPLICATION_API_BASE_URL` | Authenticated portal components (`ApplicationComponent`, `UserManagementComponent`, `RoleManagementComponent`, `ProfileComponent`, etc.) |
| `nswag.public.json` | `swagger-public.json` | `src/app/services/content.services.ts` | `APPLICATION_API_BASE_URL` *(same token name as `nswag.app.json` — separate generated `InjectionToken` object though, see §2.4)* | Public-site components (`EnquiryModalComponent`, `CoursePromotionCarouselComponent`, `FeedbackCarouselComponent`) |

A fourth config, `nswag.json`, generates against a live-reflected document straight from the
compiled assembly (`aspNetCoreToOpenApi`) into `services/test.services.ts` — **this file doesn't
exist in the Angular repo and no MSBuild target invokes this config.** Treat it as dead/scratch
config, not part of the real pipeline.

### 2.2 How regeneration actually happens

You do **not** run `nswag run` by hand in the normal workflow. `Eduflex/Eduflex.csproj` chains
four `AfterTargets="Build"` MSBuild targets (all skipped when `BuildingInsideDocker=true`):

```
Build (dotnet build, outside Docker)
  → ExportSwagger      : dotnet swagger tofile → swagger-auth.json / swagger-public.json / swagger-app.json
  → NSwagAuth           : nswag run nswag.auth.json    → public.services.ts
  → NSwagPublic         : nswag run nswag.public.json  → content.services.ts
  → NSwagApp            : nswag run nswag.app.json     → api.services.ts
```

**Practical workflow**: change a controller (new endpoint, new DTO field, new route), run a plain
local `dotnet build` on the `Eduflex` project (not inside Docker), and the three Angular client
files are overwritten automatically. There is nothing to run on the Angular side — the generated
files are checked into the repo like any other source file, so just review the diff after a
backend build. Full detail on the export/codegen chain and the Docker skip condition is in
[01-system-architecture.md](01-system-architecture.md) §6.

### 2.3 Codegen settings and what the generated code looks like

All three live configs share: `template: "Angular"` (NSwag's built-in Angular template),
`httpClass: "HttpClient"`, `useSingletonProvider: true`, `injectionTokenType: "InjectionToken"`,
`operationGenerationMode: "MultipleClientsFromOperationId"`, `generateDtoTypes: true`,
`typeStyle: "Class"` (DTOs are TS **classes** with a static `fromJS()` deserializer and a
`toJSON()`, not bare interfaces), `dateTimeType: "Date"`, `exceptionClass: "ApiException"`,
`wrapResponses: false`.

Despite `MultipleClientsFromOperationId`, every generated file ends up exporting **one class,
always literally named `Client`** (not one class per controller) — confirmed by reading the
actual output. This means all three generated files export a same-named `Client` class; a
consuming component must always import it via its specific module path
(`@services/api.services`, `@services/content.services`, or `@services/public.services` — path
aliases configured in `tsconfig.json`) and can never import two of them side-by-side without an
import alias.

```ts
// api.services.ts (excerpt)
export const APPLICATION_API_BASE_URL = new InjectionToken<string>('APPLICATION_API_BASE_URL');

@Injectable({ providedIn: 'root' })
export class Client {
    private http: HttpClient;
    private baseUrl: string;
    constructor(@Inject(HttpClient) http: HttpClient, @Optional() @Inject(APPLICATION_API_BASE_URL) baseUrl?: string) {
        this.http = http;
        this.baseUrl = baseUrl ?? "";
    }
    applicationsAll(): Observable<ApplicationDto[]> { /* GET /api/Applications */ }
    // ... one method per operation, all returning Observable despite promiseType: "Promise" in config
    //     (the Angular template always emits RxJS regardless of that setting)
}
```

**Consumption pattern — no hand-written wrapper.** Every current feature component injects the
generated `Client` class directly by constructor DI (`private appService: Client` from
`api.services.ts`, `private apiClient: Client` from `public.services.ts` or
`content.services.ts`) and calls its methods straight from the component. There is no service
layer between components and the generated client — this is a deliberate simplification (see §5
for the historical hand-written-service pattern this replaced).

### 2.4 ⚠ Known gap: `content.services.ts`'s base URL is never provided

`app.config.ts` only provides `APPLICATION_API_BASE_URL` from `api.services.ts` and
`AUTH_API_BASE_URL` from `public.services.ts`. But `content.services.ts` declares its **own**
`InjectionToken` instance, separately, also labeled `'APPLICATION_API_BASE_URL'` as a string — and
Angular compares injection tokens by **object identity**, not by string label. Since
`app.config.ts` never imports or provides `content.services.ts`'s token, and the generated
constructor uses `@Optional()`, `Client` instances from `content.services.ts` silently fall back
to `baseUrl = ""`. Any component using that client (`EnquiryModalComponent`,
`CoursePromotionCarouselComponent`, `FeedbackCarouselComponent`) issues requests to relative
paths (e.g. `/api/Enquiries`) rather than the configured API origin — which only "works" if the
SPA happens to be served same-origin with the API. In both the dev and production environments
documented in `environment.ts`/`environment.prod.ts`, the API is on a **different origin**, so
this is a live bug affecting the public enquiry form and both carousels. Fix: add
`{ provide: CONTENT_SERVICES_BASE_URL_TOKEN, useValue: environment.apiClientUrl }` to
`app.config.ts`, importing the token from `content.services.ts` specifically (not reusing the
`api.services.ts` one, since it's a different object). Tracked in
[05-findings-and-recommendations.md](05-findings-and-recommendations.md).

## 3. Routing

`src/app/app.routes.ts` is the **single, flat routes array** — there are no feature-level route
files and no `loadChildren`. This is the one file to edit for any new route.

```ts
export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'about', component: AboutComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', loadComponent: () => import('./components/register/register.component').then(m => m.RegisterComponent) },

  {
    path: 'student-portal', component: HomepageComponent, canActivate: [AuthGuard],
    children: [
      { path: 'application', component: ApplicationComponent, canActivate: [AuthGuard] },
      { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },
      { path: '', redirectTo: 'application', pathMatch: 'full' }
    ]
  },
  {
    path: 'staff-portal', component: HomepageComponent, canActivate: [AuthGuard],
    children: [
      { path: 'applications', component: ApplicationComponent, canActivate: [AuthGuard] },
      { path: 'feedback', component: FeedbackManagementComponent, canActivate: [AuthGuard] },
      { path: 'course-promotions', component: CoursePromotionManagementComponent, canActivate: [AuthGuard] },
      { path: 'roles', component: RoleManagementComponent, canActivate: [AuthGuard, RoleGuard], data: { roles: ['Admin'] } },
      { path: 'users', component: UserManagementComponent, canActivate: [AuthGuard, RoleGuard], data: { roles: ['Admin'] } },
      { path: 'profile', component: ProfileComponent, canActivate: [AuthGuard] },
      { path: '', redirectTo: 'applications', pathMatch: 'full' }
    ]
  },
  { path: '**', redirectTo: '' }
];
```

**Where to register a new route** — concretely:

1. Public page → add a top-level entry before the `'**'` catch-all, following
   `{ path: 'x', component: XComponent }`.
2. Student-portal page → add to the `student-portal` `children` array with
   `canActivate: [AuthGuard]`.
3. Staff-portal page → add to the `staff-portal` `children` array; add `RoleGuard` +
   `data: { roles: [...] }` if the page should be role-restricted (only `Admin` is used today, on
   `roles`/`users`).
4. Add a sidebar link in `src/app/components/portal/sidebar/sidebar.component.html` so the new
   page is actually reachable from the portal UI (routes work standalone but nothing links to
   them otherwise).
5. Prefer `loadComponent: () => import(...)` (as `register` already does) over eager `component:`
   registration for anything not needed on first paint — currently only 1 of ~14 routes is
   code-split, so eager loading is the de facto convention, but lazy is the better default for new
   portal-only pages that public visitors never hit.

**Layout is structural, not route-config-driven** — there's no `PublicLayoutComponent`/
`PortalLayoutComponent` route data pattern. `app.component.ts` (the real bootstrapped root)
conditionally shows the public navbar (`*ngIf="!isPortalRoute()"`, checking
`router.url.startsWith('/student-portal'|'/staff-portal')`) around a single `<router-outlet>`,
while `HomepageComponent` (used as the `component:` for both portal parent routes) renders the
sidebar and its own nested `<router-outlet>` for the `children`. `HomepageComponent.ngOnInit` also
does its own redundant `authHelper.isLoggedIn()` check on top of the route-level `AuthGuard` —
harmless belt-and-suspenders, not a second security boundary.

⚠ **SSR root-component mismatch**: `main.ts` bootstraps the real `app.component.ts`
(navbar+footer+outlet) for the client; `main.server.ts` bootstraps a *different*, leftover
scaffold component (`src/app/app.ts`/`app.html`, the default `ng new` output, bare
`RouterOutlet` only) for the server-rendered shell. This means the SSR-rendered HTML and the
client-hydrated HTML come from two different root components. See
[05-findings-and-recommendations.md](05-findings-and-recommendations.md).

### Guards — `src/app/guards/`

- **`auth.guard.ts`** — class-based `AuthGuard implements CanActivate` (not the functional
  `CanActivateFn` style, despite Angular 20 supporting it). Checks `isPlatformBrowser(platformId)`
  first — on the server it unconditionally returns `false` (SSR-safe, avoids touching
  `localStorage` during server rendering); on the browser it delegates to
  `authHelper.isLoggedIn()`, redirecting to `/login` on failure.
- **`role.guard.ts`** — `RoleGuard implements CanActivate`, reads `route.data['roles']` (e.g.
  `['Admin']`) and compares against `authHelper.getUserRole()`. Redirects to `/login` if not
  logged in, or to `/student-portal`/`/staff-portal` (based on the user's actual role) if logged
  in but mismatched. Always paired with `AuthGuard` in the routes array — `RoleGuard` alone does
  not check token validity/expiry, only the role string.

## 4. HTTP interceptor & auth token flow

`src/app/guards/interceptors/auth.interceptor.ts` — functional `HttpInterceptorFn`:

```ts
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authHelper = inject(AuthHelperService);
  const authToken = authHelper.getAuthToken();

  if (req.url.includes('/api/Auth/login') || req.url.includes('/api/Auth/register')) {
    return next(req);
  }
  if (authToken) {
    const authReq = req.clone({ headers: req.headers.set('Authorization', `Bearer ${authToken}`) });
    return next(authReq);
  }
  return next(req);
};
```

Registered in `app.config.ts` via `provideHttpClient(withInterceptors([authInterceptor]), ...)`.
It reads the token through `AuthHelperService.getAuthToken()` → `localStorage.getItem('authToken')`
(guarded by `isPlatformBrowser` for SSR safety). **There is no global 401 handling** — no
auto-logout, no redirect-to-login on an expired/invalid token; a 401 simply propagates to
whatever `.subscribe({ error: ... })` the calling component wrote.

**Login flow**: `LoginComponent` injects the generated `Client` from `public.services.ts` directly
and calls `apiClient.login(loginDto)`. On success it writes directly to `localStorage`:

```ts
localStorage.setItem('authToken', authResponse.token || '');
localStorage.setItem('userData', JSON.stringify({
  id: authResponse.userId, email: authResponse.email, firstName: authResponse.firstName,
  lastName: authResponse.lastName, role: authResponse.roleName, roleId: authResponse.roleId,
  mustChangePassword: authResponse.mustChangePassword
}));
```

⚠ **Two parallel auth-state services exist, and only one is actually wired up.**
`AuthHelperService` (keys `authToken`/`userData`) is the one used by the guards, the interceptor,
and most components — it's the real, live one. `AuthService` (a `BehaviorSubject`-based service
using **different** keys, `access_token`/`user_info`, and the only place that actually decodes+
checks JWT `exp` via `jwt-decode`) is referenced only by `register.component.ts`,
`navbar.component.ts`, and `models/user.ts` — never populated by `LoginComponent`'s
`storeAuthData()`, so its `isLoggedIn` effectively always reads a missing key. Treat
`AuthHelperService` as the source of truth today; `AuthService` should be reconciled or retired.
See [05-findings-and-recommendations.md](05-findings-and-recommendations.md).

## 5. Services layer — direct-client vs. legacy wrapper pattern

`src/app/services/` holds the 3 NSwag-generated files plus `auth.service.ts`,
`auth-helper.service.ts`, and three **superseded, dead** hand-written HTTP wrappers:
`course-promotion.service.ts`, `enquiry.service.ts`, `feedback.service.ts`.

**Current ("after") pattern** — inject the generated `Client` straight into the component:

```ts
import { Client, CreateEnquiryDto } from '@services/content.services';

constructor(private fb: FormBuilder, private apiClient: Client) {}

const payload = new CreateEnquiryDto({ firstName, middleName, lastName, email, mobile, enquiry, recaptchaToken: this.recaptchaToken });
this.apiClient.enquiries(payload).subscribe({ next: () => {...}, error: (err) => {...} });
```

**Legacy ("before") pattern**, kept intentionally as a before/after learning reference (not
deleted, per explicit project decision) — e.g. `enquiry.service.ts`:

```ts
@Injectable({ providedIn: 'root' })
export class EnquiryService {
  private readonly baseUrl = `${environment.apiClientUrl}/api/Enquiries`;
  constructor(private http: HttpClient) {}
  create(enquiry: CreateEnquiry): Observable<Enquiry> {
    return this.http.post<Enquiry>(this.baseUrl, enquiry);
  }
}
```

None of `EnquiryService`, `CoursePromotionService`, or `FeedbackService` is imported anywhere
outside their own file — fully superseded. The trade-off worth understanding: the legacy pattern
hand-duplicates URL-building and DTO shapes that NSwag now generates and keeps in sync
automatically; the current pattern removes that duplication at the cost of coupling components
directly to a generated class (mocking in tests means overriding `Client` via Angular's DI/TestBed
rather than swapping an interface). **When building a new feature, follow the current pattern** —
inject the relevant generated `Client` directly; do not add a new hand-written wrapper service.

## 6. Generic components (`src/generic-components/`)

Reusable, standalone, imported via the `@generic/*` path alias.

### `app-data-table` (`data-table/data-table.component.ts` + `data-table.models.ts`)

Generic over row type `T`, wraps `angular-datatables`/`datatables.net-bs5`:

```ts
export class DataTableComponent<T> implements OnInit, OnDestroy {
  @Input() columns: DataTableColumn<T>[] = [];
  @Input() data: T[] = [];
  @Input() tableClass: string = 'table table-bordered table-hover mb-0';
  @Input() settings?: DataTableSettings;
  @Input() rowActions: DataTableRowAction<T>[] = [
    { action: 'view', label: 'View', icon: 'fa-eye', cssClass: 'btn btn-sm btn-outline-primary' }
  ];
  @Output() actionClick = new EventEmitter<DataTableAction<T>>();
}

export interface DataTableColumn<T = any> {
  field: keyof T | string;
  title: string;
  className?: string;
  formatter?: (value: any, row?: any) => any;   // value+row → display value
  render?: (value: unknown, row: T) => string;   // raw HTML string producer
}
```

A consumer supplies `formatter` for simple value transforms (e.g.
`formatter: (value) => formatDateTime(value, 'dd/MM/yyyy HH:mm')` in `application.component.ts`)
or `render` for full custom cell HTML; `getCellData()` applies whichever is present, falling back
to stringify/boolean-to-Yes-No/JSON.stringify otherwise. Because DataTables/jQuery need a real
DOM, the component only `import('datatables.net-bs5')` and fires `dtTrigger.next()` inside an
`isPlatformBrowser` guard — an SSR-safety measure. Note: `getStatusBadgeClass()` hardcodes an
app-specific status→Bootstrap-badge-class mapping inside this otherwise generic component — a
minor layering leak worth knowing about if reusing this component for an unrelated table.

### `app-modal` (`modal/modal.component.ts`)

A generic confirm/save dialog shell — holds no persistence logic itself, purely emits:

```ts
export class ModalComponent {
  @Input() title = '';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() isSaving = false;
  @Input() saveDisabled = false;
  @Input() showSave = true; @Input() showUpdate = false; @Input() showCancel = true; @Input() showDelete = false;
  @Input() saveLabel = 'Save'; @Input() updateLabel = 'Update'; @Input() cancelLabel = 'Cancel'; @Input() deleteLabel = 'Delete'; @Input() savingLabel = 'Saving...';
  @Output() closeModal = new EventEmitter<void>();
  @Output() save = new EventEmitter<void>();
  @Output() update = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
  @Output() delete = new EventEmitter<void>();
}
```

Content is projected in (the consumer owns the form/body markup); the `show*` booleans control
which action buttons render, and the consumer handles every emitted event. Backdrop/close clicks
both just emit `closeModal`, suppressed while `isSaving` to prevent dismiss mid-save.

### `app-notification` (`notification/notification.component.ts`)

```ts
export class NotificationComponent {
  @Input() type: 'success' | 'error' | 'warning' | 'info' = 'info';
  @Input() message = '';
  @Input() dismissible = false;
  @Output() dismissed = new EventEmitter<void>();
}
```

A color/icon-coded banner (`fa-check-circle`/`fa-exclamation-circle`/`fa-exclamation-triangle`/
`fa-info-circle`), typically paired with `app-modal` around a save/create flow (see Feedback
Management, Course Promotion Management for the reference implementation).

`file-uploader/` and `datetime-picker/` follow the same standalone, single-purpose,
`@Input`/`@Output`-only shape as the three above.

## 7. Styling architecture (brief)

Tailwind v4 (via `@tailwindcss/postcss`, configured in `.postcssrc.json`) coexists with Bootstrap
5 by **deliberately skipping Tailwind's Preflight reset** — `src/styles.css` imports only
`tailwindcss/theme.css` (layer `theme`) and `tailwindcss/utilities.css` (layer `utilities`), never
Preflight, so Bootstrap's own reset/base styles remain in effect. A `@theme { --color-navy-900,
--color-navy-700, --color-accent, --color-surface, --color-border, ... }` block defines the
shared navy/brass brand palette as Tailwind design tokens (usable as `bg-navy-900`/`text-accent`
utilities), unifying what were previously two competing palettes (navy/brass on the public site
vs. blue/orange in the portal). Bootstrap supplies component-level styling, grid, and JS behavior
(forms, DataTables); Tailwind utilities + the `@theme` palette are used for bespoke
layout/branding on top.

## 8. Models (`src/app/models/`)

Five hand-written plain TypeScript `interface` files: `user.ts`, `authResponse.ts`,
`course-promotion.ts`, `enquiry.ts`, `feedback.ts`. These predate the NSwag client generation and
exist to support the now-dead hand-written services in §5 — `course-promotion.ts`, `enquiry.ts`,
and `feedback.ts` are effectively superseded duplicates of the NSwag-generated DTO **classes**
(`CoursePromotionDto`, `EnquiryDto`/`CreateEnquiryDto`, `FeedbackDto`, all exported from
`content.services.ts`/`api.services.ts`), which live components actually use. `user.ts` and
`authResponse.ts` remain live but only in service of the also-mostly-dead `AuthService`/
`register.component.ts`/`navbar.component.ts` path (§4) — the real login flow uses NSwag's
`AuthResponseDto`/`LoginDto` from `public.services.ts` instead. When adding a new feature, prefer
the NSwag-generated DTO types over adding a new hand-written model interface.
