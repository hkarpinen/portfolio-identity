# Use Case: Administration

**Manager:** `IdentityManager`  
**Actor:** Admin user

---

## Ban User

**Entry point:** `POST /admin/users/{id}/ban`

```mermaid
sequenceDiagram
    participant C as Client (Admin)
    participant Ctrl as AdminController
    participant Mgr as IdentityManager
    participant U as AppUser
    participant UR as IUserRepository

    C->>Ctrl: POST /admin/users/{id}/ban
    Ctrl->>Mgr: BanAsync(userId)
    Mgr->>UR: GetByIdAsync(userId)
    UR-->>Mgr: user (or null → Failure)
    Mgr->>U: user.Ban()
    Note over U: LockoutEnabled=true, LockoutEnd=DateTimeOffset.MaxValue
    U-->>Mgr: (+UserBanned event)
    Mgr->>UR: SaveAsync(user)
    Mgr-->>Ctrl: Result.Success()
    Ctrl-->>C: 200 OK
```

Effect: The user's account is locked indefinitely via ASP.NET Identity lockout. Future login attempts will fail with `"Account is locked out."`.

---

## Change Role

**Entry point:** `POST /admin/users/{id}/role`

```mermaid
sequenceDiagram
    participant C as Client (Admin)
    participant Ctrl as AdminController
    participant Mgr as IdentityManager
    participant U as AppUser
    participant UR as IUserRepository

    C->>Ctrl: POST /admin/users/{id}/role {role}
    Ctrl->>Mgr: ChangeRoleAsync(userId, role)
    Mgr->>UR: GetByIdAsync(userId)
    UR-->>Mgr: user (or null → Failure)
    Mgr->>U: user.ChangeRole(newRole)
    U-->>Mgr: (+UserRoleChanged event)
    Mgr->>UR: SaveAsync(user)
    Mgr-->>Ctrl: Result.Success()
    Ctrl-->>C: 200 OK
```

## Notes

- The `UserBanned` domain event is published via RabbitMQ and consumed by the Forum service to enforce content restrictions.
- Role changes are reflected in new JWT tokens on next login; existing tokens retain the old role until expiry.
