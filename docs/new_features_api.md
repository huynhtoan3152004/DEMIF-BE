# TÀI LIỆU API: CÁC TÍNH NĂNG ENGAGEMENT, ADMIN & AUTH SECURITY

Tài liệu này bao gồm danh sách chi tiết các Endpoint, định dạng JSON Request/Response, cách xử lý luồng (flow) và các mã lỗi (Errors) thường gặp để Frontend Team tích hợp.

---

## 🚀 1. LUỒNG THÔNG TIN STREAK & LEADERBOARD

### 1.1 Tự động cộng Streak khi Login (Đã được xử lý ngầm)
- **APIs ảnh hưởng:** 
  - `POST /api/auth/login`
  - `POST /api/auth/google-login`
- **Frontend Flow:** Bạn không cần thay đổi bất kỳ body payload nào. Cứ mỗi khi gọi 2 API này thành công, Backend sẽ tự động kiểm tra xem ngày hôm nay người dùng đã Login chưa. Nếu chưa đăng nhập trong ngày (chuẩn UTC), Backend sẽ tự động tăng mốc Chuỗi ngày học liên tiếp (Current Streak) lên +1. Nếu bỏ lỡ ngày hôm qua, hệ thống sẽ reset về 1.

### 1.2 Xem Bảng Xếp Hạng (Leaderboard)
Lấy top User có `Streak` cao nhất hệ thống. Dùng để render Cúp/Bảng vinh danh.
- **Endpoint:** `GET /api/me/stats/leaderboard`
- **Auth:** Require `Bearer Token`
- **Query Params:** 
  - `limit` (int, default = 10): Số lượng người trong top cần lấy.
- **Response (200 OK):**
```json
[
  {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "username": "DavidNguyen",
    "avatarUrl": "https://example.com/avatar.png",
    "currentStreak": 15,    // Dùng để so sánh top
    "totalPoints": 4500,    // Chỉ số phụ trong trường hợp bằng nhau
    "level": "Intermediate" // Hiển thị badge Level cạnh username
  }
]
```
- **Lỗi có thể gặp:** 
  - `401 Unauthorized` nếu token hết hạn.

---

## 🎧 2. LUỒNG ĐỒNG BỘ BÀI HỌC (LESSON TRACKER)

Giúp User nghe hay làm bài dở dang vẫn có thể quay lại học tiếp ở đoạn cũ. 

### 2.1 Cập nhật tiến độ bài học
- **Flow Logic:** FE nên gọi API này khi User: Chuyển đoạn nghe (Segment), Tạm dừng / Thoát ra khỏi giao diện học, hoặc Gõ hoàn thành đúng 1 cầu. Khuyên dùng cơ chế *Debounce* (Ví dụ: 5s gọi 1 lần để tránh Spam API).
- **Endpoint:** `POST /api/lessons/{id}/sync-progress`
  *(Trong đó `{id}` là ID của Lesson đang học)*
- **Auth:** Require `Bearer Token`
- **Request Body:**
```json
{
  "segmentIndex": 3,     // Vị trí (Index) đoạn Audio/Video đang dừng lại
  "isCompleted": false   // Bằng true khi User bấm Kết Thúc bài, bằng false nếu đang học dở
}
```
- **Response (200 OK):**
```json
{
  "userId": "3fa85f64...",
  "lessonId": "b1b85f64...",
  "lessonTitle": "Lesson title",
  "lessonLevel": "Beginner",
  "status": "InProgress", // Sẽ trả về "Completed" nếu truyền isCompleted: true
  "lastSegmentIndex": 3,
  "totalSegments": 12,
  "completedSegments": 4,
  "remainingSegments": 8,
  "progressPercent": 33.3,
  "isLessonCompleted": false,
  "nextUncompletedSegmentIndex": 4,
  "completedSegmentIndexes": [0, 1, 2, 3],
  "remainingSegmentIndexes": [4, 5, 6, 7, 8, 9, 10, 11]
}
```
- **Lỗi có thể gặp:**
  - `400 Bad Request`: `{ "error": "Lesson.NotFound", "message": "Không tìm thấy bài học này." }` (URL truyền sai ID của lesson).
  - `400 Bad Request`: `{ "error": "SegmentIndex '99' không hợp lệ. Bài này chỉ có 12 segment(s)." }` (segmentIndex vượt quá số segment của bài).

---

## 🔒 3. LUỒNG QUÊN MẬT KHẨU (FORGOT PASSWORD) TRẢ QUA MAIL

### 3.1 Yêu cầu gửi Link đặt lại mật khẩu
- **Flow Logic:** User gõ email vào màn hình Quên Mật Khẩu trên UI. Bấm Gửi. Do tính chất bảo mật, API luôn báo thành công ngay cả khi email tào lao (để chống việc Hacker dùng màn hình này dò xem Email nào có xài app).
- **Endpoint:** `POST /api/auth/forgot-password`
- **Auth:** API Public (Không cần Token)
- **Request Body:**
```json
{
  "email": "user@example.com"
}
```
- **Response (200 OK):**
```json
{
  "message": "Nếu email hợp lệ, link lấy lại mật khẩu đã được gửi."
}
```

### 3.2 Nhập mật khẩu mới sau khi bấm vào Link ở Email
- **Flow Logic:** Trong hộp thư của hệ thống DEMIF gửi về sẽ có 1 đường Link: `http://{frontend-url}/reset-password?token=XYZ...`
  - FE lấy cục Token `XYZ` từ url.
  - Sau đó cho User nhập `newPassword`. Rồi nã xuống API bên dưới.
  - Thành công thì yêu cầu chuyển hướng về màn hình Login để đăng nhập lại.
- **Endpoint:** `POST /api/auth/reset-password`
- **Auth:** API Public (Không cần Token)
- **Request Body:**
```json
{
  "token": "Mã_Lấy_Từ_Param_URL",
  "newPassword": "123PasswordMoi@!"
}
```
- **Response (200 OK):**
```json
{
  "message": "Khôi phục mật khẩu thành công. Vui lòng đăng nhập lại."
}
```
- **Lỗi có thể gặp:**
  - `400 Bad Request`: `{ "error": "Link đổi mật khẩu đã hết hạn hoặc không hợp lệ." }` -> Yêu cầu người dùng bấm Forgot Password lại từ đầu do qua 15 phút.

---

## 👨‍💼 4. ADMINISTRATION (CHỈ DÀNH CHO ROLE ADMIN)

Hiển thị hệ thống Thống kê chuyên sâu làm biểu đồ tại trang Dashboard.

### 4.1 Lấy toàn bộ Analytics cho Dashboard
- **Endpoint:** `GET /api/admin/analytics`
- **Auth:** Require `Bearer Token` (Role = `Admin`)
- **Response (200 OK):**
```json
{
  "generatedAt": "2026-03-04T10:00:00Z",
  "summary": {
    "totalUsers": 1500,
    "activeUsers": 930,
    "newUsersToday": 24,
    "totalLessons": 180,
    "publishedLessons": 160,
    "totalExercises": 12040,
    "totalVocabulary": 8600,
    "dueVocabulary": 420,
    "activeSubscriptions": 280,
    "expiringSubscriptionsSoon": 18,
    "totalRevenue": 20000000,
    "pendingPayments": 6,
    "totalBlogs": 42
  },
  "users": {
    "dailyActiveUsers": 142,
    "monthlyActiveUsers": 500,
    "usersActiveInLast7Days": 310,
    "byStatus": [
      { "key": "Active", "count": 930 },
      { "key": "Pending", "count": 48 }
    ],
    "byAuthProvider": [
      { "key": "email", "count": 1200 },
      { "key": "google", "count": 300 }
    ],
    "byLevel": [
      { "key": "Beginner", "count": 760 },
      { "key": "Intermediate", "count": 510 }
    ]
  },
  "lessons": {
    "publishedLessons": 160,
    "draftLessons": 12,
    "archivedLessons": 8,
    "dictationLessons": 94,
    "shadowingLessons": 86,
    "averageScore": 74.3,
    "popularLessons": [
      {
        "lessonId": "b1b222...",
        "title": "Daily Greeting",
        "avgScore": 8.0,
        "completionsCount": 350
      }
    ],
    "difficultLessons": [
      {
        "lessonId": "b1b85f64...",
        "title": "Business Negotiation 01",
        "avgScore": 2.5,
        "completionsCount": 50
      }
    ]
  },
  "payments": {
    "totalRevenue": 20000000,
    "revenueByTier": [
      { "key": "Premium", "amount": 15000000 },
      { "key": "Basic", "amount": 5000000 }
    ],
    "revenueByBillingCycle": [
      { "key": "Monthly", "amount": 18000000 },
      { "key": "Lifetime", "amount": 2000000 }
    ]
  },
  "alerts": [
    {
      "code": "expiring_subscriptions",
      "title": "Subscription sắp hết hạn",
      "message": "Có subscription active sẽ hết hạn trong 30 ngày.",
      "count": 18,
      "severity": "warning"
    }
  ]
}
```
- **Frontend rule:** Render theo từng nhóm `summary`, `users`, `lessons`, `exercises`, `vocabulary`, `subscriptions`, `payments`, `blogs`, `notifications`, `engagement`, `alerts`.
- **Frontend rule:** `lessons.accessStats` là số lượt mở bài được ghi bằng tracker khi user đã đăng nhập mở lesson detail hoặc segments.
- **Frontend rule:** Không tự suy đoán doanh thu theo tên gói. Backend đã trả `revenueByTier` và `revenueByBillingCycle` từ dữ liệu thật của `SubscriptionPlan` + `Payment`.
- **Frontend rule:** Dùng các field trạng thái trả từ server (`reviewStatus`, `byStatus`, `byType`, `byChannel`, `byTier`) để đổ chart và filter, tránh map lại bằng logic FE.
- **Lỗi:**
  - `403 Forbidden` nểu Tài khoản gọi API không phải Admin.

### 4.2 Lấy danh sách Người Cài Đặt (List Users)
- **Endpoint:** `GET /api/admin/users`
- **Query Params:**
  - `page` (default: 1)
  - `pageSize` (default: 20)
  - `search` (Tên hoặc email)
  - `status` (Active / Banned)
  - `role` (ví dụ: User, Admin)
- **Response:** (Đã có sẵn dạng phân trang Pagination).
