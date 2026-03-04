# DEMIF — API Reference

> Base URL: `/api`  
> Authentication: JWT Bearer Token  
> Last updated: 2026-03-04

---

## 🔐 Authentication

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | — | Register new account (sends verification email) |
| GET | `/api/auth/verify-email?token=xxx` | — | Verify email → returns JWT |
| POST | `/api/auth/login` | — | Login with email/password |
| POST | `/api/auth/google-login` | — | Login with Google OAuth |
| POST | `/api/auth/refresh-token` | — | Refresh JWT access token |
| POST | `/api/auth/logout` | — | Logout (revoke refresh token) |

### Register

**`POST /api/auth/register`**

```json
{
  "email": "user@example.com",
  "password": "SecurePass1",
  "confirmPassword": "SecurePass1",
  "username": "john_doe"
}
```

**Validation:**
- `email` — required, valid email, max 256 chars
- `password` — required, 6-100 chars, 1 uppercase, 1 lowercase, 1 digit
- `confirmPassword` — must match password
- `username` — required, 3-50 chars, alphanumeric + underscores only

**Responses:** `200` success | `400` validation error | `409` email/username exists

### Login

**`POST /api/auth/login`**

```json
{
  "email": "user@example.com",
  "password": "SecurePass1"
}
```

**Responses:** `200` JWT tokens | `400` validation | `401` invalid credentials | `403` email not verified

### Google Login

**`POST /api/auth/google-login`**

```json
{
  "idToken": "google-id-token-from-client"
}
```

**Responses:** `200` JWT tokens | `401` invalid token

### Refresh Token

**`POST /api/auth/refresh-token`**

```json
{
  "refreshToken": "your-refresh-token"
}
```

**Responses:** `200` new JWT tokens | `401` invalid/expired

---

## 👤 Profile (Authenticated)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/profile/me` | ✅ | Get my profile |
| PUT | `/api/profile/me` | ✅ | Update my profile |
| POST | `/api/profile/change-password` | ✅ | Change password |

### Update Profile

**`PUT /api/profile/me`**

```json
{
  "username": "new_username",
  "country": "Vietnam",
  "nativeLanguage": "Vietnamese",
  "targetLanguage": "English"
}
```

All fields are optional — only provided fields will be updated.

**Responses:** `200` success | `401` unauthenticated | `409` username conflict

---

## 📚 Lessons

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/lessons` | — | List lessons (paginated, filterable) |
| GET | `/api/lessons/{id}` | — | Get lesson detail (checks premium) |
| GET | `/api/lessons/{id}/dictation?level=Beginner` | — | Get dictation exercise |
| POST | `/api/lessons/{id}/dictation/submit` | ✅ | Submit dictation answers |

### List Lessons

**`GET /api/lessons?page=1&pageSize=10&level=Beginner&type=...&category=...`**

### Dictation Level

Accepts string or number: `Beginner`/`0`, `Intermediate`/`1`, `Advanced`/`2`, `Expert`/`3`

---

## 💳 Subscription Plans

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/subscription-plans` | — | List active plans |
| POST | `/api/subscription-plans/subscribe` | ✅ | Subscribe to a plan |
| GET | `/api/subscription-plans/my-subscription` | ✅ | Get my subscription |
| POST | `/api/subscription-plans/cancel` | ✅ | Cancel auto-renewal |

---

## 💰 Payments

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/payments/sepay/webhook` | — | SEPay payment callback |

---

## 🛠 Admin — Lessons (Staff+)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/admin/lessons` | Staff | List all lessons |
| GET | `/api/admin/lessons/{id}` | Staff | Get lesson detail |
| POST | `/api/admin/lessons` | Staff | Create lesson |
| PUT | `/api/admin/lessons/{id}` | Staff | Update lesson |
| DELETE | `/api/admin/lessons/{id}` | Staff | Delete lesson (archive) |
| POST | `/api/admin/lessons/{id}/regenerate-templates` | Staff | Regenerate dictation templates |
| GET | `/api/admin/lessons/youtube/preview?url=...` | Staff | Preview YouTube video metadata |
| POST | `/api/admin/lessons/from-youtube` | Staff | Create lesson from YouTube URL |

---

## 🛠 Admin — Subscription Plans (Admin)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/admin/subscription-plans` | Admin | List plans with stats |
| GET | `/api/admin/subscription-plans/stats` | Admin | Get statistics overview |
| POST | `/api/admin/subscription-plans` | Admin | Create plan |
| PUT | `/api/admin/subscription-plans/{id}` | Admin | Update plan |
| DELETE | `/api/admin/subscription-plans/{id}` | Admin | Delete plan |

---

## 🛠 Admin — Users (Admin)

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/users` | Admin | List users (paginated) |
| GET | `/api/users/{id}` | Admin | Get user detail |
| POST | `/api/users` | Admin | Create user |
| PUT | `/api/users/{id}` | Admin | Update user |
| DELETE | `/api/users/{id}` | Admin | Delete user (soft) |
| PATCH | `/api/users/{id}/status` | Admin | Update status |
| POST | `/api/users/{id}/roles` | Admin | Assign role |
| DELETE | `/api/users/{id}/roles/{roleName}` | Admin | Remove role |

---

## 📋 Response Format

### Success
```json
{
  "data": { ... },
  "message": "Success"
}
```

### Error
```json
{
  "error": "Error message here"
}
```

### Validation Error
```json
{
  "errors": [
    { "propertyName": "Email", "errorMessage": "Email is required" }
  ]
}
```

### Pagination
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 10,
  "totalCount": 100,
  "totalPages": 10
}
```
