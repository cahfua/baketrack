# BakeTrack

Bakery Inventory and Production Management System for the CSE 499 team project.

## Overview

BakeTrack is a web application designed to help small bakeries manage ingredients, monitor inventory levels, organize products and recipes, and track daily production.

### Core Features

- **Ingredient Management:** Add, edit, and manage bakery ingredients.
- **Stock Level Monitoring:** Track ingredient quantities and identify low-stock items.
- **Product Management:** Manage bakery products and their associated recipes.
- **Daily Production Tracking:** Record daily production and track ingredient usage.
- **Role-Based Access Control:** Admin and Staff roles powered by ASP.NET Core Identity.

### Planned Enhancements

- Search and filtering.
- Ingredient expiry date monitoring.
- Waste management and reporting.

## Tech Stack

- **Framework:** ASP.NET Core / Blazor (Interactive Server, .NET 10 / C#)
- **Database & ORM:** PostgreSQL + Entity Framework Core (`Npgsql`)
- **Frontend:** HTML, CSS, Bootstrap
- **Deployment:** Docker + Render.com

## Quick Start

Run all commands below from the repository root.

### 1. Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/downloads)
- [Docker](https://www.docker.com/) *(recommended)* or a local PostgreSQL installation
- IDE: [Visual Studio Code](https://code.visualstudio.com/) (with [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)) or Visual Studio

### 2. Clone the Repository

```bash
git clone https://github.com/cahfua/baketrack.git
cd baketrack
```

### 3. Configure Local PostgreSQL

Each developer uses a local PostgreSQL database via **.NET User Secrets** so credentials are never committed and `Development` runs never connect to production.

**Option A: Docker (recommended)**

```bash
docker compose up -d
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=baketrack;Username=baketrack;Password=baketrack_dev" --project src/BakeTrack
```

**Option B: Existing local PostgreSQL**

1. Create an empty database (e.g., `CREATE DATABASE baketrack;`).
2. Save your connection string in User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=baketrack;Username=YOUR_USER;Password=YOUR_PASSWORD" --project src/BakeTrack
```

### 4. Run the Application

```bash
dotnet run --project src/BakeTrack
```

Dependencies restore, EF Core migrations apply automatically on startup, and the app starts at `http://localhost:5285` (or `https://localhost:7079`). For hot reload during development, use `dotnet watch --project src/BakeTrack`.

## Authentication & User Roles

BakeTrack uses **ASP.NET Core Identity** with admin-managed account creation (public registration is disabled):

- **`Admin`**: Full access to Dashboard, Ingredients (including delete), Products, Production, Waste & Reports (`/waste`), and User Management (`/users`).
- **`Staff`**: Operational access to Dashboard, Ingredients (add/edit only), Products, and Production.

In `Development`, two demo accounts are seeded automatically on startup:

- **Admin:** `admin@baketrack.local` / `Admin123!`
- **Staff:** `staff@baketrack.local` / `Staff123!`

To override the seeded admin credentials locally:

```bash
dotnet user-secrets set "ADMIN_EMAIL" "your-admin@example.com" --project src/BakeTrack
dotnet user-secrets set "ADMIN_PASSWORD" "YourSecurePassword1" --project src/BakeTrack
```

## Project Structure

```text
baketrack/
├── docs/                  # Deployment and test documentation
├── src/
│   └── BakeTrack/
│       ├── Components/    # Blazor layout and pages
│       ├── data/          # DbContext, seeding, and DB config
│       ├── Migrations/    # EF Core migrations
│       ├── models/        # Domain and Identity models
│       ├── Services/      # Business logic and data services
│       ├── wwwroot/       # Static assets
│       ├── appsettings.json
│       ├── BakeTrack.csproj
│       └── Program.cs
├── CONTRIBUTING.md
├── docker-compose.yml
├── Dockerfile
├── README.md
└── render.yaml
```

## Documentation

- [Contributing Guide](CONTRIBUTING.md) — GitHub workflow, coding guidelines, and EF Core migrations
- [Deployment Guide](docs/deployment.md) — Deploying to Render.com and production environment setup

## Team Quote

> Don't let today's shadow of failure affect tomorrow's light of success.  
> — Celestine Ahfua

> It's a challenge to see the "why" behind the code, not just the "how". — Ederson Villalba
 