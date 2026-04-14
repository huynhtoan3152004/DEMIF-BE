# 📑 Tài liệu Tích hợp API: Audio & Gamification (v1.0)

Tài liệu này tổng hợp các thay đổi về API (Endpoint, Request/Response) sau đợt cập nhật tính năng Audio Upload, Progress Tracking, và hệ thống tính điểm XP.

---

## 1. Upload Audio Lesson (Admin)
Hệ thống đã hỗ trợ linh hoạt hơn trong việc nhận file từ FE, không còn bắt buộc tên field cố định.

- **Endpoint:** `POST /api/admin/lessons/audio/upload`
- **Method:** `POST`
- **Content-Type:** `multipart/form-data`
- **Tên tham số file (Field Name) hỗ trợ:** `AudioFile`, `audioFile`, `File`, `file`, `audio`, `mp3File`.
- **Định dạng hỗ trợ:** `.mp3`, `.wav`, `.ogg`, `.m4a`, `.aac`, `.wma`, `.flac`.
- **Response:**
  ```json
  {
      "url": "https://res.cloudinary.com/...",
      "publicId": "audio/lessons/...",
      "format": "mp3",
      "duration": 125.5
  }
  ```

---

## 2. Lấy TIẾN ĐỘ chi tiết trong 1 bài học
Dùng để hiển thị trạng thái của từng đoạn (segment) trong bài học (câu nào đã làm, điểm cao nhất).

- **Endpoint:** `GET /api/lessons/{id}/my-progress`
- **Method:** `GET`
- **Authentication:** Required (Bearer Token)
- **Response Schema:**
  ```json
  {
      "lessonId": "guid",
      "lessonTitle": "Lesson Title",
      "totalSegments": 10,
      "completedCount": 4,
      "progressPercent": 40.0,
      "status": "InProgress", // NotStarted, InProgress, Completed
      "lastSegmentIndex": 3,
      "completedAt": "2024-04-14T...", // nullable
      "completedSegments": [
          {
              "segmentIndex": 0,
              "bestScore": 95.5,
              "attempts": 2
          },
          {
              "segmentIndex": 1,
              "bestScore": 88.0,
              "attempts": 1
          }
      ]
  }
  ```

---

## 3. Lấy LỊCH SỬ học tập (Lesson History)
Danh sách các bài học người dùng đã từng tương tác, hỗ trợ phân trang và lọc theo trạng thái.

- **Endpoint:** `GET /api/me/lesson-history`
- **Method:** `GET`
- **Query Params:** 
  - `page`: mặc định 1
  - `pageSize`: mặc định 20
  - `status`: lọc theo `NotStarted`, `InProgress`, `Completed` (Optional)
- **Response Schema:**
  ```json
  {
      "items": [
          {
              "lessonId": "guid",
              "title": "Introduction to English",
              "level": "Beginner",
              "status": "Completed",
              "completedSegments": 5,
              "avgScore": 85.0,
              "bestScore": 100.0,
              "startedAt": "2024-04-10T..."
          }
      ],
      "page": 1,
      "pageSize": 20,
      "totalCount": 15,
      "totalPages": 1
  }
  ```

---

## 4. Hệ thống XP (Gamification)
XP được tính toán và cộng tự động trên Server khi gọi các API tương tác bài học.

### a. Level Segment (Check Segment)
- **Quy tắc:** +1 XP cho mỗi segment mới hoàn thành lần đầu.
- **Bonus:** +10 XP khi hoàn thành tất cả các segment của một bài học (Lesson Completion).

### b. Level Lesson (Submit Dictation)
- **Quy tắc:** `XP = Score / 10` (Ví dụ: 85 điểm -> 8 XP). Tối thiểu luôn là 1 XP nếu có nộp bài.

---

## 5. Chính sách Audio = Premium
Lưu ý cho hiển thị UI:
- **Tất cả bài học Audio đều là Premium:** Lessons có `MediaType == "audio"` sẽ có trường `isPremiumOnly = true`.
- FE nên hiển thị icon Badge hoặc Khóa cho các bài học này đối với User thường.

---

## 6. Leaderboard (Bảng xếp hạng)
- **Endpoint:** `/api/me/stats/leaderboard`
- **Thay đổi:** Đã loại bỏ hoàn toàn các tài khoản **Admin** và **Moderator**. Dữ liệu trả về chỉ gồm người dùng thật sự giúp tăng tính cạnh tranh công bằng.
