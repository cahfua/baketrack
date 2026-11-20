# Deploying BakeTrack to Render.com

BakeTrack includes a [`Dockerfile`](../Dockerfile) and a [`render.yaml`](../render.yaml) Blueprint for deploying to Render.com.

## Blueprint Deployment

1. Push your branch/repository to GitHub.
2. In Render, select **New + -> Blueprint** and choose this repository (or provision a **PostgreSQL** database and a **Docker Web Service** manually).
3. Configure the required environment variables (see below).
4. Render will build the Docker image, monitor `/health`, apply pending EF Core migrations automatically on startup, and seed the initial `Admin` account.

## Production Environment Variables

In `Production`, demo `Staff` accounts are never seeded and no default admin password is used. Set these environment variables in Render before first launch:

| Variable | Required | Description |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Yes | Set to `Production` (configured in `render.yaml`). |
| `DATABASE_URL` | Yes | PostgreSQL connection string linked from the Render database (`baketrack-db`). |
| `ADMIN_EMAIL` | Yes | Email for the initial `Admin` user. |
| `ADMIN_PASSWORD` | Yes | Password for the initial `Admin` user (min. 8 chars, upper, lower, digit). |
| `ADMIN_FULL_NAME` | No | Optional display name for the initial `Admin` user. |

## Dev / Prod Database Isolation

In `Development` mode, `DATABASE_URL` is ignored and Render hostnames are blocked by `DatabaseConfiguration` so local development never connects to the production database.
