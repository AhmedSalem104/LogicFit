<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8" />
  <img src="https://img.shields.io/badge/C%23-12.0-239120?style=for-the-badge&logo=csharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/SQL%20Server-2022-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white" alt="SQL Server" />
  <img src="https://img.shields.io/badge/JWT-Authentication-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white" alt="JWT" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License" />
</p>

<h1 align="center">LogicFit</h1>

<p align="center">
  <strong>Multi-Tenant Gym Management SaaS Platform</strong>
</p>

<p align="center">
  A comprehensive gym management system built with Clean Architecture, featuring workout programs, diet plans, subscription management, and detailed analytics.
</p>

---

## Overview

**LogicFit** is a powerful SaaS platform designed for gym owners and fitness coaches to manage their clients, create personalized workout programs, design diet plans, track body measurements, and handle subscriptions with full financial reporting.

### Key Highlights

- **Multi-Tenant Architecture** - Complete data isolation between gyms
- **Role-Based Access Control** - Owner, Coach, and Client roles with granular permissions
- **Clean Architecture** - Maintainable, testable, and scalable codebase
- **CQRS Pattern** - Separation of commands and queries for optimal performance

---

## Tech Stack

| Technology | Purpose |
|------------|---------|
| **.NET 8** | Backend Framework |
| **C# 12** | Programming Language |
| **Entity Framework Core** | ORM & Database Access |
| **SQL Server** | Database |
| **MediatR** | CQRS & Mediator Pattern |
| **AutoMapper** | Object Mapping |
| **FluentValidation** | Request Validation |
| **JWT Bearer** | Authentication |
| **BCrypt** | Password Hashing |
| **Serilog** | Structured Logging |

---

## Architecture

```
LogicFit/
├── LogicFit.API/                 # Presentation Layer
│   ├── Features/                 # Controllers organized by feature
│   ├── Middleware/               # Exception handling & Tenant resolution
│   └── wwwroot/uploads/          # File storage
│
├── LogicFit.Application/         # Application Layer
│   ├── Features/                 # CQRS Commands & Queries
│   ├── Common/                   # Behaviors, Interfaces, Models
│   └── Mappings/                 # AutoMapper profiles
│
├── LogicFit.Domain/              # Domain Layer
│   ├── Entities/                 # Domain entities
│   ├── Enums/                    # Enumerations
│   ├── Exceptions/               # Domain exceptions
│   └── Common/                   # Base classes & interfaces
│
└── LogicFit.Infrastructure/      # Infrastructure Layer
    ├── Identity/                 # JWT & Authentication
    ├── Persistence/              # EF Core, Configurations, Migrations
    └── Services/                 # External services implementation
```

---

## Features

### User Management
- **Multi-role system**: Owner, Coach, Client
- **Phone-based authentication** per tenant
- **Password reset** with secure tokens
- **Profile management** with personal details

### Workout Management
- **Workout Programs** - Create structured training programs
- **Program Routines** - Day-by-day workout schedules
- **Exercise Library** - Comprehensive exercise database with muscle targeting
- **Workout Sessions** - Track actual workout performance
- **Set Logging** - Record weight, reps, RPE, and personal records

### Nutrition Management
- **Diet Plans** - Custom meal plans with macro targets
- **Food Database** - Nutritional information per 100g
- **Meal Tracking** - Log actual food consumption
- **Alternative Foods** - Swap options for flexibility

### Subscription Management
- **Subscription Plans** - Flexible pricing tiers
- **Client Subscriptions** - Track active memberships
- **Freeze Subscriptions** - Pause memberships temporarily
- **Sales Tracking** - Attribute sales to coaches

### Coach-Client System
- **Assign Clients** - Link trainees to specific coaches
- **Coach Dashboard** - Personal trainee overview
- **Progress Tracking** - Monitor client improvements

### Body Measurements
- **InBody Integration** - Store body composition data
- **Progress Photos** - Front, side, back photo uploads
- **Historical Tracking** - View measurement trends

### Reports & Analytics
- **Dashboard Report** - Quick overview statistics
- **Clients Report** - Client activity analysis
- **Subscriptions Report** - Membership insights
- **Financial Report** - Revenue tracking
- **Coach Reports** - Trainee performance metrics

---

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register new gym & owner |
| POST | `/api/auth/login` | User login |
| POST | `/api/auth/forget-password` | Request password reset |
| POST | `/api/auth/reset-password` | Reset password |

### Core Resources
| Resource | Endpoints |
|----------|-----------|
| **Clients** | `GET/POST/PUT/DELETE /api/clients` |
| **Users** | `GET/PUT /api/users` |
| **Workout Programs** | `GET/POST/DELETE /api/workoutprograms` |
| **Workout Sessions** | `GET/POST /api/workoutsessions` |
| **Diet Plans** | `GET/POST/DELETE /api/dietplans` |
| **Exercises** | `GET/POST/PUT/DELETE /api/exercises` |
| **Foods** | `GET/POST/PUT/DELETE /api/foods` |
| **Muscles** | `GET /api/muscles` |
| **Body Measurements** | `GET/POST/DELETE /api/bodymeasurements` |
| **Subscriptions** | `GET/POST /api/subscriptions` |
| **Coach Clients** | `GET/POST/DELETE /api/coach-clients` |
| **Gym Profile** | `GET/PUT /api/gymprofile` |
| **Reports** | `GET /api/reports/*` |

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Express, or full)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/BDCdevo/LogicFit.git
   cd LogicFit
   ```

2. **Update the connection string**

   Edit `LogicFit.API/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=.;Database=LogicFitDb;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }
   ```

3. **Apply database migrations**
   ```bash
   cd LogicFit.API
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the API**
   - Swagger UI: `https://localhost:7xxx/swagger`
   - API Base URL: `https://localhost:7xxx/api`

---

## Configuration

### JWT Settings
```json
{
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyHere_MustBe32CharsOrMore",
    "Issuer": "LogicFit",
    "Audience": "LogicFitUsers",
    "ExpiryMinutes": 60
  }
}
```

### File Upload Paths
```
wwwroot/uploads/
├── images/
│   └── {year}/{month}/
│       ├── exercises/
│       ├── measurements/
│       ├── gym-logos/
│       ├── gym-covers/
│       └── gym-gallery/
└── videos/
    └── {year}/{month}/
        └── exercises/
```

---

## User Roles & Permissions

### Owner (Gym Administrator)
- Full system access
- Manage gym profile and branding
- Create subscription plans
- View all reports (dashboard, financial, clients)
- Manage coaches and clients
- Assign clients to any coach

### Coach (Trainer)
- Create workout programs for assigned trainees
- Create diet plans for assigned trainees
- View trainee progress and sessions
- Assign clients to self
- Access coach-specific reports
- Manage exercises and foods

### Client (Gym Member)
- View assigned workout programs
- Start and complete workout sessions
- Log meals and body measurements
- View personal progress
- Read-only access to exercises and foods

---

## Multi-Tenant Architecture

Each gym (tenant) has completely isolated data:

```
┌─────────────────────────────────────┐
│           Tenant (Gym)              │
├─────────────────────────────────────┤
│  Users (Owner, Coaches, Clients)    │
│  Workout Programs & Sessions        │
│  Diet Plans & Meal Logs             │
│  Subscriptions & Plans              │
│  Body Measurements                  │
│  Exercises & Foods (Custom)         │
└─────────────────────────────────────┘
```

- Every entity includes `TenantId` for filtering
- Automatic tenant resolution from JWT claims
- Cross-tenant data access is prevented at the database level

---

## API Response Formats

### Success Response
```json
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5,
  "totalCount": 50
}
```

### Error Response
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "errors": {
    "PhoneNumber": ["Phone number is required"]
  }
}
```

### HTTP Status Codes
| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created |
| 204 | No Content |
| 400 | Bad Request |
| 401 | Unauthorized |
| 403 | Forbidden |
| 404 | Not Found |
| 500 | Server Error |

---

## Documentation

For detailed API documentation and usage examples, see:
- [Project Documentation](PROJECT_DOCUMENTATION.md) - Full system documentation (Arabic)
- [Frontend Developer Guide](FRONTEND_DEVELOPER_GUIDE.md) - Integration guide for frontend developers

---

## Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Contact

**BDCdevo** - [GitHub Profile](https://github.com/BDCdevo)

Project Link: [https://github.com/BDCdevo/LogicFit](https://github.com/BDCdevo/LogicFit)

---

<p align="center">
  Made with dedication for the fitness community
</p>
