# Use Case: Registration

**Manager:** `IdentityManager`

---

## Register

**Actor:** Anonymous user  
**Entry point:** `POST /auth/register`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as AuthController
    participant Mgr as IdentityManager
    participant U as AppUser
    participant UR as IUserRepository
    participant Email as IEmailGateway

    C->>Ctrl: POST /auth/register {email, password, displayName}
    Ctrl->>Mgr: RegisterAsync(request)
    Mgr->>U: AppUser.Create(Email.From(email), displayName)
    U-->>Mgr: user (+UserRegistered event)
    Mgr->>UR: CreateWithPasswordAsync(user, password)
    UR-->>Mgr: (succeeded, error?)
    alt failure
        Mgr-->>Ctrl: Result.Failure(error)
        Ctrl-->>C: 400 Bad Request
    end
    Mgr->>UR: GenerateEmailConfirmationTokenAsync(user)
    UR-->>Mgr: token
    Mgr->>Email: SendConfirmationEmailAsync(email, userId, token, displayName)
    Mgr-->>Ctrl: Result.Success()
    Ctrl-->>C: 200 OK
```

---

## Confirm Email

**Entry point:** `POST /auth/confirm-email`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as AuthController
    participant Mgr as IdentityManager
    participant UR as IUserRepository

    C->>Ctrl: POST /auth/confirm-email {userId, token}
    Ctrl->>Mgr: ConfirmEmailAsync(request)
    Mgr->>UR: GetByIdAsync(userId)
    UR-->>Mgr: user (or null → Failure)
    alt already confirmed
        Mgr-->>Ctrl: Result.Success() (idempotent)
    end
    Mgr->>UR: ConfirmEmailAsync(user, token)
    UR-->>Mgr: (succeeded, error?)
    Mgr-->>Ctrl: Result
    Ctrl-->>C: 200 OK / 400
```

## Guard failures

| Guard | Error |
|---|---|
| Invalid email format | `Email.From` throws `ArgumentException` |
| Duplicate email / weak password | `CreateWithPasswordAsync` returns error string |
| Invalid confirmation token | `ConfirmEmailAsync` returns error |
