# BakeTrack

Bakery inventory and production management system for the CSE 499 team project.

## Project Overview

BakeTrack is a web application designed to help small bakeries manage ingredients, monitor inventory levels, organize products and recipes, and track daily production.

### Core Features

- **Ingredient Management:** Add, edit, and manage bakery ingredients.
- **Stock Level Monitoring:** Track ingredient quantities and identify low-stock items.
- **Product Management:** Manage bakery products and their associated recipes.
- **Daily Production Tracking:** Record daily production and track ingredient usage.

### Planned Enhancements

- Search and filtering
- Ingredient expiration date monitoring
- Waste management and reporting

## Technology Stack

- **Framework:** ASP.NET Core / Blazor
- **Language:** C#
- **Database:** PostgreSQL (hosted on Render)
- **ORM:** Entity Framework Core
- **Frontend:** HTML, CSS, Bootstrap
- **Version Control:** Git and GitHub
- **Development Environment:** Visual Studio Code or Visual Studio
- **Hosting:** Render (planned)

## Getting Started

Follow these instructions to set up BakeTrack on your local machine.

### 1. Prerequisites

Install the following software:

- .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
- Visual Studio Code: https://code.visualstudio.com/
- Git: https://git-scm.com/downloads

If using VS Code, install the C# Dev Kit extension:

https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit

### 2. Clone the Repository

Open a terminal and run:


git clone https://github.com/cahfua/baketrack.git
cd baketrack


Open the project in VS Code:


code .


### 3. Install Project Dependencies

Navigate to the application directory:


cd src/BakeTrack


Restore the required NuGet packages:


dotnet restore


This will automatically install the dependencies defined in `BakeTrack.csproj`, including Entity Framework Core and the PostgreSQL provider.

You do not need to install these packages individually.

### 4. Configure the Database Connection

BakeTrack uses a shared PostgreSQL database hosted on Render.

Contact the project administrator to obtain the development database credentials.

The connection string is stored locally using .NET User Secrets to prevent sensitive information from being committed to GitHub.

Initialize User Secrets:


dotnet user-secrets init


Configure the connection string:
ASK OWEN FOR VALUES FOR THIS STRING!!!

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=YOUR_HOST;Port=5432;Database=YOUR_DATABASE;Username=YOUR_USERNAME;Password=YOUR_PASSWORD;SSL Mode=Require"


Replace the placeholders with the credentials provided by the project administrator.

**Important:**

- Use the Render external database hostname when developing locally.
- Never commit passwords or connection strings containing credentials to GitHub.
- Do not share database credentials in GitHub issues or pull requests.
- Use development credentials rather than production credentials whenever possible.

### 5. Build and Run the Application

From the `src/BakeTrack` directory, build the project:


dotnet build


Start the application:


dotnet run


The terminal will display a local URL similar to:


https://localhost:5285


Open the URL shown in your terminal to access BakeTrack.

For automatic rebuilding during development, use:


dotnet watch run


### 6. Database Migrations

BakeTrack uses Entity Framework Core migrations to manage database schema changes.

Install the EF Core command-line tool if it is not already installed:


dotnet tool install --global dotnet-ef --version "10.*"


To check existing migrations:


dotnet ef migrations list


The initial database migration has already been created and applied to the shared development database.

Team members should not create or apply new migrations unless their changes require modifications to the database schema.

When a schema change is necessary:

1. Coordinate the change with the team.
2. Update the relevant model classes and database context.
3. Generate a migration using `dotnet ef migrations add MigrationName`.
4. Review the migration before applying it.
5. Have the designated team member apply it to the shared database.

The migration files must be committed to GitHub so every developer has the same database schema.

### 7. Project Structure


baketrack/
├── docs/
├── src/
│   └── BakeTrack/
│       ├── Components/
│       │   ├── Layout/
│       │   └── Pages/
│       ├── Data/
│       ├── Models/
│       ├── Services/
│       ├── wwwroot/
│       ├── Program.cs
│       ├── appsettings.json
│       └── BakeTrack.csproj
├── .gitignore
└── README.md


## GitHub Workflow

To avoid conflicts when multiple people are working on the project, use feature branches rather than committing directly to `main`.

### 1. Create a Feature Branch

Before starting work, make sure your local `main` branch is up to date:


git switch main
git pull origin main


Create a new branch:


git switch -c feature/your-feature-name


For example:


git switch -c feature/ingredient-management


### 2. Make and Commit Changes

After completing your work:


git add .
git commit -m "Add ingredient management functionality"


### 3. Push Changes to GitHub


git push -u origin feature/ingredient-management


Open a pull request on GitHub to merge your changes into `main`.

Another team member should review the changes before merging.

### 4. Keep Your Branch Updated

If other changes have been merged into `main`, update your feature branch:


git fetch origin
git merge origin/main


Resolve any merge conflicts before continuing.

## Development Guidelines

- Follow the existing project structure.
- Keep database models inside `Models/`.
- Place database operations and business logic in `Services/`.
- Keep Blazor pages focused on the user interface.
- Coordinate database schema changes with the rest of the team.
- Verify that `dotnet build` succeeds before creating a pull request.
- Never commit passwords, API keys, or other sensitive credentials.
- Avoid unnecessary dependencies or major architectural changes without discussing them with the team.

## Team Quotes

> Don't let today's shadow of failure affect tomorrow's light of success.  
> — Celestine Ahfua