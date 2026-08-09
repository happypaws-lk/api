# Task: Implement One-Time Admin Setup Endpoints

## Goal

Add a new `Setup` endpoint group that allows creating the very first admin account in production. This replaces the demo seeder (which only runs in Development). Once an admin exists, these endpoints lock themselves permanently.

Think of how Coolify, Ghost, or WordPress handle first-time setup: the setup is available exactly once, then gone forever.

## New Endpoint Group: `Setup`

Create the standard 3-file structure at `src/HappyPaws.Api/Endpoints/Setup/`:

- `SetupContracts.cs`
- `SetupEndpoints.cs`
- `SetupValidators.cs`

The `MapEndpoints()` auto-discovery will register it under `/api/v1/setup` automatically. Do NOT modify `Program.cs`.

---

## Contracts

### `SetupStatusResponse`

```
IsSetupComplete: bool
```

### `SetupCompleteRequest`

```
Name: string (required, 2-100 chars)
Email: string (required, valid email)
Password: string (required, min 8 chars)
```

### Response on success

Reuse the existing `AuthResponse` from `HappyPaws.Api.Endpoints.Auth.AuthContracts`. It contains `AccessToken`, `RefreshToken`, and `ExpiresAt`.

---

## Endpoints

### `GET /setup/status`

- **Public.** No authentication.
- Check if any user has the `Admin` role: `db.UserRoles.AnyAsync(r => r.Role == Role.Admin)`.
- Return `SetupStatusResponse` with the result.
- Add proper OpenAPI metadata (WithName, WithSummary, WithDescription, Produces).

### `POST /setup/complete`

- **Public.** No authentication (no accounts exist yet, so auth is impossible).
- Apply `ValidationFilter<SetupCompleteRequest>`.
- Apply `.RequireRateLimiting("RegisterLimiter")` (already exists in Program.cs).
- **Security guard (critical):** First thing, check if an admin already exists. If yes, return `409 Conflict`. This is the one-time lock.
- Check if the email is already taken by a completed account. If yes, return `409 Conflict`.
- Create the `User` with `IsVerified = true`, hash the password, assign the `Admin` role, generate tokens (access + refresh), save everything, and return `201 Created` with the `AuthResponse`.
- Add proper OpenAPI metadata.

Follow the exact patterns from `AuthEndpoints.cs`: static async handler methods, `TypedResults`, strongly-typed `Results<T1, T2>` return types, DI through method parameters.

---

## Verification

Run `dotnet build` to confirm compilation. The new endpoints should appear in the OpenAPI spec automatically.
