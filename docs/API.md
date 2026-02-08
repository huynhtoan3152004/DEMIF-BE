# DEMIF - API Reference

## 🔐 Authentication

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | - | Đăng ký email |
| POST | `/api/auth/login` | - | Đăng nhập |
| POST | `/api/auth/firebase` | - | Login Google |
| POST | `/api/auth/refresh-token` | - | Refresh JWT |
| POST | `/api/auth/logout` | ✅ | Logout |
| POST | `/api/auth/change-password` | ✅ | Đổi mật khẩu |

---

## 👤 Profile

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/profile/me` | ✅ | Lấy profile |
| PUT | `/api/profile/me` | ✅ | Cập nhật profile |

---

## 📚 Lessons

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/lessons` | - | List lessons (filter theo subscription) |
| GET | `/api/lessons/{id}` | - | Chi tiết lesson (check premium access) |

### Admin Lessons
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/admin/lessons` | Staff | List all lessons |
| GET | `/api/admin/lessons/{id}` | Staff | Chi tiết |
| POST | `/api/admin/lessons` | Staff | Tạo mới |
| PUT | `/api/admin/lessons/{id}` | Staff | Cập nhật |
| DELETE | `/api/admin/lessons/{id}` | Staff | Xóa (archive) |

---

## 💳 Subscriptions

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/subscription-plans` | - | List gói Premium |
| POST | `/api/subscription-plans/subscribe` | ✅ | Đăng ký gói |
| GET | `/api/subscription-plans/my-subscription` | ✅ | Subscription hiện tại |
| POST | `/api/subscription-plans/cancel` | ✅ | Hủy auto-renew |

### Admin Subscriptions
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/admin/subscription-plans` | Admin | List + stats |
| GET | `/api/admin/subscription-plans/stats` | Admin | Thống kê |
| POST | `/api/admin/subscription-plans` | Admin | Tạo plan |
| PUT | `/api/admin/subscription-plans/{id}` | Admin | Update/đổi giá |
| DELETE | `/api/admin/subscription-plans/{id}` | Admin | Xóa plan |

---

## 💰 Payments

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/payments/sepay/webhook` | - | SEPay callback |

---

## 👥 Admin - Users

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/admin/users` | Admin | List users |
| GET | `/api/admin/users/{id}` | Admin | Chi tiết user |
| POST | `/api/admin/users` | Admin | Tạo user |
| PUT | `/api/admin/users/{id}` | Admin | Cập nhật |
| PATCH | `/api/admin/users/{id}/status` | Admin | Đổi status |
| DELETE | `/api/admin/users/{id}` | Admin | Xóa |
| POST | `/api/admin/users/{id}/roles/{roleId}` | Admin | Gán role |
| DELETE | `/api/admin/users/{id}/roles/{roleId}` | Admin | Xóa role |

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
