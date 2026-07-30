<a href="https://github.com/happypaws-lk/happypaws-api" align="center">
    <img src=".github/assets/banner.jpg" alt="HappyPaws API">
</a>

<p align="center">The central backend service and API for HappyPaws.lk.</p>
  
<!-- Badges -->
<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat&logo=dotnet&labelColor=171717" alt=".NET 10.0" />
  <img src="https://img.shields.io/badge/C%23-239120?style=flat&logo=c-sharp&labelColor=171717" alt="C#" />
  <img src="https://img.shields.io/badge/PostgreSQL-16+-4169E1?style=flat&logo=postgresql&labelColor=171717" alt="PostgreSQL" />
  <img src="https://img.shields.io/badge/EF_Core-8+-512BD4?style=flat&logo=nuget&labelColor=171717" alt="EF Core" />
  <img src="https://img.shields.io/badge/SignalR-Realtime-000000?style=flat&labelColor=171717" alt="SignalR" />
  <img src="https://img.shields.io/badge/Docker-Enabled-2496ED?style=flat&logo=docker&labelColor=171717" alt="Docker" />
  <img src="https://img.shields.io/badge/License-Proprietary-c03dfe?style=flat&labelColor=171717" alt="License" />
</p>

<h4 align="center">
    <a href="#introduction">Introduction</a> 
    <span> · </span>    
    <a href="#getting-started">Getting started</a>
    <span> · </span>
    <a href="#architecture">Architecture</a>
    <span> · </span>
    <a href="#scaling-strategy">Scaling strategy</a>
    <span> · </span>
    <a href="#secret-management">Secret management</a>
    <span> · </span>
    <a href="#deployment">Deployment</a>
</h4>

<br />

## Introduction

HappyPaws.lk is a verified-identity and reputation-led platform for animal rescue and rehoming in Sri Lanka. This repository contains the **HappyPaws API**, the central backend service that handles business logic, real-time communication, spatial queries, and external integrations for all HappyPaws client applications (Admin Dashboard, Android App, and Web).

It provides secure RESTful endpoints built with ASP.NET Core Minimal APIs, real-time messaging via SignalR, AI urgency triage via Gemini Vision, and geographic capabilities using PostgreSQL with PostGIS.

## Tech stack

- **[.NET 10.0](https://dotnet.microsoft.com/)** & **[C#](https://learn.microsoft.com/en-us/dotnet/csharp/)**
- **[ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)** (HTTP endpoints)
- **[Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)** (ORM)
- **[PostgreSQL](https://www.postgresql.org/) & [PostGIS](https://postgis.net/)** (Relational Database & Spatial Queries)
- **[SignalR](https://dotnet.microsoft.com/en-us/apps/aspnet/signalr)** (Real-time WebSockets)
- **[Google Gemini API](https://ai.google.dev/)** (AI Triage & Urgency Classification)
- **[Firebase Cloud Messaging](https://firebase.google.com/docs/cloud-messaging)** (Push Notifications)
- **[AWS SES](https://aws.amazon.com/ses/)** (Transactional Emails)
- **[Cloudflare R2](https://www.cloudflare.com/developer-platform/r2/)** (S3-compatible Object Storage for KYC & Media)

## Getting started

Follow these steps to set up the API locally.

1. **Clone the repository:**
   ```bash
   git clone https://github.com/happypaws-lk/happypaws-api.git
   cd happypaws-api
   ```

2. **Start the database:**
   Ensure Docker is installed and running, then spin up the PostgreSQL container (with PostGIS enabled):
   ```bash
   docker-compose up -d
   ```

3. **Configure user secrets:**
   Local development secrets must be managed using the .NET Secret Manager.

   First, generate a secure JWT signing key (must be at least 256 bits / 32 bytes):
   ```bash
   # PowerShell
   [Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Max 256) }))

   # bash / openssl
   openssl rand -base64 32
   ```

   Then set the secrets (the connection string below matches the default `docker-compose.yml`):
   ```bash
   cd src/HappyPaws.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=happypaws;Username=happypaws;Password=happypaws_dev"
   dotnet user-secrets set "Jwt:Key" "<paste-generated-key-here>"
   cd ../..
   ```

4. **Apply database migrations:**
   ```bash
   dotnet ef database update --project src/HappyPaws.Infrastructure --startup-project src/HappyPaws.Api
   ```

5. **Start the development server:**
   ```bash
   dotnet run --project src/HappyPaws.Api
   ```
   The API will be available at `http://localhost:5047`. You can access the OpenAPI documentation at `http://localhost:5047/openapi/v1.json`.

6. **Local seed data:**
   On first startup in the `Development` environment, the API automatically seeds demo accounts. No manual step is required.

   | Role | Email | Password |
   |---|---|---|
   | Admin | `admin@happypaws.lk` | `Admin@123` |
   | Veterinarian | `vet@happypaws.lk` | `Vet@123` |
   | Rescuer | `rescuer@happypaws.lk` | `Rescuer@123` |

   **Re-seeding:** The seeder is skipped if any users already exist. To reset and re-seed, clear the relevant rows and restart the server:
   ```bash
   docker exec -it happypaws-db psql -U happypaws -d happypaws \
     -c "DELETE FROM user_roles; DELETE FROM users;"
   ```
   Then restart the API — seed data will be recreated on startup.

## Architecture

The API follows a Clean Architecture approach with three main layers:

- **HappyPaws.Api**: The presentation layer. Contains Minimal API endpoint groupings, SignalR hubs, middleware, and dependency injection setup.
- **HappyPaws.Core**: The domain layer. Holds domain entities, interfaces, enums, and core business rules. This layer has zero external dependencies.
- **HappyPaws.Infrastructure**: The data and integration layer. Implements data access using EF Core, repository patterns, and external services (Gemini, AWS SES, Cloudflare R2, FCM).

## Scaling strategy

Our rate limiting and output caching approach is designed to evolve gracefully with our infrastructure scale, leveraging ASP.NET Core's provider abstractions.

**Current State (In-Memory)**
- **Configuration:** We currently rely on ASP.NET Core's built-in in-memory providers (`Microsoft.AspNetCore.RateLimiting` and `Microsoft.AspNetCore.OutputCaching`).
- **Why it fits:** For our current single-node deployment (and local Docker Compose environment), in-memory execution is exceptionally fast, simple to maintain, and requires no external infrastructure.
- **Constraints:** If scaled across multiple servers, limits would be per-instance (e.g., 5 requests/min per server, not globally) and cached data would be duplicated in RAM across instances.

**Future State (Redis Centralization)**
- **The trigger:** Once we deploy multiple instances horizontally behind a load balancer without sticky sessions.
- **The migration:** We will introduce a Redis cluster (e.g., AWS ElastiCache) and swap the in-memory providers for `Microsoft.AspNetCore.RateLimiting.Redis` and `Microsoft.Extensions.Caching.StackExchangeRedis`.
- **The benefit:** This enforces strict global rate limits across all nodes and offloads memory pressure from the API instances to a centralized, persistent cache. Because of ASP.NET Core's abstractions, this migration requires only connection string updates and minimal middleware configuration changes in `Program.cs`, leaving the endpoint business logic completely untouched.

## Secret management

Local development secrets must be managed using the .NET Secret Manager. Do not commit secrets to source control. For production, set these as environment variables (replace `:` with `__`, e.g. `Jwt:Key` → `Jwt__Key`). See `.env.example` for the full production reference.

### Development quick-start (minimum required locally)

When `ASPNETCORE_ENVIRONMENT=Development`, file storage (R2), email (SES), and push notifications (Firebase) are replaced by local no-op stubs. You only need two secrets to start the API:

```bash
cd src/HappyPaws.Api
dotnet user-secrets init

# 1. Database — matches the default docker-compose.yml
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=happypaws;Username=happypaws;Password=happypaws_dev"

# 2. JWT signing key (must be at least 32 bytes / 256 bits)
#    Generate one:  openssl rand -base64 32
dotnet user-secrets set "Jwt:Key" "<paste-generated-key-here>"
```

### Complete secrets reference

The table below covers every configuration key the API reads. Set them via `dotnet user-secrets set "<Key>" "<value>"` locally, or as environment variables in production.

| Key | Required | Notes |
|-----|----------|-------|
| `ConnectionStrings:DefaultConnection` | Yes | PostgreSQL DSN: `Host=...;Port=5432;Database=happypaws;Username=...;Password=...` |
| `Jwt:Key` | Yes | HMAC-SHA256 signing key. Min 32 bytes. Generate: `openssl rand -base64 32` |
| `Jwt:Issuer` | No | Defaults to `https://happypaws.lk` |
| `Jwt:Audience` | No | Defaults to `https://happypaws.lk` |
| `Gemini:ApiKey` | Yes (for rescue triage) | Google AI API key from [aistudio.google.com](https://aistudio.google.com/app/apikey). Falls back to rule-based classifier if absent, but the classifier still throws — omit only if you will not test rescue photo upload. |
| `Firebase:ServiceAccountJson` | Production only | Base64-encoded service account JSON. Firebase Console → Project Settings → Service accounts → Generate new private key. Encode: `openssl base64 -in key.json \| tr -d '\n'` |
| `Storage:AccountId` | Production only | Cloudflare account ID (right sidebar of the Cloudflare dashboard) |
| `Storage:AccessKey` | Production only | R2 API token access key. R2 → Manage R2 API tokens → Create API token |
| `Storage:SecretKey` | Production only | R2 API token secret key |
| `Storage:PublicBucket` | No | Defaults to `happypaws-public` |
| `Storage:PrivateBucket` | No | Defaults to `happypaws-private` |
| `Storage:CustomDomain` | No | Defaults to `cdn.happypaws.lk` |
| `Ses:AccessKey` | Production only | AWS IAM access key ID. User needs `ses:SendEmail` permission. |
| `Ses:SecretKey` | Production only | AWS IAM secret access key |
| `Ses:Region` | No | Defaults to `us-east-1`. Use `ap-south-1` for lower latency from Sri Lanka. |
| `Ses:FromAddress` | No | Defaults to `noreply@happypaws.lk`. Must be a verified SES identity. |
| `Cors:AllowedOrigins` | Production only | Array of allowed origins. Empty = all browser requests blocked. Set as `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc. |
| `RateLimiting:Disabled` | No | Set to `true` to bypass rate limits during automated testing. Defaults to `false`. |

## Deployment

The API is containerized using a multi-stage Dockerfile and deployed to AWS (Elastic Beanstalk / EC2) with an Amazon RDS PostgreSQL database.
A GitHub Actions CI/CD pipeline automatically builds, tests, and deploys the application upon pushes to the `main` branch.

## Contact

You can reach out to the development team if you have any questions regarding the backend platform or integration.
