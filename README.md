<div align="center">

# LogicFit

### Enterprise-Grade Gym Management Platform

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?style=flat-square&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?style=flat-square&logo=nuget&logoColor=white)](https://docs.microsoft.com/en-us/ef/core/)
[![JWT](https://img.shields.io/badge/JWT-Auth-000000?style=flat-square&logo=jsonwebtokens&logoColor=white)](https://jwt.io/)
[![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)](LICENSE)

<p align="center">
  <img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/dotnetcore/dotnetcore-original.svg" alt="dotnet" width="60" height="60"/>
  <img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/csharp/csharp-original.svg" alt="csharp" width="60" height="60"/>
  <img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/microsoftsqlserver/microsoftsqlserver-plain-wordmark.svg" alt="sqlserver" width="60" height="60"/>
  <img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/swagger/swagger-original.svg" alt="swagger" width="60" height="60"/>
</p>

**A powerful Multi-Tenant SaaS platform for gym owners and fitness coaches**

[Features](#-features) · [Tech Stack](#-tech-stack) · [Quick Start](#-quick-start) · [API Docs](#-api-documentation) · [Architecture](#-architecture)

---

</div>

## Overview

**LogicFit** is a comprehensive gym management solution built with Clean Architecture principles. It enables gym owners to manage their entire operation - from client subscriptions and coach assignments to personalized workout programs and nutrition plans - all within a secure, multi-tenant environment.

### Why LogicFit?

| Challenge | Solution |
|-----------|----------|
| Managing multiple gyms | Multi-tenant architecture with complete data isolation |
| Complex role permissions | Granular RBAC (Owner, Coach, Client) |
| Tracking client progress | Body measurements, workout sessions, and analytics |
| Subscription management | Flexible plans with freeze/cancel capabilities |
| Coach-client relationships | Dedicated assignment and progress tracking system |

---

## Features

<table>
<tr>
<td width="50%">

### Client Management
- Complete client profiles with health data
- Subscription tracking and history
- Body measurement records with progress photos
- Personal records and achievements

### Workout System
- Custom workout program builder
- Day-by-day routine scheduling
- Exercise library with muscle targeting
- Session tracking with set logging
- RPE and volume load calculations

### Nutrition Planning
- Personalized diet plans
- Macro targets (calories, protein, carbs, fats)
- Food database with nutritional info
- Meal logging and alternatives

</td>
<td width="50%">

### Subscription Management
- Flexible subscription plans
- Auto-expiry tracking
- Freeze/unfreeze capabilities
- Sales attribution to coaches
- Financial reporting

### Analytics & Reports
- Real-time dashboard statistics
- Client activity reports
- Financial analytics
- Coach performance metrics
- Trainee progress reports

### Multi-Tenant Security
- Complete data isolation per gym
- JWT-based authentication
- Role-based access control
- Secure password hashing (BCrypt)

</td>
</tr>
</table>

---

## Tech Stack

<table>
<tr>
<td align="center" width="96">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/dotnetcore/dotnetcore-original.svg" width="48" height="48" alt=".NET" />
<br><strong>.NET 8</strong>
</td>
<td align="center" width="96">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/csharp/csharp-original.svg" width="48" height="48" alt="C#" />
<br><strong>C# 12</strong>
</td>
<td align="center" width="96">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/microsoftsqlserver/microsoftsqlserver-plain.svg" width="48" height="48" alt="SQL Server" />
<br><strong>SQL Server</strong>
</td>
<td align="center" width="96">
<img src="https://raw.githubusercontent.com/devicons/devicon/master/icons/swagger/swagger-original.svg" width="48" height="48" alt="Swagger" />
<br><strong>Swagger</strong>
</td>
</tr>
</table>

| Category | Technologies |
|----------|-------------|
| **Framework** | .NET 8, ASP.NET Core Web API |
| **Language** | C# 12 |
| **Database** | SQL Server, Entity Framework Core 8 |
| **Architecture** | Clean Architecture, CQRS, Mediator Pattern |
| **Libraries** | MediatR, AutoMapper, FluentValidation, Serilog |
| **Security** | JWT Bearer Tokens, BCrypt Password Hashing |
| **Documentation** | Swagger / OpenAPI |

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                          LogicFit Solution                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │                    LogicFit.API                              │   │
│  │                 (Presentation Layer)                         │   │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────────────┐    │   │
│  │  │ Controllers │ │ Middleware  │ │ Dependency Injection│    │   │
│  │  └─────────────┘ └─────────────┘ └─────────────────────┘    │   │
│  └──────────────────────────┬──────────────────────────────────┘   │
│                             │                                       │
│  ┌──────────────────────────▼──────────────────────────────────┐   │
│  │                LogicFit.Application                          │   │
│  │                  (Application Layer)                         │   │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐    │   │
│  │  │ Commands │ │ Queries  │ │   DTOs   │ │  Validators  │    │   │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────┘    │   │
│  └──────────────────────────┬──────────────────────────────────┘   │
│                             │                                       │
│  ┌──────────────────────────▼──────────────────────────────────┐   │
│  │                  LogicFit.Domain                             │   │
│  │                   (Domain Layer)                             │   │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐    │   │
│  │  │ Entities │ │  Enums   │ │Exceptions│ │ Value Objects│    │   │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────┘    │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                             ▲                                       │
│  ┌──────────────────────────┴──────────────────────────────────┐   │
│  │              LogicFit.Infrastructure                         │   │
│  │                (Infrastructure Layer)                        │   │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐    │   │
│  │  │DbContext │ │ Identity │ │ Services │ │ Repositories │    │   │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────┘    │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Project Structure

```
LogicFit/
│
├── LogicFit.API/                    # Presentation Layer
│   ├── Features/                    # Feature-based Controllers
│   │   ├── Auth/                    # Authentication endpoints
│   │   ├── Clients/                 # Client management
│   │   ├── WorkoutPrograms/         # Workout management
│   │   ├── DietPlans/               # Nutrition management
│   │   ├── Subscriptions/           # Subscription handling
│   │   └── Reports/                 # Analytics & reporting
│   ├── Middleware/                  # Custom middleware
│   └── wwwroot/uploads/             # File storage
│
├── LogicFit.Application/            # Application Layer
│   ├── Features/                    # CQRS Commands & Queries
│   ├── Common/                      # Shared components
│   │   ├── Behaviors/               # Pipeline behaviors
│   │   ├── Interfaces/              # Abstractions
│   │   └── Models/                  # Shared models
│   └── Mappings/                    # AutoMapper profiles
│
├── LogicFit.Domain/                 # Domain Layer
│   ├── Entities/                    # Domain entities
│   ├── Enums/                       # Enumerations
│   ├── Exceptions/                  # Domain exceptions
│   └── Common/                      # Base classes
│
└── LogicFit.Infrastructure/         # Infrastructure Layer
    ├── Identity/                    # JWT & Auth services
    ├── Persistence/                 # EF Core & Database
    │   ├── Configurations/          # Entity configurations
    │   ├── Migrations/              # Database migrations
    │   └── SeedData/                # Initial data
    └── Services/                    # External services
```

---

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB / Express / Full)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Installation

```bash
# Clone the repository
git clone https://github.com/AhmedSalem104/LogicFit.git

# Navigate to project directory
cd LogicFit

# Restore dependencies
dotnet restore

# Update connection string in LogicFit.API/appsettings.json
# Then apply migrations
cd LogicFit.API
dotnet ef database update

# Run the application
dotnet run
```

### Configuration

Update `appsettings.json` with your settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=LogicFitDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyHere_MustBe32CharsOrMore",
    "Issuer": "LogicFit",
    "Audience": "LogicFitUsers",
    "ExpiryMinutes": 60
  }
}
```

---

## API Documentation

### Authentication Endpoints

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `POST` | `/api/auth/register` | Register new gym with owner account |
| `POST` | `/api/auth/login` | Authenticate and receive JWT token |
| `POST` | `/api/auth/forget-password` | Request password reset token |
| `POST` | `/api/auth/reset-password` | Reset password with token |

### Core API Endpoints

<details>
<summary><strong>Client Management</strong></summary>

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/api/clients` | Get all clients (paginated) |
| `GET` | `/api/clients/{id}` | Get client by ID |
| `POST` | `/api/clients` | Create new client |
| `PUT` | `/api/clients/{id}` | Update client |
| `DELETE` | `/api/clients/{id}` | Delete client (soft delete) |

</details>

<details>
<summary><strong>Workout Programs</strong></summary>

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/api/workoutprograms` | Get all programs |
| `GET` | `/api/workoutprograms/{id}` | Get program with routines |
| `POST` | `/api/workoutprograms` | Create program |
| `POST` | `/api/workoutprograms/{id}/routines` | Add routine to program |
| `POST` | `/api/workoutprograms/routines/{id}/exercises` | Add exercise to routine |
| `DELETE` | `/api/workoutprograms/{id}` | Delete program |

</details>

<details>
<summary><strong>Diet Plans</strong></summary>

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/api/dietplans` | Get all diet plans |
| `GET` | `/api/dietplans/{id}` | Get plan with meals |
| `POST` | `/api/dietplans` | Create diet plan |
| `POST` | `/api/dietplans/{id}/meals` | Add meal to plan |
| `POST` | `/api/dietplans/meals/{id}/items` | Add food to meal |
| `DELETE` | `/api/dietplans/{id}` | Delete plan |

</details>

<details>
<summary><strong>Subscriptions</strong></summary>

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/api/subscriptions/plans` | Get subscription plans |
| `POST` | `/api/subscriptions/plans` | Create subscription plan |
| `GET` | `/api/subscriptions` | Get client subscriptions |
| `POST` | `/api/subscriptions` | Create client subscription |
| `POST` | `/api/subscriptions/{id}/freeze` | Freeze subscription |
| `POST` | `/api/subscriptions/{id}/cancel` | Cancel subscription |

</details>

<details>
<summary><strong>Reports & Analytics</strong></summary>

| Method | Endpoint | Description |
|:------:|----------|-------------|
| `GET` | `/api/reports/dashboard` | Dashboard overview |
| `GET` | `/api/reports/clients` | Clients report |
| `GET` | `/api/reports/subscriptions` | Subscriptions report |
| `GET` | `/api/reports/financial` | Financial report |
| `GET` | `/api/reports/coach/dashboard` | Coach dashboard |
| `GET` | `/api/reports/coach/trainees` | Coach trainees report |

</details>

### Response Format

```json
// Success Response (Paginated)
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "totalCount": 50,
  "hasPreviousPage": false,
  "hasNextPage": true
}

// Error Response
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "Field": ["Error message"]
  }
}
```

---

## User Roles & Permissions

<table>
<tr>
<th width="33%">Owner</th>
<th width="33%">Coach</th>
<th width="33%">Client</th>
</tr>
<tr>
<td>

- Full system access
- Manage gym profile
- Create subscription plans
- View all reports
- Manage all users
- Assign clients to coaches
- Financial oversight

</td>
<td>

- Manage assigned trainees
- Create workout programs
- Create diet plans
- Track trainee progress
- View coach reports
- Self-assign clients
- Manage exercises/foods

</td>
<td>

- View assigned programs
- Log workout sessions
- Track body measurements
- Log meals
- View personal progress
- Read-only resources

</td>
</tr>
</table>

---

## Multi-Tenant Architecture

```
┌────────────────────────────────────────────────────────────┐
│                      LogicFit Platform                      │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐        │
│  │   Gym A     │  │   Gym B     │  │   Gym C     │        │
│  │  (Tenant)   │  │  (Tenant)   │  │  (Tenant)   │        │
│  ├─────────────┤  ├─────────────┤  ├─────────────┤        │
│  │ • Users     │  │ • Users     │  │ • Users     │        │
│  │ • Programs  │  │ • Programs  │  │ • Programs  │        │
│  │ • Plans     │  │ • Plans     │  │ • Plans     │        │
│  │ • Subs      │  │ • Subs      │  │ • Subs      │        │
│  └─────────────┘  └─────────────┘  └─────────────┘        │
│        ▲                ▲                ▲                 │
│        │                │                │                 │
│        └────────────────┼────────────────┘                 │
│                         │                                  │
│              ┌──────────┴──────────┐                       │
│              │  TenantId Filtering │                       │
│              │  (Automatic/Secure) │                       │
│              └─────────────────────┘                       │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

**Key Features:**
- Complete data isolation between tenants
- Automatic tenant resolution from JWT claims
- TenantId included in all entity queries
- Cross-tenant access prevention at database level

---

## Documentation

| Document | Description |
|----------|-------------|
| [PROJECT_DOCUMENTATION.md](PROJECT_DOCUMENTATION.md) | Complete system documentation (Arabic) |
| [FRONTEND_DEVELOPER_GUIDE.md](FRONTEND_DEVELOPER_GUIDE.md) | Frontend integration guide |

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

<div align="center">

## Contact

**Ahmed Salem** · [GitHub](https://github.com/AhmedSalem104)

[![GitHub](https://img.shields.io/badge/GitHub-AhmedSalem104-181717?style=for-the-badge&logo=github)](https://github.com/AhmedSalem104)

---

<sub>Built with passion for the fitness community</sub>

</div>
