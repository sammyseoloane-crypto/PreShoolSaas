# BabyCareApp — Preschool Management & Discovery SaaS

Multi-tenant preschool management, parent portal, and discovery marketplace platform.

- Frontend: Next.js (TypeScript), static export, deployed to Firebase Hosting (`babycareapp-8f3b5`).
- Backend: ASP.NET Core Web API (.NET 8), PostgreSQL with Row Level Security.

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the full solution architecture
(application structure, tenant isolation, auth, API conventions, deployment).

Development proceeds in small, independently testable slices — see `ARCHITECTURE.md` for current
status and open decisions.
