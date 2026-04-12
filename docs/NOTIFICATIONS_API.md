# DEMIF Notifications API

> Base URL: `/api`
>
> Authentication: JWT Bearer Token
>
> Authorization:
> - Admin broadcast: `RequireAdmin`
> - User inbox: authenticated user

---

## 1. Overview

Notifications are persisted per user in the inbox table and can also be broadcast by admin through email.

Current backend supports:

- admin broadcast notification
- user inbox list
- unread badge count
- mark one notification as read
- mark all notifications as read

---

## 2. Admin Broadcast

### Endpoint

`POST /api/admin/notifications/broadcast`

### Purpose

Send a system announcement from admin to all eligible users.

The backend will:

- create a `UserNotification` row for each eligible user
- send the same content by email
- return a broadcast summary for FE

### Request JSON

```json
{
  "title": "System maintenance tonight",
  "message": "We will perform maintenance from 23:00 to 23:30 UTC. The app may be temporarily unavailable.",
  "actionUrl": "https://demif.app/status"
}
```

### Request Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `title` | string | yes | Short announcement title, max 120 chars |
| `message` | string | yes | Main content of the announcement, max 4000 chars |
| `actionUrl` | string | no | Optional CTA URL, max 500 chars |

### Success Response JSON

```json
{
  "notificationId": "7d3e0b26-7c9f-4f1a-a8ff-2f9d1c5dbb14",
  "title": "System maintenance tonight",
  "message": "We will perform maintenance from 23:00 to 23:30 UTC. The app may be temporarily unavailable.",
  "actionUrl": "https://demif.app/status",
  "audience": "all-reachable-users",
  "channel": "email",
  "eligibleUserCount": 1542,
  "sentCount": 1542,
  "failedCount": 0,
  "summary": "Đã gửi thông báo đến 1542 người dùng."
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `notificationId` | string (guid) | Broadcast batch id for tracking |
| `title` | string | Normalized title |
| `message` | string | Normalized message |
| `actionUrl` | string or null | Optional CTA URL |
| `audience` | string | Current audience label, `all-reachable-users` |
| `channel` | string | Current delivery channel, `email` |
| `eligibleUserCount` | number | Number of user inbox rows created |
| `sentCount` | number | Number of successful email deliveries |
| `failedCount` | number | Number of failed email deliveries |
| `summary` | string | Human-readable FE message |

### Error Response JSON

```json
{
  "error": "Không có người nhận hợp lệ để gửi thông báo.",
  "code": "Admin.Notification.NoRecipients"
}
```

---

## 3. User Inbox

### 3.1 Get My Notifications

`GET /api/me/notifications?page=1&pageSize=20`

Returns unread items first, then newest items.

#### Success Response JSON

```json
{
  "items": [
    {
      "id": "c8e8a3cf-0e18-4f20-a5b5-9a3fa82a6ad0",
      "type": "system_announcement",
      "title": "Weekend offer",
      "message": "Get 30% off premium plans this weekend only.",
      "actionUrl": "https://demif.app/pricing",
      "channel": "email",
      "isRead": false,
      "readAt": null,
      "createdAt": "2026-04-12T10:15:00Z"
    }
  ],
  "totalCount": 12,
  "unreadCount": 3,
  "page": 1,
  "pageSize": 20,
  "totalPages": 1
}
```

#### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `items` | array | Notification list, unread first |
| `totalCount` | number | Total notifications for current user |
| `unreadCount` | number | Number of unread notifications |
| `page` | number | Current page |
| `pageSize` | number | Page size |
| `totalPages` | number | Total pages |

#### Item Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string (guid) | Notification id |
| `type` | string | Notification type, currently `system_announcement` |
| `title` | string | Notification title |
| `message` | string | Notification body |
| `actionUrl` | string or null | Optional action link |
| `channel` | string | Delivery channel, currently `email` |
| `isRead` | boolean | Read state |
| `readAt` | string or null | Read timestamp |
| `createdAt` | string | Creation timestamp |

### 3.2 Get Unread Count

`GET /api/me/notifications/unread-count`

#### Success Response JSON

```json
{
  "unreadCount": 3
}
```

### 3.3 Mark One Notification As Read

`PATCH /api/me/notifications/{id}/read`

#### Success Response JSON

```json
{
  "id": "c8e8a3cf-0e18-4f20-a5b5-9a3fa82a6ad0",
  "isRead": true,
  "readAt": "2026-04-12T10:18:00Z"
}
```

### 3.4 Mark All As Read

`POST /api/me/notifications/read-all`

#### Success Response JSON

```json
{
  "updatedCount": 3
}
```

---

## 4. FE Call Flow

### Inbox badge

Use `GET /api/me/notifications/unread-count` for the top bar badge.

### Notification drawer / page

Use `GET /api/me/notifications?page=1&pageSize=20` and render `items`.

### Read interaction

- Clicking one item: call `PATCH /api/me/notifications/{id}/read`
- Clicking “Mark all as read”: call `POST /api/me/notifications/read-all`

### Broadcast compose flow for admin

1. Admin opens compose modal.
2. Admin enters `title`, `message`, optional `actionUrl`.
3. FE calls `POST /api/admin/notifications/broadcast`.
4. FE shows `summary` and `sentCount`.

---

## 5. Notes

- Broadcast currently uses email as the delivery channel.
- User inbox is persisted, so FE can safely reload and fetch history.
- `actionUrl` is optional and can be used as a CTA button in email and inbox rendering.
