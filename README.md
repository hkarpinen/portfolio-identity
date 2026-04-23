# portfolio-identity

Authentication and user identity service. Handles registration, email confirmation, JWT login, TOTP two-factor authentication, profile management, avatar upload, and admin controls.

## Stack

- .NET 8 / ASP.NET Core Web API
- ASP.NET Identity (EF Core + PostgreSQL 17)
- JWT authentication
- TOTP 2FA (RFC 6238 / Google Authenticator compatible)
- Clean Architecture: Domain → Application → Infrastructure → Client

## Running locally

```bash
dotnet run --project src/Client
```

Or via the full stack in `portfolio-infra`:

```bash
docker compose up identity
```

## Structure

```
src/
  Domain/          AppUser aggregate, value objects, domain events, IEmailGateway
  Application/     IdentityManager, service interfaces (IJwtTokenGenerator, IFileStorage, IPasswordAuthenticationEngine)
  Infrastructure/  EF Core, ASP.NET Identity adapters, JWT, local file storage, email (Mailpit)
  Client/          ASP.NET Core controllers, validators, DI wiring
```

## Docs

- [Domain model & invariants](docs/Domain.md)
- Use cases: [`docs/use-cases/`](docs/use-cases/)

## Environment variables

| Variable | Description |
|---|---|
| `ConnectionStrings__Identity` | PostgreSQL connection string |
| `Jwt__Secret` | JWT signing key (≥ 32 chars) |
| `RabbitMq__Host` | RabbitMQ hostname |
| `RabbitMq__Username` | RabbitMQ username |
| `RabbitMq__Password` | RabbitMQ password |
| `Email__Host` | SMTP hostname (Mailpit in dev) |
| `Email__Port` | SMTP port |
| `Storage__LocalPath` | Local avatar storage path |
| `Storage__PublicBaseUrl` | Public URL prefix for avatars |
