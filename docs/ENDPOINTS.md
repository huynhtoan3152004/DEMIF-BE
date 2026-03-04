# Demif API — Endpoint Summary

> Auto-generated comprehensive summary of all API endpoints, grouped by domain.

---

## Overview

| Group | Controller | Route Prefix | Auth | Endpoints |
|-------|-----------|--------------|------|-----------|
| Authentication | `AuthController` | `api/auth` | Public | 6 |
| Profile | `ProfileController` | `api/profile` | User | 3 |
| Lessons (Public) | `LessonsController` | `api/lessons` | Mixed | 4 |
| Subscriptions | `SubscriptionPlansController` | `api/subscription-plans` | Mixed | 4 |
| Payments | `PaymentsController` | `api/payments` | Public | 1 |
| **Admin** — Lessons | `AdminLessonsController` | `api/admin/lessons` | Staff+ | 7 |
| **Admin** — Subscriptions | `AdminSubscriptionPlansController` | `api/admin/subscription-plans` | Admin | 4 |
| **Admin** — Users | `UsersController` | `api/users` | Admin | 8 |
| **Total** | | | | **37** |

---

## 1. Authentication (`api/auth`)

**Controller:** `AuthController` — Public endpoints, no token required.

### POST Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 1 | `POST` | `/api/auth/register` | Public | Register new account (sends verification email) |
| 2 | `POST` | `/api/auth/login` | Public | Login with email/password (requires verified email) |
| 3 | `POST` | `/api/auth/google-login` | Public | Login with Google OAuth ID Token |
| 4 | `POST` | `/api/auth/refresh-token` | Public | Refresh JWT access token |
| 5 | `POST` | `/api/auth/logout` | Public | Logout (revoke refresh token) |

### GET Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 6 | `GET` | `/api/auth/verify-email?token=xxx` | Public | Verify email after registration |

### Request/Response Summary

| Endpoint | Request Body | Response |
|----------|-------------|----------|
| Register | `{ email, password, confirmPassword, username }` | `RegisterResponse` |
| Login | `{ email, password }` | `LoginResponse` (JWT + refresh token) |
| Google Login | `{ idToken }` | `GoogleLoginResponse` |
| Refresh Token | `{ refreshToken }` | `RefreshTokenResponse` |
| Logout | `{ refreshToken }` | `{ message }` |
| Verify Email | Query: `token` | `VerifyEmailResponse` (JWT for auto-login) |

---

## 2. Profile (`api/profile`)

**Controller:** `ProfileController` — All endpoints require `[Authorize]`.

### GET Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 1 | `GET` | `/api/profile/me` | User | Get current user's profile |

### POST/PUT Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 2 | `PUT` | `/api/profile/me` | User | Update profile (username, language, etc.) |
| 3 | `POST` | `/api/profile/change-password` | User | Change password |

---

## 3. Lessons — Public (`api/lessons`)

**Controller:** `LessonsController` — Browse lessons and practice dictation.

### GET Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 1 | `GET` | `/api/lessons?page=1&pageSize=10&level=&type=&category=` | Public | Paginated lesson list with filters |
| 2 | `GET` | `/api/lessons/{id}` | Public | Lesson detail (checks premium access) |
| 3 | `GET` | `/api/lessons/{id}/dictation?levelStr=Beginner` | Public | Get dictation exercise (answers stripped) |

### POST Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 4 | `POST` | `/api/lessons/{id}/dictation/submit` | **User** | Submit dictation answers (scoring + save) |

### Media Type System

All lesson responses include these fields for proper frontend rendering:

| Field | Type | Description |
|-------|------|-------------|
| `mediaUrl` | `string?` | Primary media URL (MP3 path or YouTube embed URL) |
| `audioUrl` | `string?` | Legacy audio URL (backward compat) |
| `mediaType` | `string` | `"audio"` \| `"video"` \| `"youtube"` |
| `videoId` | `string?` | YouTube Video ID (only when `mediaType == "youtube"`) |
| `embedUrl` | `string?` | YouTube embed URL (only when `mediaType == "youtube"`) |
| `thumbnailUrl` | `string?` | Lesson thumbnail (auto-fetched for YouTube) |

**Frontend rendering logic:**
```
if (mediaType === "youtube") → render YouTube iframe with embedUrl or videoId
if (mediaType === "audio")   → render HTML5 <audio> with mediaUrl
if (mediaType === "video")   → render HTML5 <video> with mediaUrl
```

---

## 4. Subscriptions (`api/subscription-plans`)

**Controller:** `SubscriptionPlansController` — Browse plans (public) + manage subscription (authenticated).

### GET Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 1 | `GET` | `/api/subscription-plans` | Public | List all active plans |
| 2 | `GET` | `/api/subscription-plans/my-subscription` | User | Get current user's active subscription |

### POST Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 3 | `POST` | `/api/subscription-plans/subscribe` | User | Subscribe to premium plan (creates pending payment) |
| 4 | `POST` | `/api/subscription-plans/cancel` | User | Cancel auto-renewal |

---

## 5. Payments (`api/payments`)

**Controller:** `PaymentsController` — Payment webhook callbacks.

### POST Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 1 | `POST` | `/api/payments/sepay/webhook` | Public* | SEPay payment webhook (*verified by signature) |

---

## 6. Admin — Lessons (`api/admin/lessons`)

**Controller:** `AdminLessonsController` — Requires `RequireStaff` policy (Staff or Admin role).

### GET Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 1 | `GET` | `/api/admin/lessons?page=1&pageSize=10&status=` | Staff+ | List all lessons (no premium filter) |
| 2 | `GET` | `/api/admin/lessons/{id}` | Staff+ | Get lesson detail (full admin info) |
| 3 | `GET` | `/api/admin/lessons/youtube/preview?url=` | Staff+ | Preview YouTube video before creating lesson |

### POST Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 4 | `POST` | `/api/admin/lessons` | Staff+ | Create lesson manually (auto-generates DictationTemplates) |
| 5 | `POST` | `/api/admin/lessons/from-youtube` | Staff+ | Create lesson from YouTube URL (auto-fetch metadata + captions) |
| 6 | `POST` | `/api/admin/lessons/{id}/regenerate-templates` | Staff+ | Re-generate DictationTemplates for existing lesson |

### PUT/DELETE Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 7 | `PUT` | `/api/admin/lessons/{id}` | Staff+ | Update lesson (re-generates templates if transcript changed) |
| 8 | `DELETE` | `/api/admin/lessons/{id}` | Staff+ | Soft delete lesson (archived) |

---

## 7. Admin — Subscription Plans (`api/admin/subscription-plans`)

**Controller:** `AdminSubscriptionPlansController` — Requires `RequireAdmin` policy.

### GET Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 1 | `GET` | `/api/admin/subscription-plans` | Admin | List all plans with subscriber statistics |
| 2 | `GET` | `/api/admin/subscription-plans/stats` | Admin | Subscription statistics overview |

### POST/PUT/DELETE Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 3 | `POST` | `/api/admin/subscription-plans` | Admin | Create new subscription plan |
| 4 | `PUT` | `/api/admin/subscription-plans/{id}` | Admin | Update plan (including pricing) |
| 5 | `DELETE` | `/api/admin/subscription-plans/{id}` | Admin | Soft delete plan |

---

## 8. Admin — Users (`api/users`)

**Controller:** `UsersController` — Requires `RequireAdmin` policy.

### GET Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 1 | `GET` | `/api/users?page=&pageSize=` | Admin | List users with pagination |
| 2 | `GET` | `/api/users/{id}` | Admin | Get user details by ID |

### POST Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 3 | `POST` | `/api/users` | Admin | Create new user |
| 4 | `POST` | `/api/users/{id}/roles` | Admin | Assign role to user |

### PUT/PATCH Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 5 | `PUT` | `/api/users/{id}` | Admin | Update user information |
| 6 | `PATCH` | `/api/users/{id}/status` | Admin | Update user status (Activate/Deactivate/Ban) |

### DELETE Endpoints

| # | Method | Route | Auth | Description |
|---|--------|-------|------|-------------|
| 7 | `DELETE` | `/api/users/{id}` | Admin | Soft delete user |
| 8 | `DELETE` | `/api/users/{id}/roles/{roleName}` | Admin | Remove role from user |

---

## Authorization Policies

| Policy | Required Role | Used By |
|--------|--------------|---------|
| (none) | Public access | Auth, Payments, Lesson browsing |
| `[Authorize]` | Any authenticated user | Profile, Dictation submit |
| `RequireUser` | User role | Subscribe, My subscription, Cancel |
| `RequireStaff` | Staff or Admin | Admin Lessons |
| `RequireAdmin` | Admin only | Admin Users, Admin Subscription Plans |

---

## Controller Relationships

```
┌─────────────────────────────────────────────────────── PUBLIC ──┐
│                                                                 │
│  AuthController          → Registration, Login, Token flow      │
│  LessonsController       → Browse & practice (GET=public,       │
│                            POST submit=authenticated)           │
│  SubscriptionPlansController → Browse plans (GET=public),       │
│                               manage subscription (POST=user)   │
│  PaymentsController      → Webhook only (SEPay)                 │
│                                                                 │
├──────────────────────────────────────────── AUTHENTICATED ──────┤
│                                                                 │
│  ProfileController       → Personal profile CRUD + password     │
│                                                                 │
├─────────────────────────────────────────────── ADMIN ───────────┤
│                                                                 │
│  AdminLessonsController           → Lesson CRUD + YouTube       │
│       ↕ related                     import + Templates          │
│  LessonsController                → Public lesson view          │
│                                                                 │
│  AdminSubscriptionPlansController → Plan CRUD + Stats           │
│       ↕ related                                                 │
│  SubscriptionPlansController      → Public plan view + Subscribe│
│       ↕ related                                                 │
│  PaymentsController               → Payment webhook             │
│                                                                 │
│  UsersController                  → User CRUD + Roles + Status  │
│       ↕ related                                                 │
│  ProfileController                → Self-service profile        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## HTTP Method Summary

| Method | Count | Endpoints |
|--------|-------|-----------|
| `GET` | 14 | List/Detail/Preview operations |
| `POST` | 16 | Create, Submit, Login, Webhook operations |
| `PUT` | 4 | Update operations |
| `PATCH` | 1 | Status update |
| `DELETE` | 4 | Soft delete operations |
| **Total** | **39** | |
