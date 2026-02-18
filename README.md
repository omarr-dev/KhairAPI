# Khair API — نظام إدارة حلقات تحفيظ القرآن الكريم

A production-ready REST API for managing Quran memorization circles (Halaqat). Built with ASP.NET Core 8, it supports multi-tenant organizations, role-based access, student progress tracking, and automated background jobs.

---

## Features

- **Multi-Tenancy** — Each organization (association) operates in a fully isolated data scope via JWT-based tenant resolution
- **Role-Based Access Control** — Three roles: `Supervisor`, `HalaqaSupervisor`, and `Teacher`, each with scoped permissions
- **Student & Teacher Management** — Full CRUD for students, teachers, and their halaqa assignments
- **Attendance Tracking** — Daily attendance recording for students and teachers, with automated absence marking at end-of-day via background jobs
- **Progress Records** — Track Quran memorization progress per student (Hifz, Muraja'a, Tajweed)
- **Streak & Target System** — Set memorization targets per student and automatically reset streaks on missed days
- **Statistics & Reports** — At-risk student detection, attendance rates, and achievement summaries per halaqa or teacher
- **Excel Export** — Export attendance and progress data as `.xlsx` files using ClosedXML
- **Background Jobs** — Hangfire-powered scheduled jobs (PostgreSQL storage) with a protected dashboard
- **Swagger UI** — Interactive API documentation with JWT authentication support

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 8 |
| Database | PostgreSQL (via Npgsql + EF Core 8) |
| Authentication | JWT Bearer Tokens |
| Background Jobs | Hangfire + Hangfire.PostgreSql |
| ORM | Entity Framework Core 8 |
| Mapping | AutoMapper |
| Validation | FluentValidation |
| Export | ClosedXML |
| Docs | Swashbuckle / Swagger |

---

## Project Structure

```
KhairAPI/
├── Controllers/       # HTTP endpoints (Auth, Students, Teachers, Attendance, etc.)
├── Services/
│   ├── Interfaces/    # Service contracts
│   └── Implementations/  # Business logic
├── Models/
│   ├── Entities/      # EF Core database models
│   └── DTOs/          # Request/response shapes
├── Core/
│   ├── Extensions/    # Service registration, caching, and helper extensions
│   ├── Middleware/    # Global exception handler
│   └── Responses/     # Standardized API response wrappers
├── Middleware/        # Tenant resolution middleware
├── Filters/           # Hangfire dashboard authorization
├── Migrations/        # EF Core database migrations
└── Data/              # AppDbContext and seed data
```

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL 14+

### Setup

**1. Clone the repository**

```bash
git clone https://github.com/your-username/KhairAPI.git
cd KhairAPI
```

**2. Configure settings**

Copy the sample config and fill in your values:

```bash
cp appsettings.sample.json appsettings.json
```

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=khair_db;Username=postgres;Password=your_password"
  },
  "JwtSettings": {
    "SecretKey": "a-secret-key-at-least-32-characters-long",
    "Issuer": "KhairAPI",
    "Audience": "KhairFrontend",
    "ExpirationMinutes": 1440
  }
}
```

**3. Run migrations and start**

```bash
dotnet ef database update
dotnet run
```

The API will be available at `https://localhost:5001`.
Swagger UI: `https://localhost:5001/swagger`

---

## API Overview

| Group | Endpoints |
|---|---|
| Auth | `POST /api/auth/login`, `POST /api/auth/register` |
| Students | `GET/POST/PUT/DELETE /api/students` |
| Teachers | `GET/POST/PUT/DELETE /api/teachers` |
| Halaqat | `GET/POST/PUT/DELETE /api/halaqat` |
| Attendance | `POST /api/attendance`, `GET /api/attendance/{halaqaId}` |
| Teacher Attendance | `POST /api/teacher-attendance` |
| Progress | `POST /api/progress`, `GET /api/progress/{studentId}` |
| Statistics | `GET /api/statistics/at-risk`, `GET /api/statistics/summary` |
| Export | `GET /api/export/attendance`, `GET /api/export/progress` |
| Hangfire Dashboard | `/hangfire` (Supervisor role required) |

---

## Background Jobs

Two recurring jobs run daily at **23:59 KSA (Arab Standard Time)**:

- `mark-absent-students` — Automatically marks students as absent if no attendance was recorded for the day
- `reset-streaks-for-missed-targets` — Resets memorization streaks for students who did not meet their daily target

---

## License

MIT
