# Use Case: Authentication

**Manager:** `IdentityManager`

---

## Login

**Actor:** Registered user with confirmed email  
**Entry point:** `POST /auth/login`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as AuthController
    participant Mgr as IdentityManager
    participant UR as IUserRepository
    participant PWD as IPasswordAuthenticationEngine
    participant JWT as IJwtTokenGenerator

    C->>Ctrl: POST /auth/login {email, password}
    Ctrl->>Mgr: LoginAsync(request)
    Mgr->>UR: GetByEmailAsync(email)
    UR-->>Mgr: user (or null → Failure)
    Mgr-->>Mgr: guard: EmailConfirmed
    Mgr->>PWD: CheckPasswordAsync(user, password)
    PWD-->>Mgr: {Succeeded, IsLockedOut}
    alt locked out
        Mgr-->>Ctrl: Failure("Account is locked out.")
    else wrong password
        Mgr-->>Ctrl: Failure("Invalid email or password.")
    end
    alt 2FA enabled
        Mgr-->>Ctrl: Success(RequiresTwoFactor=true, Token=null)
        Ctrl-->>C: 200 OK (no token — client must call /auth/2fa/verify)
    else
        Mgr->>JWT: GenerateToken(user)
        JWT-->>Mgr: {Token, ExpiresAt}
        Mgr-->>Ctrl: Success(RequiresTwoFactor=false, Token)
        Ctrl-->>C: 200 OK + JWT
    end
```

---

## Enable Two-Factor Authentication

**Entry point:** `POST /auth/2fa/enable`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as AuthController
    participant Mgr as IdentityManager
    participant UR as IUserRepository

    C->>Ctrl: POST /auth/2fa/enable
    Ctrl->>Mgr: EnableTwoFactorAsync(userId)
    Mgr->>UR: GetByIdAsync(userId)
    UR-->>Mgr: user (or null → Failure)
    Mgr->>UR: ResetAuthenticatorKeyAsync(user)
    Mgr->>UR: GetAuthenticatorKeyAsync(user)
    UR-->>Mgr: key
    Mgr-->>Mgr: GenerateAuthenticatorUri(email, key)
    Mgr-->>Ctrl: Success({key, otpauthUri})
    Ctrl-->>C: 200 OK (scan QR in authenticator app)
```

---

## Verify Two-Factor Code (first-time or login)

**Entry point:** `POST /auth/2fa/verify`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as AuthController
    participant Mgr as IdentityManager
    participant UR as IUserRepository
    participant JWT as IJwtTokenGenerator

    C->>Ctrl: POST /auth/2fa/verify {email, code}
    Ctrl->>Mgr: VerifyTwoFactorAsync(request)
    Mgr->>UR: GetByEmailAsync(email)
    UR-->>Mgr: user (or null → Failure)
    Mgr->>UR: VerifyTwoFactorTokenAsync(user, code)
    UR-->>Mgr: isValid
    alt invalid
        Mgr-->>Ctrl: Failure("Invalid verification code.")
    end
    alt 2FA not yet enabled
        Mgr->>UR: SetTwoFactorEnabledAsync(user, true)
    end
    Mgr->>JWT: GenerateToken(user)
    JWT-->>Mgr: {Token, ExpiresAt}
    Mgr-->>Ctrl: Success(Token)
    Ctrl-->>C: 200 OK + JWT
```

## Guard failures

| Guard | Error |
|---|---|
| User not found | `Failure("User not found.")` or `"Invalid email or password."` |
| Email not confirmed | `Failure("Email not confirmed.")` |
| Account locked out | `Failure("Account is locked out.")` |
| Wrong password | `Failure("Invalid email or password.")` |
| Invalid 2FA code | `Failure("Invalid verification code.")` |
