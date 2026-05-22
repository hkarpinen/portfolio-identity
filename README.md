# portfolio-identity

Authentication and user identity service. The single source of truth for who a user is across the entire platform. Every other service validates JWTs issued here — nothing else issues tokens.

Built from scratch on ASP.NET Identity rather than delegating to a third-party auth provider. The goal was to understand every header, claim, and token exchange rather than treat auth as a black box.

## What it does

- **Registration & email confirmation** — sign up with username/email/password; a confirmation link is sent before the account is activated
- **Login** — username + password returns a short-lived JWT access token and a rotating refresh token (stored server-side, invalidated on use)
- **Refresh token rotation** — silent re-auth via `/auth/refresh`; old token is revoked on each rotation; reuse of a revoked token revokes the entire family
- **TOTP two-factor authentication** — RFC 6238 / Google Authenticator compatible; generate a QR code, scan it, confirm with a live code to enable 2FA on the account
- **Password reset** — forgot-password email flow with a time-limited signed token
- **Profile management** — display name, bio, avatar upload (stored locally in dev, swappable for S3 in prod)
- **Notification preferences** — per-user toggle for email/push notification categories
- **Admin controls** — list users, promote/demote roles, suspend accounts
- **Sessions** — list active sessions (device + IP + last seen), revoke individual sessions or all sessions

## Stack

- .NET 8 / ASP.NET Core Web API
- ASP.NET Identity (EF Core + PostgreSQL 17)
- JWT access tokens + rotating refresh tokens
- TOTP 2FA (RFC 6238)
- RBAC (User, Moderator, Admin roles)
- Clean Architecture: Domain → Application → Infrastructure → Client

## Running locally

```bash
# From repo root — requires postgres + rabbitmq + mailpit (see infra/)
dotnet run --project src/Client
```

Or via the full stack:

```bash
docker compose -f infra/compose.dev.yaml up identity
```

Mailpit (dev SMTP) runs at `http://localhost:8025` — all outbound email is captured there.

## Structure

```
src/
  Domain/          AppUser aggregate, value objects, domain events, IEmailGateway
  Application/     IdentityManager, service interfaces (IJwtTokenGenerator, IFileStorage,
                   IPasswordAuthenticationEngine, ITotpEngine)
  Infrastructure/  EF Core, ASP.NET Identity adapters, JWT generation, refresh token store,
                   local file storage, Mailpit SMTP gateway
  Client/          ASP.NET Core controllers, FluentValidation validators, DI wiring
```

## API surface

| Controller | Routes | Purpose |
|---|---|---|
| `AuthController` | `POST /api/identity/auth/register`, `/login`, `/refresh`, `/logout` | Core auth flow |
| `TwoFactorController` | `GET/POST /api/identity/auth/2fa/*` | TOTP setup + verification |
| `PasswordController` | `POST /api/identity/auth/forgot-password`, `/reset-password` | Password reset flow |
| `ProfileController` | `GET/PUT /api/identity/profile`, `POST …/avatar` | User profile + avatar |
| `SessionsController` | `GET/DELETE /api/identity/sessions` | Active session management |
| `AdminController` | `GET/PUT /api/identity/admin/users` | Admin user controls |

## Environment variables

| Variable | Description |
|---|---|
| `ConnectionStrings__Identity` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key (≥ 32 chars) |
| `Jwt__ExpiryMinutes` | Access token lifetime (default: 15) |
| `RabbitMq__Host` | RabbitMQ hostname |
| `RabbitMq__Username` | RabbitMQ username |
| `RabbitMq__Password` | RabbitMQ password |
| `Email__Host` | SMTP hostname (Mailpit in dev: `localhost`) |
| `Email__Port` | SMTP port (Mailpit default: `1025`) |
| `Email__Username` | SMTP username (empty in dev) |
| `Email__Password` | SMTP password (empty in dev) |
| `Email__FromAddress` | From address for outbound email |
| `Email__FromName` | From display name |
| `Email__BaseUrl` | Base URL used in email links |
| `Storage__LocalPath` | Filesystem path for avatar uploads |
| `Storage__PublicBaseUrl` | Public URL prefix served over static files |
| `Recaptcha__SecretKey` | reCAPTCHA v2 secret key |
| `Cors__AllowedOrigins__0` | First allowed CORS origin |

## CI/CD

Two workflows run on push to `main`:

| Workflow | File | What it does |
|---|---|---|
| **Build & Publish** | `.github/workflows/docker-publish.yml` | Runs `dotnet test`, builds the Docker image, pushes to `ghcr.io/hkarpinen/portfolio-identity:latest` |
| **Deploy** | `.github/workflows/deploy.yml` | Triggers after Build & Publish succeeds; SSHes into the server, pulls the new image, and restarts only the `identity` container |

### Required GitHub Actions secrets

| Secret | Description |
|---|---|
| `DEPLOY_HOST` | VPS IP address or hostname |
| `DEPLOY_USER` | SSH user on the server |
| `DEPLOY_KEY` | Private SSH key for that user |
| `DEPLOY_PATH` | Absolute path to the infra directory on the server |

## Docs

- [Domain model & invariants](docs/Domain.md)
- Use cases: [`docs/use-cases/`](docs/use-cases/)

