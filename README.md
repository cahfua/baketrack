# BakeTrack

Bakery Inventory and Production Management System for the CSE 499 team project.

## Project Overview

BakeTrack is a web application designed to help small bakeries manage ingredients, monitor inventory levels, organise products and recipes, and track daily production.

## Core Features

- **Ingredient Management:** Add, edit, and manage bakery ingredients.
- **Stock Level Monitoring:** Track ingredient quantities and identify low-stock items.
- **Product Management:** Manage bakery products and their associated recipes.
- **Daily Production Tracking:** Record daily production and track ingredient usage.

## Planned Enhancements

- Search and filtering.
- Ingredient expiry date monitoring.
- Waste management and reporting.

## Technology Stack

- **Framework:** ASP.NET Core / Blazor (Interactive Server)
- **Language:** C# (.NET 10)
- **Database:** PostgreSQL
- **ORM:** Entity Framework Core + Npgsql
- **Frontend:** HTML, CSS, Bootstrap
- **Deployment:** Docker + Render.com
- **Version Control:** Git and GitHub
- **Development Environment:** Visual Studio Code or Visual Studio

## Getting Started

Follow these instructions to set up BakeTrack on your local machine.

### 1. Prerequisites

Install the following software:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Visual Studio Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/downloads)

If using VS Code, install the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit).

### 2. Clone the Repository

Open a terminal and run:

```bash
git clone https://github.com/cahfua/baketrack.git
cd baketrack
```

Open the project in VS Code:

```bash
code .
```

### 3. Navigate to the Application Directory

Run:

```bash
cd src/BakeTrack
```

### 4. Restore Project Dependencies

Restore the required NuGet packages:

```bash
dotnet restore
```

This installs the dependencies defined in `BakeTrack.csproj`, including Entity Framework Core and the Npgsql PostgreSQL provider.

You do not need to install these packages individually.

### 5. Set Up Your Local PostgreSQL Database

BakeTrack uses **PostgreSQL** with Entity Framework Core. Each team member uses their own local PostgreSQL database via **.NET User Secrets**, so personal credentials are never committed to GitHub and local development never points to the production Render database.

Choose **one** of the two local options below:

#### Option A: You already have PostgreSQL installed locally

1. Create an empty local database (for example, `baketrack`):

   ```sql
   CREATE DATABASE baketrack;
   ```

2. Save your personal local connection string in .NET User Secrets (from the repository root or `src/BakeTrack`):

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=baketrack;Username=YOUR_USER;Password=YOUR_PASSWORD" --project src/BakeTrack
   ```

#### Option B: Use the included Docker PostgreSQL container

If you do not have PostgreSQL installed locally, start the development database with Docker:

```bash
docker compose up -d
```

Then save the matching local connection string in .NET User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=baketrack;Username=baketrack;Password=baketrack_dev" --project src/BakeTrack
```

#### Apply Database Migrations

First, install the Entity Framework Core command-line tool if it is not already installed:

```bash
dotnet tool install --global dotnet-ef --version "10.*"
```

If it is already installed, update it when necessary:

```bash
dotnet tool update --global dotnet-ef --version "10.*"
```

Apply the existing migrations to your local PostgreSQL database:

```bash
dotnet ef database update
```

*(Migrations also run automatically when the application starts.)*

**Important:**

- Each developer creates and maintains their own local PostgreSQL database via `dotnet user-secrets`.
- Never commit passwords or connection strings to `appsettings.json` or `appsettings.Development.json`.
- In `Development` mode, `DATABASE_URL` is ignored and Render hostnames are blocked so local runs never connect to production.
- Migration files must be committed to GitHub so all developers and production use the same database structure.
- When new migrations are added by the team, pull the latest changes and run `dotnet ef database update` (or restart the app).

### 6. Build and Run the Application

From the `src/BakeTrack` directory, build the project:

```bash
dotnet build
```

Start the application:

```bash
dotnet run
```

The terminal will display a local URL similar to:

```text
http://localhost:5285
```

Open the URL displayed in your terminal to access BakeTrack.

For automatic rebuilding during development, use:

```bash
dotnet watch run
```

### 7. Deploying to Render.com

The repository includes a `Dockerfile` and a `render.yaml` Blueprint for Render.com:

1. Push your branch/repository to GitHub.
2. In Render, choose **New + -> Blueprint** and select this repository (or create a **PostgreSQL** service and a **Docker Web Service** manually).
3. Link the Render PostgreSQL `DATABASE_URL` environment variable to the web service and set `ASPNETCORE_ENVIRONMENT=Production`.
4. Render will build the Docker container, check `/health`, and apply pending EF Core migrations automatically on startup.

### 8. Database Migrations

BakeTrack uses Entity Framework Core migrations to manage database schema changes.

To check existing migrations:

```bash
dotnet ef migrations list
```

When a database schema change is necessary:

1. Coordinate the change with the team.
2. Update the relevant model classes and database context.
3. Generate a migration:

   ```bash
   dotnet ef migrations add MigrationName
   ```

4. Review the generated migration.
5. Apply the migration to your local database:

   ```bash
   dotnet ef database update
   ```

6. Commit and push the migration files to GitHub through your feature branch.

Other team members should pull the latest changes and run `dotnet ef database update` to apply the new migration to their local databases.

Avoid creating duplicate migrations or changing the database schema without coordinating with the team.

## Project Structure

```text
baketrack/
├── docs/
├── src/
│   └── BakeTrack/
│       ├── Components/
│       │   ├── Layout/
│       │   └── Pages/
│       ├── Data/
│       ├── Migrations/
│       ├── Models/
│       ├── Services/
│       ├── wwwroot/
│       ├── Program.cs
│       ├── appsettings.json
│       └── BakeTrack.csproj
├── .gitignore
└── README.md
```

## GitHub Workflow

To avoid conflicts when multiple people are working on the project, use feature branches rather than committing directly to `main`.

### 1. Create a Feature Branch

Before starting work, update your local `main` branch:

```bash
git switch main
git pull origin main
```

Create a new branch:

```bash
git switch -c feature/your-feature-name
```

For example:

```bash
git switch -c feature/ingredient-management
```

### 2. Make and Commit Changes

After completing your work:

```bash
git add .
git commit -m "Add ingredient management functionality"
```

### 3. Push Changes to GitHub

```bash
git push -u origin feature/ingredient-management
```

Open a pull request on GitHub to merge your changes into `main`.

Another team member should review the changes before merging.

### 4. Keep Your Branch Updated

If other changes have been merged into `main`, update your feature branch:

```bash
git fetch origin
git merge origin/main
```

Resolve any merge conflicts before continuing.

## Development Guidelines

- Follow the existing project structure.
- Keep database models inside `Models/`.
- Place database operations and business logic in `Services/`.
- Keep Blazor pages focused on the user interface.
- Coordinate database schema changes with the rest of the team.
- Verify that `dotnet build` succeeds before creating a pull request.
- Never commit passwords, API keys, or other sensitive information.
- Avoid unnecessary dependencies or major architectural changes without discussing them with the team.

## Team Quote

> Don't let today's shadow of failure affect tomorrow's light of success.  
> — Celestine Ahfua
