# CYH Care: Antigravity Master Prompt

## ROLE
You are a senior .NET architect and full-stack developer. Build the project "CYH Care" step by step, exactly as specified below. Work in PHASES. After each phase: build the solution, run it, fix errors, summarise what you did, and STOP. Wait for me to say "next phase" before continuing.

## PROJECT SUMMARY
CYH Care ("People Help People") is a platform where a **User** creates a job (hospital visit / local assistance), nearby verified **Helpers** get notified, one Helper accepts, and GPS tracking runs in "Duty + Arrival" mode until the job is completed. Working hours then generate a payment. An **Admin** verifies helpers and watches a live map.

Roles: `User`, `Helper`, `Admin`.

Job status flow: `Pending -> Accepted -> OnTheWay -> Arrived -> InProgress -> Completed` (also `Cancelled`).

## TECH STACK (fixed, do not change)
- .NET 8, ASP.NET Core Web API (JWT auth)
- Blazor Web App (Server interactivity) for User dashboard and Admin panel
- .NET MAUI Blazor Hybrid for the Helper mobile app (later phase)
- SQL Server
- **Data access: Dapper + Stored Procedures ONLY. No EF Core. No inline SQL in C#.**
- SignalR for live tracking and notifications
- BCrypt.Net-Next for password hashing
- Leaflet.js + OpenStreetMap for maps (no Google Maps)

## SOLUTION STRUCTURE
```
CYHCare.sln
|-- CYHCare.Domain          (entities, enums, no dependencies)
|-- CYHCare.Application     (interfaces, DTOs, services, validators)
|-- CYHCare.Infrastructure  (Dapper repositories, DbConnectionFactory)
|-- CYHCare.Api             (controllers, JWT, SignalR TrackingHub)
|-- CYHCare.Shared.UI       (Razor Class Library, shared components)
|-- CYHCare.Web             (Blazor: User + Admin)
|-- CYHCare.Mobile          (MAUI Blazor Hybrid: Helper app)  [later]
|-- Database/               (01_Tables.sql, 02_StoredProcedures.sql, 03_Seed.sql)
```

## CODING RULES
1. Every DB call goes through a stored procedure named `sp_<Entity>_<Action>` (example: `sp_Job_Create`).
2. One repository per entity. Repositories only call SPs via Dapper with `CommandType.StoredProcedure`.
3. Controllers are thin: validate input, call a service, return a result. Business rules live in Application services.
4. Use DTOs for API input and output. Never expose entities directly.
5. All times stored in UTC. Passwords never stored in plain text and never returned in any response.
6. Use `async/await` everywhere, `ILogger`, and a global exception-handling middleware returning a consistent JSON error shape.
7. Authorization: `[Authorize(Roles = "...")]` on every endpoint except register and login.
8. Connection string and JWT secret go in `appsettings.json` placeholders (use user-secrets for real values). Never hardcode secrets.
9. Every SP that changes more than one table runs inside `BEGIN TRAN ... COMMIT` with `TRY/CATCH` and `THROW`.
10. Add XML comments to public service methods. Keep code readable over clever.

## PHASES (execute one at a time)
- Phase 1: Database
- Phase 2: Solution skeleton
- Phase 3: Domain and repositories
- Phase 4: Auth API
- Phase 5: Job flow APIs
- Phase 6: Live tracking
- Phase 7: Blazor Web
- Phase 8: Mobile Helper app (MAUI Blazor Hybrid)
- Phase 9: Privacy and hardening
