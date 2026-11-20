# Contributing to BakeTrack

## GitHub Workflow

Use feature branches rather than committing directly to `main`.

### 1. Create a Feature Branch

```bash
git switch main
git pull origin main
git switch -c feature/your-feature-name
```

Example:

```bash
git switch -c feature/ingredient-management
```

### 2. Make and Commit Changes

```bash
git add .
git commit -m "Add ingredient management functionality"
```

### 3. Push and Open a Pull Request

```bash
git push -u origin feature/your-feature-name
```

Open a pull request on GitHub targeting `main` and have a team member review it before merging.

### 4. Keep Your Branch Updated

```bash
git fetch origin
git merge origin/main
```

Resolve any merge conflicts before continuing.

## Development Guidelines

- Follow the existing project structure inside `src/BakeTrack/`:
  - Keep database and domain models in `models/`.
  - Place database operations and business logic in `Services/`.
  - Keep Blazor pages in `Components/Pages/` focused on the UI.
- Verify that `dotnet build src/BakeTrack` succeeds before opening a pull request.
- Never commit passwords, API keys, or connection strings to `appsettings.json` or `appsettings.Development.json`—use `.NET User Secrets` for local configuration.
- Coordinate database schema changes and major architectural or dependency changes with the team beforehand.

## Database Migrations

BakeTrack uses Entity Framework Core migrations (`src/BakeTrack/Migrations/`). Pending migrations also run automatically when the application starts.

### Install or Update the EF Core CLI Tool

```bash
dotnet tool install --global dotnet-ef --version "10.*"
# Or update if already installed:
dotnet tool update --global dotnet-ef --version "10.*"
```

### Common Commands

Run from the repository root (or omit `--project src/BakeTrack` if running inside `src/BakeTrack`):

```bash
# List existing migrations
dotnet ef migrations list --project src/BakeTrack

# Add a new migration after updating models / BakeryDbContext
dotnet ef migrations add MigrationName --project src/BakeTrack

# Apply migrations to your local PostgreSQL database
dotnet ef database update --project src/BakeTrack
```

### Migration Rules

1. Coordinate schema changes with the team to avoid conflicting or duplicate migrations.
2. Review the generated migration files before committing.
3. Commit and push migration files on your feature branch so all environments stay in sync.
4. After pulling new migrations from teammates, run `dotnet ef database update --project src/BakeTrack` (or restart the app).
