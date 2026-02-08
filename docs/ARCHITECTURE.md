# DEMIF - Architecture

## 🏗️ Clean Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Demif.Api                             │
│  Controllers, Middleware, Program.cs                         │
│  (ASP.NET Core 8)                                           │
├─────────────────────────────────────────────────────────────┤
│                    Demif.Application                         │
│  Features (Services), DTOs, Abstractions (Interfaces)       │
│  No external dependencies                                    │
├─────────────────────────────────────────────────────────────┤
│                   Demif.Infrastructure                       │
│  Repositories, DbContext, EF Configurations                 │
│  External services (JWT, Firebase, SEPay)                   │
├─────────────────────────────────────────────────────────────┤
│                      Demif.Domain                            │
│  Entities, Enums, Value Objects                             │
│  Pure C#, no dependencies                                   │
└─────────────────────────────────────────────────────────────┘
```

---

## 📂 Project Structure

```
src/
├── Demif.Api/
│   ├── Controllers/
│   │   ├── Admin/           # Admin-only endpoints
│   │   ├── AuthController.cs
│   │   ├── LessonsController.cs
│   │   ├── SubscriptionPlansController.cs
│   │   └── PaymentsController.cs
│   └── Program.cs
│
├── Demif.Application/
│   ├── Abstractions/
│   │   ├── Repositories/    # IRepository interfaces
│   │   └── Services/        # Service interfaces
│   ├── Common/
│   │   └── Models/          # Result, Error, PagedList
│   └── Features/
│       ├── Auth/
│       ├── Lessons/
│       ├── Payments/
│       ├── Subscriptions/
│       └── Users/
│
├── Demif.Infrastructure/
│   ├── Persistence/
│   │   ├── Configurations/  # EF Type Configs
│   │   ├── Migrations/
│   │   └── ApplicationDbContext.cs
│   ├── Repositories/
│   └── Services/
│
└── Demif.Domain/
    ├── Entities/
    └── Enums/
```

---

## 🔧 Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core 8 |
| Database | PostgreSQL |
| ORM | Entity Framework Core |
| Auth | JWT + Firebase |
| Container | Docker |
| Deploy | Coolify |

---

## 🔐 Authorization Policies

| Policy | Roles |
|--------|-------|
| RequireAdmin | Admin |
| RequireStaff | Admin, Staff |
| RequireUser | Admin, Staff, User, Premium |
| RequirePremium | Admin, Premium |
