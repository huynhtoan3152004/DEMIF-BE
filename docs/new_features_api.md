# Tài Liệu Cập Nhật API (Tính Năng Mới)

## 1. Leaderboard & Streak

- **Tính Năng Đăng Nhập Tự Động Tăng Streak**: Tính năng này chạy ngầm phía server. Mỗi khi FE gọi `POST /api/auth/login` hoặc `POST /api/auth/google-login`, Backend tự động cập nhật Streak của User tại thời điểm đó nếu khác ngày. Không cần thay đổi từ phía FE cho 2 API này.
  
### Lấy bảng xếp hạng
- **URL**: `GET /api/me/stats/leaderboard`
- **Auth**: Cần `Bearer Token`.
- **Query Params**: `limit` (int, default = 10). Mặc định lấy top 10.
- **Response**:
```json
[
  {
    "userId": "guid",
    "username": "Nguyen Van A",
    "avatarUrl": "https://...",
    "currentStreak": 15,
    "totalPoints": 5000,
    "level": "Advanced"
  }
]
```

## 2. Lesson Progress Tracker

Tính năng theo dõi xem user học tới đâu (cụ thể hỗ trợ ngắt quãng và quay lại bài nghe).

### Cập nhật / Đồng bộ tiến trình bài học (Save Progress)
- **Hành vi**: Gọi liên tục khi chuyển đoạn (segment) hoặc khi user pause, hoặc gọi định kỳ (5-10s/lần) lúc đang làm bài. FE không nhất thiết gửi nguyên dictation answers.
- **URL**: `POST /api/lessons/{id}/sync-progress`
- **Auth**: Cần `Bearer Token`.
- **Body Request**:
```json
{
  "segmentIndex": 2, // Index của đoạn đang nghe dở hoặc đã làm
  "isCompleted": false // Chuyền true nếu user nhấn Next/Hoàn thành tới cuối bài
}
```
- **Response** (200 OK):
```json
{
  "userId": "guid",
  "lessonId": "guid",
  "status": "InProgress", // Started, InProgress, Completed
  "lastSegmentIndex": 2
}
```

## 3. Thống Kê Giao Dịch (Admin)

### Tổng quan thống kê doanh thu
- **URL**: `GET /api/admin/payments/stats`
- **Auth**: Cần `Bearer Token` với Role `Admin`.
- **Response**:
```json
{
  "totalRevenue": 20000000, 
  "currentMonthRevenue": 5000000,
  "totalTransactions": 150,
  "currency": "VND"
}
```

## 4. Danh sách người dùng đăng ký (Admin)
- Tính năng này đã tồn tại ở **URL**: `GET /api/admin/users`.
- Hỗ trợ gọi với Query Param truyền vào (search, filter, page, pageSize, v.v.).
