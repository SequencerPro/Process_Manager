# Process Manager — Deployment Manual

This document covers all supported deployment scenarios:

1. [Local development](#1-local-development)
2. [On-premises (Docker Compose)](#2-on-premises-docker-compose)
3. [Cloud — Render.com](#3-cloud--rendercom)
4. [Environment variable reference](#4-environment-variable-reference)
5. [First-run behaviour & default data](#5-first-run-behaviour--default-data)
6. [Upgrades & migrations](#6-upgrades--migrations)
7. [Troubleshooting](#7-troubleshooting)

---

## 1. Local Development

### Prerequisites

| Tool | Minimum version |
|------|----------------|
| .NET SDK | 8.0 |
| PostgreSQL | 14+ (or Docker) |

### Steps

1. **Clone the repository**
   ```bash
   git clone <repo-url>
   cd Process_Manager
   ```

2. **Configure the API connection string**

   Edit `src/ProcessManager.Api/appsettings.Development.json` (create it if it does not exist):
   ```json
   {
     "ConnectionStrings": {
       "Default": "Host=localhost;Database=processmanager;Username=postgres;Password=<your-postgres-password>"
     },
     "Jwt": {
       "Key": "any-32-char-or-longer-dev-secret!"
     }
   }
   ```
   Settings in `appsettings.Development.json` override `appsettings.json` in the `Development` environment and are git-ignored.

3. **Configure the Web base URL**

   Edit `src/ProcessManager.Web/appsettings.Development.json`:
   ```json
   {
     "ApiBaseUrl": "https://localhost:7001"
   }
   ```
   Adjust the port to match whatever the API is listening on (check `Properties/launchSettings.json`).

4. **Run the API**
   ```bash
   dotnet run --project src/ProcessManager.Api
   ```
   On first run the application will:
   - Apply all pending EF Core migrations (creating the schema automatically)
   - Seed roles, a default admin account, and sample domain data

5. **Run the Web front-end**
   ```bash
   dotnet run --project src/ProcessManager.Web
   ```

6. **Sign in**
   - URL: `https://localhost:<web-port>`
   - Username: `admin`
   - Password: `Admin1234!` (or whatever you set in `SEED_ADMIN_PASSWORD`)

---

## 2. On-Premises (Docker Compose)

The `docker-compose.yml` file at the repository root starts three containers: PostgreSQL, the API, and the Blazor Web front-end.

### Prerequisites

| Tool | Minimum version |
|------|----------------|
| Docker Engine | 24+ |
| Docker Compose | v2 (the `docker compose` plugin) |

### Steps

1. **Clone the repository onto the host machine**
   ```bash
   git clone <repo-url>
   cd Process_Manager
   ```

2. **Create the environment file**
   ```bash
   cp .env.example .env
   ```
   Open `.env` in a text editor and fill in the two required values:

   | Variable | Description |
   |----------|-------------|
   | `JWT_KEY` | Random secret, 32+ characters. Generate with `openssl rand -base64 32` |
   | `POSTGRES_PASSWORD` | Password for the PostgreSQL database |

   Optional values can be left at their defaults (see [§4](#4-environment-variable-reference)).

   > **Security note:** `.env` is git-ignored. Never commit it to source control.

3. **Build and start all containers**
   ```bash
   docker compose up -d --build
   ```
   Docker will:
   - Build the API and Web images from source (takes 1–3 minutes on first run)
   - Start PostgreSQL and wait until it passes its health-check before starting the API
   - Run database migrations and seed default data automatically on API startup

4. **Verify the services are running**
   ```bash
   docker compose ps
   ```
   All three services should show `running (healthy)` or `running`.

5. **Access the application**

   | Service | URL |
   |---------|-----|
   | Web UI | http://\<host\>:5000 |
   | API | http://\<host\>:10000 |
   | Swagger | http://\<host\>:10000/swagger |

6. **Sign in**
   - Username: `admin`
   - Password: value of `SEED_ADMIN_PASSWORD` in `.env` (default: `Admin1234!`)

### Useful commands

```bash
# View logs for all services
docker compose logs -f

# View logs for only the API
docker compose logs -f api

# Stop everything (data is preserved in the pgdata volume)
docker compose down

# Stop and delete all data (irreversible)
docker compose down -v

# Rebuild images after a code change
docker compose up -d --build
```

### Exposing the application publicly

By default the ports are only bound to `localhost`. To serve the application on a public IP or domain, place a reverse proxy (e.g. nginx or Caddy) in front of the containers and terminate TLS there. Do not expose port 5432 (PostgreSQL) to the public network.

---

## 3. Cloud — Render.com

The repository includes a `render.yaml` Blueprint that provisions all required services automatically.

### Prerequisites

- A [Render.com](https://render.com) account
- The repository hosted on GitHub or GitLab

### First-time deployment

1. **Connect the repository**
   In the Render dashboard, click **New → Blueprint** and select the repository. Render reads `render.yaml` and proposes the three services defined there (API, Web, PostgreSQL).

2. **Set secret environment variables**
   The following variables have `sync: false` in `render.yaml`, meaning Render will prompt you to supply them before the first deploy:

   | Variable | Where to set | Description |
   |----------|--------------|-------------|
   | `Jwt__Key` | API service → Environment | Random secret, 32+ characters |
   | `Swagger__Password` | API service → Environment | Password for `/swagger` basic-auth. Leave blank to disable. |
   | `ApiBaseUrl` | Web service → Environment | Public URL of the API service, e.g. `https://process-manager-api.onrender.com` |

   `ConnectionStrings__Default` is injected automatically by Render from the linked PostgreSQL database — you do not need to set it.

3. **Deploy**
   Click **Apply**. Render builds both Docker images and deploys them. The first build typically takes 3–5 minutes.

4. **Sign in**
   Navigate to the Web service URL (shown in the Render dashboard).
   - Username: `admin`
   - Password: `Admin1234!`

   Change this password immediately after your first login.

### Subsequent deployments

Render auto-deploys on every push to the `master` branch (configured in `render.yaml`). To trigger a manual deploy, click **Manual Deploy → Deploy latest commit** in the Render dashboard.

### Costs (as of render.yaml configuration)

| Service | Plan | Approximate monthly cost |
|---------|------|--------------------------|
| API | Starter | ~$7 |
| Web | Starter | ~$7 |
| PostgreSQL | Basic 1 GB | ~$20 |
| **Total** | | **~$34/month** |

Plans can be changed in `render.yaml` or in the Render dashboard.

---

## 4. Environment Variable Reference

All variables use `__` as the section separator for ASP.NET Core configuration (e.g. `Jwt__Key` sets `Jwt:Key`).

### API service

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ConnectionStrings__Default` | Yes | — | PostgreSQL connection string (key-value Npgsql format or `postgresql://` URL) |
| `Jwt__Key` | Yes | — | HMAC-SHA256 signing secret for JWT tokens. Minimum 32 characters. |
| `Jwt__Issuer` | No | `ProcessManager.Api` | JWT `iss` claim |
| `Jwt__Audience` | No | `ProcessManager` | JWT `aud` claim |
| `Jwt__ExpiryMinutes` | No | `480` | Token lifetime in minutes |
| `Swagger__Password` | No | *(blank — disabled)* | Password for the `/swagger` basic-auth gate. Username is always `swagger`. |
| `SeedAdminPassword` | No | `Admin1234!` | Password set on the `admin` account during first-run seeding. Has no effect after users exist. |
| `ASPNETCORE_ENVIRONMENT` | No | `Production` | Set to `Development` for verbose logging and developer exception pages |

### Web service

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ApiBaseUrl` | Yes | — | Base URL of the API service reachable from the web server (not the browser) |
| `ASPNETCORE_ENVIRONMENT` | No | `Production` | Same as API |
| `PORT` | No | `10000` | Port the Kestrel server listens on inside the container |

### Docker Compose `.env` variables

These are expanded into the variables above by `docker-compose.yml`.

| Variable | Required | Default | Maps to |
|----------|----------|---------|---------|
| `JWT_KEY` | Yes | — | `Jwt__Key` |
| `POSTGRES_PASSWORD` | Yes | — | PostgreSQL + `ConnectionStrings__Default` |
| `SWAGGER_PASSWORD` | No | *(blank)* | `Swagger__Password` |
| `SEED_ADMIN_PASSWORD` | No | `Admin1234!` | `SeedAdminPassword` |
| `JWT_EXPIRY_MINUTES` | No | `480` | `Jwt__ExpiryMinutes` |

---

## 5. First-Run Behaviour & Default Data

On every startup, the API:

1. **Runs EF Core migrations** — creates the database schema if it does not exist, or applies any pending migrations. Safe to run on an already-populated database.
2. **Seeds roles** — creates `Admin` and `Engineer` roles if they do not exist.
3. **Seeds the admin account** — creates the `admin` user only if the database contains no users at all. Uses `SeedAdminPassword` (default: `Admin1234!`).
4. **Seeds demo domain data** — processes, kinds, workflows, and other reference data. Each record is inserted only if its unique code is not already present (idempotent).
5. **Seeds ISO 9001 QMS documents** — 21 document processes each with four structured sections. Idempotent.
6. **Seeds training courses** — 12 onboarding courses each with 3–5 learning modules. Idempotent.

All seeding is idempotent: re-running startup never creates duplicates or overwrites existing data.

> **Change the admin password** after first login in a production environment. You can do this through the application UI or by setting a strong `SEED_ADMIN_PASSWORD` before first startup.

---

## 6. Upgrades & Migrations

### Docker Compose

```bash
# Pull the latest code
git pull

# Rebuild images and restart containers
docker compose up -d --build
```

Migrations run automatically on API startup. No manual `dotnet ef` commands are needed.

### Render.com

Push to the `master` branch. Render rebuilds and redeploys automatically. The new API container applies any pending migrations before accepting traffic.

### Adding migrations during development

When you change EF Core entities, generate a migration locally:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/ProcessManager.Api \
  --startup-project src/ProcessManager.Api
```

Commit the generated files. They will be applied automatically on the next deployment.

---

## 7. Troubleshooting

### API container exits immediately

Check the logs:
```bash
docker compose logs api
```

Common causes:
- **`JWT Key not configured`** — `JWT_KEY` is missing or too short in `.env`.
- **`Connection string 'Default' not configured`** — `POSTGRES_PASSWORD` is missing or the postgres container is not ready. The compose file includes a health-check dependency, so this usually means PostgreSQL failed to start.
- **Migration error** — A previous migration failed partway through. Connect to the database directly and inspect the `__EFMigrationsHistory` table.

### Cannot connect to PostgreSQL

Verify the postgres container is healthy:
```bash
docker compose ps postgres
```

If the status shows `unhealthy`, check its logs:
```bash
docker compose logs postgres
```

### Web UI shows "Unable to reach API"

Ensure the `ApiBaseUrl` environment variable points to the correct address:
- **Docker Compose:** should be `http://api:10000` (the internal Docker network name)
- **Render.com:** should be the public HTTPS URL of the API service (e.g. `https://process-manager-api.onrender.com`)

### Swagger returns 401 Unauthorized

Basic-auth is enabled. The username is `swagger` and the password is the value of `Swagger__Password`. If `Swagger__Password` is blank, no authentication is required.

### Resetting the database (on-premises only)

> **This destroys all data.** Only do this on a non-production instance.

```bash
docker compose down -v   # removes pgdata volume
docker compose up -d     # recreates + re-seeds from scratch
```
