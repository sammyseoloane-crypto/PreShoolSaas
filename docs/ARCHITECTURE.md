# BabyCareApp — Solution Architecture (Slice 1)

Status: DRAFT — pending approval. No implementation code, migrations, or packages have been introduced yet.

## 0. Context

This repository is a greenfield build (confirmed in Slice 0: both the local folder and the
`PreShoolSaas` GitHub repo were empty with no prior history). This document defines the
architecture before any implementation begins, per the mandatory slice-based development process.

Two decisions were confirmed with the product owner before drafting this document:

- **Firebase's role is Hosting-only.** The Next.js frontend is deployed as a static site to
  Firebase Hosting (project `babycareapp-8f3b5`). Firebase is **not** used as the application
  database, auth provider, or backend compute. The backend remains ASP.NET Core Web API against
  PostgreSQL, as mandated.
- Two infrastructure choices are still **open** and flagged in §10 (API hosting provider,
  PostgreSQL hosting provider) — these involve accounts/billing the product owner should pick,
  not a unilateral technical call.

## 1. Application Structure

Monorepo, two deployable units plus shared contracts:

```
/BabyCareApp
  /apps
    /web                 Next.js 14+ (App Router), TypeScript — static export
    /api                 ASP.NET Core Web API (.NET 8 LTS)
  /docs
    ARCHITECTURE.md       (this file)
  /.github/workflows      CI pipelines (frontend deploy, backend build/test/deploy)
  firebase.json           Firebase Hosting config (apps/web static output only)
  .firebaserc
```

`apps/api` is itself a layered solution:

```
apps/api/
  BabyCareApp.Api/            Controllers, DTOs, middleware, composition root
  BabyCareApp.Application/    Services, validators, use cases, interfaces
  BabyCareApp.Domain/         Entities, enums, domain rules (no EF/infra dependencies)
  BabyCareApp.Infrastructure/ EF Core DbContext, repositories, migrations, external integrations
  BabyCareApp.Tests/          Unit + integration tests
```

Rationale: this separation is what lets tenant isolation, authorization, and business rules be
unit-tested without a database, and keeps EF Core out of the domain layer.

## 2. Frontend Architecture

- **Next.js App Router, TypeScript, `output: 'export'`** (static export) — not Firebase's
  SSR-via-Cloud-Functions integration. This keeps Firebase strictly a static host, with zero
  Firebase backend compute, matching the Hosting-only decision and avoiding a second compute
  runtime to operate.
- All dynamic behavior is client-side, driven entirely by REST calls to the ASP.NET Core API.
  There is no server-side code in the frontend deployable.
- **Centralised API client**: a single typed fetch wrapper (`lib/api-client.ts`) is the only place
  that constructs requests to the backend. All request/response shapes are typed DTOs generated
  from or mirrored against the backend's contracts — no ad-hoc `fetch` calls scattered through
  components.
- **Route groups by audience**: `(marketplace)` public routes, `(parent)`, `(staff)`, `(admin)` —
  each with its own layout and role-aware navigation guard. Guards are a UX convenience only;
  the API is the real enforcement boundary (see §6).
- **Design system**: a shared component library (`/apps/web/components/ui`) — buttons, forms,
  tables, modals — built once and reused across the owner/admin/teacher/parent/marketplace
  surfaces, so the four audiences don't drift into inconsistent UI patterns.

## 3. Backend Architecture

- **ASP.NET Core Web API, .NET 8 LTS**, layered as in §1.
- Cross-cutting concerns as middleware, in pipeline order:
  1. Exception handling (→ RFC 7807 Problem Details, §8)
  2. Request logging / correlation ID (§9)
  3. Authentication (JWT bearer)
  4. Tenant resolution (§4)
  5. Authorization (policy-based, §6)
- Controllers are thin: validate input shape → call an Application-layer service → map result to
  a DTO. Business rules live in `Application`, never in controllers.
- EF Core Code-First; one migration per slice that touches schema, never a single giant initial
  migration for unrelated modules.

## 4. Tenant Resolution Strategy

- Every tenant-owned table carries a non-nullable `tenant_id` column (FK → `tenants.id`).
- The JWT access token carries `user_id` only — **not** a baked-in tenant list, so membership
  changes don't require re-issuing tokens.
- Each request to a tenant-scoped endpoint must include an `X-Tenant-Id` header (or it's derived
  from a route segment for admin cross-tenant endpoints). A `TenantResolutionMiddleware`:
  1. Reads the requested tenant id.
  2. Validates the authenticated user actually has an active membership in that tenant (DB-backed,
     short-lived cache — never trusts a claim alone).
  3. Populates a scoped `ITenantContext` used by the Application layer.
  4. Sets a PostgreSQL session variable (`SET LOCAL app.tenant_id = '...'`) for the duration of the
     request, so RLS policies (§5) enforce isolation even if an Application-layer bug forgets a
     `WHERE tenant_id = ...` filter.
- Inserts always assign `tenant_id` from `ITenantContext`, never from client-supplied payload.
- Background jobs explicitly construct a tenant context per unit of work before touching
  tenant-owned data — there is no "ambient" tenant for a job.

## 5. Database Architecture

- **PostgreSQL**, EF Core Code-First migrations, UUID primary keys (`gen_random_uuid()`).
- Row Level Security enabled on every tenant-owned table; policy predicate compares the row's
  `tenant_id` against `current_setting('app.tenant_id')::uuid`. RLS is a defense-in-depth layer
  behind application-level scoping, not a replacement for it.
- `tenant_id` is the leading column of composite indexes on tenant-owned tables.
- Hosting provider is an open decision — see §10.

## 6. Authentication Strategy

- Custom authentication (not Firebase Auth, not ASP.NET Identity's default UI) issuing a
  short-lived JWT **access token** (~15 min) plus a longer-lived **refresh token**, stored
  server-side in a `refresh_tokens` table with rotation and revocation on logout.
- Passwords hashed with ASP.NET Core Identity's PBKDF2 hasher (reused as a library, not its
  scaffolding).
- Because the frontend (Firebase Hosting domain) and backend (separate API domain) are
  cross-origin, tokens are held in the SPA (short-lived access token in memory, refresh token in
  `localStorage` with rotation) rather than relying on cross-site cookies, which third-party
  cookie restrictions in modern browsers make unreliable for this split-domain setup. Mitigations:
  short access-token lifetime, refresh rotation + reuse detection, strict CORS allowlist, CSP.
  If the product owner later wants cookie-based auth, that requires putting the frontend and API
  behind the same apex domain — worth revisiting once a custom domain is in place.

## 7. Authorisation Strategy

- Permission-based, not hardcoded role checks: `Permissions`, `Roles`, `RolePermissions`, and
  `TenantMemberships` (user × tenant × role) tables.
- Enforced via ASP.NET Core policy-based authorization: `[Authorize(Policy = "learners.write")]`
  resolved by a custom `IAuthorizationHandler` that checks the current user's effective
  permissions for the resolved tenant (§4).
- The frontend receives the caller's effective permission set at login/tenant-switch purely to
  drive UI (hide/disable controls) — it is never the enforcement boundary.

## 8. API Conventions

- REST, versioned from day one: `/api/v1/{resource}`.
- Errors as RFC 7807 Problem Details (`application/problem+json`) with a stable `type` per domain
  exception so the frontend can branch on it without parsing message strings.
- Pagination via `?page=&pageSize=` initially; cursor-based pagination reserved for
  high-volume endpoints (e.g. audit logs) if needed later.
- DTOs are always distinct types from EF entities — no entity ever serializes directly to a
  response.

## 9. Error Handling & Logging

- Centralised exception-handling middleware maps typed domain exceptions
  (`NotFoundException`, `ForbiddenException`, `ValidationException`, `TenantMismatchException`,
  etc.) to the correct HTTP status + Problem Details body. No controller catches exceptions itself.
- Structured logging via Serilog, JSON output, enriched with `correlation_id`, `tenant_id`, and
  `user_id` on every log line so a single request's activity can be traced across layers. Sinks
  start as console/stdout (consumed by whatever hosting provider's log aggregation is chosen,
  §10) — no additional logging infrastructure is introduced speculatively.

## 10. Deployment Architecture

- **Frontend**: `apps/web` → `next build` (static export) → Firebase Hosting, project
  `babycareapp-8f3b5`. CI: GitHub Actions, deploy on push to `main`.
- **Backend**: `apps/api` → Docker image → **hosting provider not yet chosen**. Candidates:
  Azure App Service (good fit if other Azure resources exist already), Render, or Fly.io
  (simpler, cheaper for an early-stage SaaS). Recommendation: Render or Fly.io for now given this
  is a new project with no existing Azure footprint — but this carries ongoing cost and account
  setup, so it's the product owner's call, not mine to make unilaterally.
- **Database**: PostgreSQL — **hosting provider not yet chosen**. Candidates: Supabase (the
  master spec explicitly prefers preserving an existing Supabase environment if one exists; none
  exists here, but Supabase remains a reasonable default since it bundles managed Postgres +
  connection pooling + an easy RLS-friendly setup), or a managed Postgres instance from whichever
  provider hosts the API. Recommendation: Supabase, unless there's a reason to co-locate DB and
  API with one provider.
- Environments: `production` from the start; a `staging` environment is recommended before
  onboarding real schools, but is not a blocker for early slices.

## Open Decisions (need product-owner input before Slice 2)

1. API hosting provider (Azure App Service / Render / Fly.io / other).
2. PostgreSQL hosting provider (Supabase / other managed Postgres).

## Completion Status

**COMPLETE AND VERIFIED** for Slice 1 scope (architecture documentation only). No code,
migrations, or packages were introduced. Awaiting approval to proceed to Slice 2 (Database
Foundation), plus answers to the two open decisions above.
