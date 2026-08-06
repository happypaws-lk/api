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

## Introduction

HappyPaws.lk is a verified-identity and reputation-led platform for animal rescue and rehoming in Sri Lanka. This repository contains the HappyPaws API. It handles business logic, real-time communication, spatial queries, and external integrations for all HappyPaws client applications (Admin Dashboard, Android App, and Web).

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

1. **Clone the repository**
   ```bash
   git clone https://github.com/happypaws-lk/happypaws-api.git
   cd happypaws-api
   ```

2. **Start the database and local storage**
   Ensure Docker is installed and running. This starts PostgreSQL (with PostGIS) and MinIO (local S3-compatible storage). A one-time init container creates both buckets automatically on first run.
   ```bash
   docker-compose up -d
   ```
   The MinIO web console is available at `http://localhost:9001` (username: `minioadmin`, password: `minioadmin`).

3. **Configure user secrets**
   Local development secrets must be managed using the .NET Secret Manager. Generate a secure JWT signing key (must be at least 256 bits / 32 bytes).
   ```bash
   # PowerShell
   [Convert]::ToBase64String((1..32 | ForEach-Object { [byte](Get-Random -Max 256) }))

   # bash / openssl
   openssl rand -base64 32
   ```

   Set the minimum required secrets to start the API. The connection string below matches the default `docker-compose.yml`.
   ```bash
   cd src/HappyPaws.Api
   dotnet user-secrets init
   dotnet user-secrets set "DB_HOST" "localhost"
   dotnet user-secrets set "DB_PORT" "5432"
   dotnet user-secrets set "DB_NAME" "happypaws"
   dotnet user-secrets set "DB_USER" "happypaws"
   dotnet user-secrets set "DB_PASSWORD" "happypaws_dev"
   dotnet user-secrets set "Jwt:Key" "<paste-generated-key-here>"
   cd ../..
   ```

4. **Apply database migrations**
   ```bash
   dotnet ef database update --project src/HappyPaws.Infrastructure --startup-project src/HappyPaws.Api
   ```

5. **Start the development server**
   ```bash
   dotnet run --project src/HappyPaws.Api
   ```
   The API will be available at `http://localhost:5047`.

6. **Local seed data**
   On first startup in the `Development` environment, the API automatically seeds demo accounts. No manual step is required.

   | Role | Email | Password |
   |---|---|---|
   | Admin | `admin@happypaws.lk` | `Admin@123` |
   | Veterinarian | `vet@happypaws.lk` | `Vet@123` |
   | Rescuer | `rescuer@happypaws.lk` | `Rescuer@123` |

   **Re-seeding:** The seeder is skipped if any users already exist. To reset and re-seed, clear the relevant rows and restart the server.
   ```bash
   docker exec -it happypaws-db psql -U happypaws -d happypaws \
     -c "DELETE FROM user_roles; DELETE FROM users;"
   ```
   Restart the API and seed data will be recreated on startup.

7. **Local file storage (MinIO)**
   MinIO is started automatically by `docker-compose up -d` — no separate step is needed.

   `appsettings.Development.json` already points the API at `localhost:9000`, so no secrets are required for file uploads to work locally.

   The MinIO web console at `http://localhost:9001` shows uploaded files, bucket contents, and object metadata.

   `happypaws-public` allows anonymous read. `happypaws-private` is access-controlled and files are served via short-lived presigned URLs.

## Application environment

The application uses standard .NET configuration providers. Do not commit secrets to source control. 

For production, set these as **Environment Properties** within the AWS Elastic Beanstalk console (Configuration -> Software -> Environment properties). 
- Variables without colons (e.g., `DB_HOST`) are entered exactly as they are.
- Nested JSON configurations use `:` locally, but must be replaced with `__` (double underscore) in Elastic Beanstalk (for example, `Jwt:Key` becomes `Jwt__Key`). See `.env.example` for the full production reference.

When `ASPNETCORE_ENVIRONMENT=Development`, email (SES) and push notifications (Firebase) are replaced by local no-op stubs. File storage is handled by a local MinIO container started via `docker-compose up -d`, which replicates the Cloudflare R2 S3 API locally, including presigned URLs and bucket isolation. No storage credentials are needed for local development.

### Environment Variables Reference


| Key | Required | Notes |
|-----|----------|-------|
| `DB_HOST` | Yes | Database host, e.g. `localhost` |
| `DB_PORT` | Yes | Database port, e.g. `5432` |
| `DB_NAME` | Yes | Database name, e.g. `happypaws` |
| `DB_USER` | Yes | Database user, e.g. `happypaws` |
| `DB_PASSWORD` | Yes | Database password |
| `Jwt__Key` | Yes | HMAC-SHA256 signing key. Min 32 bytes. Generate: `openssl rand -base64 32` |
| `Jwt__Issuer` | No | Defaults to `https://happypaws.lk` |
| `Jwt__Audience` | No | Defaults to `https://happypaws.lk` |
| `Jwt__ExpiryMinutes` | No | Defaults to `15` |
| `Gemini__ApiKey` | Yes (for rescue triage) | Google AI API key from aistudio.google.com. |
| `Gemini__Model` | No | Defaults to `gemini-2.0-flash` |
| `Gemini__TimeoutSeconds` | No | Defaults to `10` |
| `Firebase__ServiceAccountJson` | Production only | Base64-encoded service account JSON. Firebase Console → Project Settings → Service accounts → Generate new private key. Encode: `openssl base64 -in key.json \| tr -d '\n'` |
| `Storage__ServiceUrl` | Local/CI only | MinIO S3 endpoint. e.g. `http://localhost:9000`. Leave unset for Cloudflare R2. |
| `Storage__AccountId` | Production only | Cloudflare account ID (right sidebar of the Cloudflare dashboard). Not required when `Storage__ServiceUrl` is set. |
| `Storage__AccessKey` | Production only | R2 API token key, or MinIO root user for local development. |
| `Storage__SecretKey` | Production only | R2 API token secret, or MinIO root password for local development. |
| `Storage__PublicBucket` | No | Defaults to `happypaws-public` |
| `Storage__PrivateBucket` | No | Defaults to `happypaws-private` |
| `Storage__CustomDomain` | No | Defaults to `cdn.happypaws.lk`. R2 custom domain for public URLs (production only). |
| `Storage__PublicBaseUrl` | No | Override for the public URL base. e.g. `http://localhost:9000/happypaws-public` for MinIO. |
| `Ses__AccessKey` | Production only | AWS IAM access key ID. User needs `ses:SendEmail` permission. |
| `Ses__SecretKey` | Production only | AWS IAM secret access key |
| `Ses__Region` | No | Defaults to `us-east-1`. Use `ap-south-1` for lower latency from Sri Lanka. |
| `Ses__FromAddress` | No | Defaults to `noreply@happypaws.lk`. Must be a verified SES identity. |
| `Cors__AllowedOrigins` | Production only | Array of allowed origins. Empty = all browser requests blocked. Set as `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc. |
| `RateLimiting__Disabled` | No | Set to `true` to bypass rate limits during automated testing. Defaults to `false`. |
| `Features__EnableApiDocs` | No | Set to `true` to enable Swagger UI. Defaults to `false` in production. |

## API documentation

We use Scalar for OpenAPI documentation UI. This provides a modern, interactive way to explore and test the API endpoints directly from the browser.

By default, the documentation UI is enabled in the `Development` environment and disabled in `Production` for security.

To explicitly enable or disable the API documentation, use the `Features__EnableApiDocs` configuration key.

```bash
# Enable the API documentation in production
export Features__EnableApiDocs=true
```

When enabled, you can access the documentation using these endpoints.

*   **OpenAPI Schema:** `http://localhost:5047/openapi/v1.json` (Raw JSON definition)
*   **Scalar UI:** `http://localhost:5047/scalar` (Interactive documentation interface)

## Deployment and CI/CD

The API backend is deployed exclusively on AWS using a containerized multi-stage Dockerfile. Our infrastructure relies on AWS Elastic Beanstalk (SingleInstance environment), Amazon RDS (PostgreSQL 16.x), and Amazon ECR. 

All Infrastructure as Code (IaC) is written in Terraform. Review [infra/README.md](./infra/README.md) for technical infrastructure details, VPC topologies, and provisioning steps.

### GitHub Actions

A GitHub Actions CI/CD pipeline automatically builds, tests, and deploys the application.

*   `ci.yml`: Runs on pull requests to the `main` branch. It provisions a temporary PostGIS database, restores dependencies, builds the project, and runs the test suite.
*   `cd.yml`: Runs on pushes to the `main` branch. It executes the tests, builds the Docker image, pushes it to Amazon ECR, and deploys the new image to Elastic Beanstalk using the `docker-compose.yml` configuration.

### GitHub Secrets setup

To enable the deployment pipeline, you must configure the following repository secrets in GitHub. Go to your repository **Settings** → **Secrets and variables** → **Actions** and create these secrets.

1.  `AWS_ACCESS_KEY_ID`: An AWS IAM access key with permissions to push to ECR and deploy to Elastic Beanstalk.
2.  `AWS_SECRET_ACCESS_KEY`: The corresponding AWS IAM secret key.

No other secrets are required for the pipeline. Elastic Beanstalk securely injects the application environment variables (database credentials, JWT keys, external API keys) into the container at runtime.

## Architecture and scaling

The API follows a Clean Architecture approach with three main layers.

- **HappyPaws.Api**: The presentation layer. Contains Minimal API endpoint groupings, SignalR hubs, middleware, and dependency injection setup.
- **HappyPaws.Core**: The domain layer. Holds domain entities, interfaces, enums, and core business rules. This layer has zero external dependencies.
- **HappyPaws.Infrastructure**: The data and integration layer. Implements data access using EF Core, repository patterns, and external services.

Our rate limiting and output caching approach is designed to evolve gracefully.

**Current State (In-Memory)**
We currently rely on ASP.NET Core's built-in in-memory providers (`Microsoft.AspNetCore.RateLimiting` and `Microsoft.AspNetCore.OutputCaching`). For our current single-node deployment, in-memory execution is exceptionally fast and requires no external infrastructure.

**Future State (Redis Centralization)**
Once we deploy multiple instances horizontally behind a load balancer, we will introduce a Redis cluster (e.g. AWS ElastiCache) and swap the in-memory providers for Redis equivalents. This enforces strict global rate limits across all nodes and offloads memory pressure from the API instances to a centralized, persistent cache. Because of ASP.NET Core's abstractions, this migration requires only connection string updates and minimal middleware configuration changes in `Program.cs`.
