# Chapar

**A multi-tenant API for managing and dispatching messages and one-time passwords (OTPs).**

[![Build](https://github.com/alireza-rezaee/chapar/actions/workflows/build.yml/badge.svg)](https://github.com/alireza-rezaee/chapar/actions/workflows/build.yml)
[![Release](https://github.com/alireza-rezaee/chapar/actions/workflows/release.yml/badge.svg)](https://github.com/alireza-rezaee/chapar/actions/workflows/release.yml)
[![Docker image tag](https://img.shields.io/github/v/release/alireza-rezaee/chapar?color=blue&logo=docker&label=Docker)](https://github.com/alireza-rezaee/chapar/pkgs/container/chapar-api)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)

## Overview

Chapar is an ASP.NET Core Web API for multi-tenant message delivery and OTP verification. It provides endpoints for tenants, tenant API keys, message templates, email and SMS sender configurations, message requests, and OTP challenges.

The API stores application data in SQL Server with Entity Framework Core, uses Redis for second-level database caching, and publishes message and OTP work through RabbitMQ with MassTransit consumers. Workers dispatch messages through configured email (SMTP) and SMS (Melipayamak) channels. This separates API requests from delivery work and provides a shared service for applications that need tenant-aware messaging and verification.

### Table of Contents

- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Local Development](#local-development)
- [Configuration](#configuration)
- [Environment Variables](#environment-variables)
- [Docker Instructions](#docker-instructions)
- [CI/CD Pipeline](#cicd-pipeline)
- [Running Tests](#running-tests)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [License](#license)

## Key Features

- Tenant-oriented management of tenants, API keys, sender configurations, and templates.
- Email and SMS message request endpoints, with asynchronous dispatch through RabbitMQ.
- OTP challenge creation and code verification, with tenant-scoped persistence and configurable sender channels.
- Template rendering with Scriban.
- SQL Server persistence through Entity Framework Core and Redis-backed second-level query caching.
- OpenAPI and Scalar API reference in the Development environment.

## Tech Stack

- **.NET 10 / ASP.NET Core** — Web API runtime and controllers.
- **Entity Framework Core 10** and **SQL Server** — data access and persistence.
- **EFCoreSecondLevelCacheInterceptor** with **StackExchange.Redis** — query caching.
- **MassTransit** with **RabbitMQ** — asynchronous message and OTP processing.
- **MailKit** — SMTP email delivery.
- **Melipayamak** — SMS delivery integration.
- **Scriban** — message template rendering.
- **Docker** — multi-stage Linux container build.
- **GitHub Actions** — CI, versioning, releases, and GHCR image publishing.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- Docker Engine or Docker Desktop for container builds and runs.
- For local API development, reachable **SQL Server**, **Redis**, and **RabbitMQ** services. The repository also provides a Docker Compose stack for these dependencies and the API.
- SMTP and/or Melipayamak sender credentials configured for the tenants that will send messages.

## Local Development

From the repository root:

```bash
dotnet restore Chapar.slnx
dotnet build Chapar.slnx --configuration Release --no-restore
```

### Store development secrets with `dotnet user-secrets`

Do not put passwords, connection strings, API keys, or other sensitive values in source-controlled `appsettings.json` files. Initialize user-secrets for the API project:

```bash
dotnet user-secrets init --project src/Chapar.API/Chapar.API.csproj
```

The project already has a `UserSecretsId` in its project file, so it is initialized; the command is included for reference if setting up a project without one. Add sensitive settings using the same hierarchical keys used by the application. This API reads `ConnectionStrings:Database` and `ConnectionStrings:Redis` (not `ConnectionStrings:DefaultConnection`) and RabbitMQ settings:

```bash
dotnet user-secrets set "ConnectionStrings:Database" "Server=localhost,1433;Database=Chapar;User Id=sa;Password=<your-password>;TrustServerCertificate=True" --project src/Chapar.API/Chapar.API.csproj
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379" --project src/Chapar.API/Chapar.API.csproj
dotnet user-secrets set "RabbitMQ:Password" "<your-rabbitmq-password>" --project src/Chapar.API/Chapar.API.csproj
```

User Secrets stores these development values outside the source tree, reducing the risk of accidentally committing them or leaking them on GitHub. It is a development convenience, not an encrypted production secret store; use an appropriate secret manager for deployed environments. ASP.NET Core loads User Secrets automatically when the app runs in Development.

Set non-sensitive connection details in environment variables if needed, then run the API using the Development launch profile:

```bash
export RabbitMQ__Host='localhost'
export RabbitMQ__VirtualHost='/'
export RabbitMQ__Username='guest'

dotnet run --project src/Chapar.API/Chapar.API.csproj --launch-profile http
```

The `http` launch profile listens at `http://localhost:5215` and sets the environment to `Development`. In Development, the API reference is available at [http://localhost:5215/scalar/v1](http://localhost:5215/scalar/v1); the OpenAPI document is at `http://localhost:5215/openapi/v1.json`. The API requires valid SQL Server and Redis connection strings and RabbitMQ configuration to start. Delivery also requires valid tenant-level sender configuration and credentials.

## Configuration

The application loads the standard ASP.NET Core `appsettings.json` and environment-specific settings. In Development, ASP.NET Core also loads User Secrets for the project. Environment variables use double underscores (`__`) in place of configuration colons. At startup, the API also adds key-per-file configuration from `/run/secrets`, which is useful for mounted container secrets. The Compose stack uses variables from a local `.env` file to configure its services and pass settings into the API container; User Secrets are for local development and are not automatically passed into Compose containers.

| Configuration key | Environment variable | Required / default | Purpose |
| --- | --- | --- | --- |
| `ConnectionStrings:Database` | `ConnectionStrings__Database` | Required; no default | SQL Server connection string used by Entity Framework Core. |
| `ConnectionStrings:Redis` | `ConnectionStrings__Redis` | Required; no default | Redis connection string used by the EF second-level cache. |
| `RabbitMQ:Host` | `RabbitMQ__Host` | Defaults to `localhost` | RabbitMQ host. In a container, `localhost` means the API container itself; set this to a reachable broker hostname. |
| `RabbitMQ:VirtualHost` | `RabbitMQ__VirtualHost` | Defaults to `/` | RabbitMQ virtual host. |
| `RabbitMQ:Username` | `RabbitMQ__Username` | Defaults to `guest` | RabbitMQ username. |
| `RabbitMQ:Password` | `RabbitMQ__Password` | Defaults to `guest` | RabbitMQ password. Use a secret in non-development environments. |
| `Logging:LogLevel:Default` | `Logging__LogLevel__Default` | Defaults to `Information` in `appsettings.json` | Minimum default application log level. |
| `Logging:LogLevel:Microsoft.AspNetCore` | `Logging__LogLevel__Microsoft.AspNetCore` | Defaults to `Warning` in `appsettings.json` | Minimum log level for ASP.NET Core categories. |
| `AllowedHosts` | `AllowedHosts` | Defaults to `*` in `appsettings.json` | ASP.NET Core allowed-host filtering. |
| `OTP:HashingKey` | `OTP__HashingKey` | Present in `appsettings.json` with an empty value | Sample OTP setting. **Current startup does not bind this section to `OTPOptions`; setting this key alone does not configure the options consumed by the OTP controller.** |

SMTP and SMS sender settings are stored in tenant sender configurations and are not application-wide environment variables. The code defines SMTP fields such as host, port, username, password, sender address/name, and socket option, and SMS fields such as sender number and provider credentials. Configure these through the API for the relevant tenant.

For Docker secrets, mount files under `/run/secrets`; the key-per-file provider loads their names and contents as configuration. Do not commit real credentials to `appsettings.json`, source control, or this README.

## Environment Variables

The repository's [`.env.example`](.env.example) lists the variables consumed by [`docker-compose.yml`](docker-compose.yml). Copy it to `.env` in the repository root and replace the placeholder credentials before starting the stack:

```bash
cp .env.example .env
```

Docker Compose reads `.env` for `${VARIABLE}` interpolation and injects the resulting values into the configured service environment. `.env.example` is a template; Compose does not automatically load that filename. The repository's `.gitignore` excludes `.env` through its `*.env` pattern. Never commit a real `.env` file, and replace the example RabbitMQ `guest` credentials and all placeholder secrets before using the stack beyond local development.

| Variable | Description | Default in `.env.example` |
| --- | --- | --- |
| `MSSQL_SA_PASSWORD` | SQL Server `sa` account password. Set a strong password that satisfies SQL Server's password policy. | `REPLACE_YOUR_PASSWORD_HERE` (placeholder; replace before use) |
| `MSSQL_PID` | SQL Server edition/product ID passed to the SQL Server container. | `Evaluation` |
| `MSSQL_DATABASE` | Database name used in the API's SQL Server connection string. | `chapar` |
| `ACCEPT_EULA` | Accepts the Microsoft SQL Server license terms. Compose requires this value to be set. | `Y` |
| `REDIS_PORT` | Redis port used by the API's Redis connection string. | `6379` |
| `REDIS_ALLOW_ADMIN` | Redis client `allowAdmin` connection option passed by the API connection string. | `true` |
| `REDIS_CONNECT_TIMEOUT` | Redis client connection timeout, in milliseconds. | `10000` |
| `RABBITMQ_USER` | RabbitMQ default user and API connection username. | `guest` (change for non-local use) |
| `RABBITMQ_PASS` | RabbitMQ default password and API connection password. | `guest` (change for non-local use) |
| `OTP_HASHING_KEY` | Hashing key passed to the API as `OTP__HashingKey`. Use a long, unpredictable secret. | `REPLACE_YOUR_LONG_LENGTH_KEY_HERE` (placeholder; replace before use) |
| `OTP_MaxAttempts` | Maximum OTP verification attempts passed as `OTP__MaxAttempts`. | `5` |

The Compose file sets `ASPNETCORE_ENVIRONMENT=Production`; development-only Scalar and OpenAPI endpoints are therefore not enabled in the Compose API container. Note that current application startup does not bind the `OTP` section to `OTPOptions`; passing the OTP variables through Compose does not by itself configure the options consumed by the OTP controller.

## Docker Instructions

Build from the API directory because the Dockerfile expects the project file at the build-context root:

```bash
docker build -t chapar-api:local ./src/Chapar.API
```

The image exposes **HTTP port 8080** (`ASPNETCORE_URLS=http://+:8080`) and runs as the non-root `app` user. Map this port to a host port and provide connection details for services reachable from the container. This example connects to services published on the Docker host; change credentials and port numbers to match your environment:

```bash
docker run --rm --name chapar-api \
  -p 8080:8080 \
  --add-host=host.docker.internal:host-gateway \
  -e ConnectionStrings__Database='Server=host.docker.internal,1433;Database=Chapar;User Id=sa;Password=Change_this_password_123!;TrustServerCertificate=True' \
  -e ConnectionStrings__Redis='host.docker.internal:6379' \
  -e RabbitMQ__Host='host.docker.internal' \
  -e RabbitMQ__VirtualHost='/' \
  -e RabbitMQ__Username='guest' \
  -e RabbitMQ__Password='guest' \
  chapar-api:local
```

`--add-host` provides the `host.docker.internal` mapping on Docker Engine for Linux. Ensure SQL Server, Redis, and RabbitMQ accept connections from the container. Alternatively, put the API and its dependencies on the same Docker network and use the dependency container/service names in the connection settings. Do not expose development credentials or unauthenticated dependencies to untrusted networks.

The published image is hosted on GitHub Container Registry:

```bash
docker pull ghcr.io/alireza-rezaee/chapar-api:<version>
```

Replace `<version>` with a tag listed on the [GHCR package page](https://github.com/alireza-rezaee/chapar/pkgs/container/chapar-api). The release workflow publishes semantic-version tags; it does not publish a `latest` tag.

### Docker Compose (Recommended)

The Compose file defines four services: `api`, `mssql`, `redis`, and `rabbitmq`. After creating and editing the root `.env` file as described in [Environment Variables](#environment-variables), start the stack with:

```bash
docker compose up -d
```

Published host ports are:

| Service | Host port(s) | Purpose |
| --- | --- | --- |
| `api` | `5000` (container port `8080`) | Chapar HTTP API |
| `mssql` | `1433` | SQL Server |
| `redis` | `6379` | Redis |
| `rabbitmq` | `5672`, `15672` | RabbitMQ messaging; management UI on `15672` |

The stack persists service data in these named volumes:

| Volume | Container mount | Service |
| --- | --- | --- |
| `mssql_data` | `/var/lib/mssql` | `mssql` |
| `redis_data` | `/data` | `redis` |
| `rabbitmq_data` | `/var/lib/rabbitmq` | `rabbitmq` |

All services join the `chapar` bridge network. The API waits for the SQL Server, Redis, and RabbitMQ health checks before starting. View status and logs with `docker compose ps` and `docker compose logs -f api`; stop the stack with `docker compose down`. The named volumes are retained when bringing the stack down.

**Before running:** the current Compose file's API build context is `src/AlirezaRezaee.Chapar.API`, but the project and Dockerfile in this repository are located at `src/Chapar.API`. Update the `build.context` in `docker-compose.yml` to `src/Chapar.API` before running `docker compose up -d`; otherwise Docker Compose cannot find the API build context. The standalone `docker build` command above already uses the existing project path.

## CI/CD Pipeline

The repository has two GitHub Actions workflows:

- **Build** ([`build.yml`](.github/workflows/build.yml)) runs on pushes and pull requests affecting the workflow, solution, or `src/**`. It calculates a semantic version with the local versioner action, sets up .NET 10, restores and builds the solution in Release configuration, and runs `dotnet test`. Its Docker job builds the image with version build arguments but does **not** push it.
- **Release** ([`release.yml`](.github/workflows/release.yml)) runs on matching pushes to `main` and `dev`. It waits for the build checks to succeed, calculates a semantic version, creates or updates the corresponding Git tag, and creates a GitHub Release containing Linux x64 publish archives (regular and debug). Its Docker job logs in to **GitHub Container Registry (GHCR)** and builds and pushes `ghcr.io/alireza-rezaee/chapar-api:<version>`. The versioner marks non-`main`/`master` branches as beta prereleases.

The workflows publish container images to **GHCR only**. No Docker Hub credentials or Docker Hub publishing step are configured. Release and image publication require the repository's GitHub Actions permissions and package settings to allow access to GHCR.

## Running Tests

Run the solution's tests with:

```bash
dotnet test Chapar.slnx
```

There is currently no test project tracked in the solution, so this command does not execute application test cases. Add a test project to `Chapar.slnx` as tests are introduced.

## Project Structure

```text
.
├── Chapar.slnx
├── LICENSE
├── README.md
├── .github/
│   ├── actions/versioner/       # Semantic-version calculation action
│   └── workflows/               # Build/test and release workflows
└── src/
    └── Chapar.API/
        ├── Controllers/         # Tenant, keys, templates, messages, OTP, sender config
        ├── Data/                # EF Core context and interceptors
        ├── DTOs/                # API data transfer objects
        ├── Models/              # Persisted domain entities
        ├── Options/             # RabbitMQ, OTP, and sender option types
        ├── Services/            # Delivery, dispatch, template, and repository services
        ├── ViewModels/          # Request/response models
        ├── Dockerfile
        ├── Program.cs           # Dependency injection and HTTP pipeline
        ├── appsettings.json
        └── Chapar.API.csproj
```

## Contributing

Contributions are welcome. Please open an issue to discuss significant changes, then submit a focused pull request with a clear description. Keep changes scoped, avoid including secrets, and run `dotnet restore`, `dotnet build`, and `dotnet test` before submitting. Update this README when setup, configuration, or operational behavior changes.

## License

Chapar is distributed under the [MIT License](./LICENSE).
