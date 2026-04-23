# Use Case: Profile Management

**Manager:** `IdentityManager`

---

## Get Profile

**Actor:** Authenticated user  
**Entry point:** `GET /profile`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as ProfileController
    participant Mgr as IdentityManager
    participant UR as IUserRepository

    C->>Ctrl: GET /profile
    Ctrl->>Mgr: GetProfileAsync(userId)
    Mgr->>UR: GetByIdAsync(userId)
    UR-->>Mgr: user (or null → Failure)
    Mgr-->>Ctrl: Success(UserProfileResponse)
    Ctrl-->>C: 200 OK
```

---

## Update Profile

**Entry point:** `PUT /profile`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as ProfileController
    participant Mgr as IdentityManager
    participant U as AppUser
    participant UR as IUserRepository

    C->>Ctrl: PUT /profile {displayName, avatarUrl?}
    Ctrl->>Mgr: UpdateProfileAsync(userId, request)
    Mgr->>UR: GetByIdAsync(userId)
    UR-->>Mgr: user (or null → Failure)
    Mgr->>U: user.UpdateProfile(displayName, avatarUrl?)
    U-->>Mgr: (+UserProfileUpdated event)
    Mgr->>UR: SaveAsync(user)
    Mgr-->>Ctrl: Result.Success()
    Ctrl-->>C: 200 OK
```

---

## Upload Avatar

**Entry point:** `POST /profile/avatar`

```mermaid
sequenceDiagram
    participant C as Client
    participant Ctrl as ProfileController
    participant Mgr as IdentityManager
    participant U as AppUser
    participant UR as IUserRepository
    participant FS as IFileStorage

    C->>Ctrl: POST /profile/avatar (multipart file)
    Ctrl->>Mgr: UploadAvatarAsync(userId, request)
    Mgr-->>Mgr: guard: file not empty
    Mgr-->>Mgr: guard: size <= 5 MB
    Mgr-->>Mgr: guard: content-type in {PNG, JPEG, WebP, GIF}
    Mgr->>UR: GetByIdAsync(userId)
    UR-->>Mgr: user (or null → Failure)
    Mgr->>FS: SaveAsync(key, stream, contentType)
    FS-->>Mgr: avatarUrl
    Mgr->>U: user.ChangeAvatar(avatarUrl)
    U-->>Mgr: (+UserProfileUpdated event)
    Mgr->>UR: SaveAsync(user)
    Mgr-->>Ctrl: Success(UploadAvatarResponse)
    Ctrl-->>C: 200 OK {avatarUrl}
```

## Guard failures

| Guard | Error |
|---|---|
| File empty | `Failure("File is empty.")` |
| File > 5 MB | `Failure("File exceeds the 5 MB limit.")` |
| Unsupported content type | `Failure("Unsupported image type...")` |
