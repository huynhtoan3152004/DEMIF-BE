## 🧠 Brainstorm: Các Tính Năng Còn Thiếu & Đề Xuất Hệ Thống

### Bối cảnh
Người dùng (User) cần:
1. **Bảng xếp hạng Streak (Streak Leaderboard)**.
2. **Ghi nhận Streak tự động**: Khi người dùng đăng nhập hoặc khi làm xong bài học.
3. **Thống kê thanh toán người dùng**: Xem hệ thống đã có chưa.
4. **Tiến độ bài học (Lesson Progress)**: Ghi nhận trạng thái học của từng bài (chưa học, đang học tới đâu, đã hoàn thành).

---

### Phân tích hiện tại & Những gì còn thiếu (Gap Analysis)

#### 1. Ghi nhận & Bảng xếp hạng Streak
- **Hiện trạng**: Đã có Entity `UserStreak` và `RecordActivityService`. Tuy nhiên, `LoginService` hiện tại truy cập vào hệ thống chỉ cập nhật `LastLoginAt` mà KHÔNG gọi cập nhật Streak. Leaderboard cũng chưa tồn tại.
- **Thiếu sót trải nghiệm**: Người dùng mở app lên nhưng không làm bài tập thì không được cộng Streak. Bảng xếp hạng chưa có để tạo động lực.
- **Đề xuất**:
  - Gắn logic gọi `UserStreak` ngay trong `LoginService` (hoặc tạo ra 1 Event / Service dùng chung để tăng Streak khi Login).
  - Tạo Endpoint `GET /api/me/stats/leaderboard` lấy Top N users có `CurrentStreak` hoặc `TotalPoints` cao nhất.

#### 2. Thống kê tất cả thanh toán
- **Hiện trạng**: Đã kiểm tra thấy **có tồn tại** tính năng này trong file `AdminPaymentService.cs`. Tính năng hỗ trợ `GetAllAsync` với phân trang, lọc theo status, search user, date.
- **Thiếu sót trải nghiệm**: Có danh sách nhưng có thể thiếu "Thống Kê Tổng Quan" (Tổng doanh thu, Tổng số tiền tháng này, v.v.).
- **Đề xuất**: 
  - Tận dụng `AdminPaymentService` hiện tại.
  - Sẽ thêm 1 Dashboard Stats nhỏ trả về `Total Revenue`, `Total Subscribers` nếu cần thiết (Tùy chọn).

#### 3. Tiến độ từng bài học (Lesson Progress)
- **Hiện trạng**: Hiện tại khi học sinh làm Dictation/Shadowing, hệ thống có lưu vào bảng `UserExercise`. Bảng `UserProgress` lưu tổng số bài đã hoàn thành. NHƯNG hệ thống không có "Trạng thái bài học" cho từng User (Ví dụ: Đang học dở Câu số 5 thì thoát app ra).
- **Thiếu sót trải nghiệm**: Mở lại bài học không biết học tới đâu. Không có thanh Progress Bar hiển thị % hoàn thành của từng bài học trên danh sách.
- **Đề xuất**:
  - Bổ sung Entity `UserLessonTracker` (hoặc bổ sung properties vào `UserExercise`). Tuy nhiên tạo `UserLessonTracker` sẽ chuẩn hơn: `UserId`, `LessonId`, `Status (Started, InProgress, Completed)`, `LastSegmentIndex`.
  - Tạo Endpoint `POST /api/lessons/{id}/progress` để frontend Ping (sync) tiến độ 10 giây/lần hoặc khi user chuyển câu.
  - Cập nhật lại logic `SubmitDictationService` để khi hoàn thành toàn bài sẽ trigger đổi `Status => Completed` trong `UserLessonTracker`.

---

### Đề xuất Kế Hoạch (Recommendation)

- **Bước 1 (Auth & Streak)**: Cập nhật `LoginService` để auto-bump Streak. Thêm List Top Streak. Khuyến khích cạnh tranh.
- **Bước 2 (Lesson Progress)**: Tạo bảng/Entity `UserLessonTracker`, apply DB Migration. Tạo logic Tracking tiến độ mỗi khi user nghe đến segment nào đó.
- **Bước 3 (Admin Stats)**: Verify endpoint Admin Payment, tạo Markdown Documentation cho FE.

=> Kế hoạch này giúp hoàn thiện vòng lặp: **Người dùng vào app -> Có Streak -> Học bài dở dang -> Có lưu tiến độ -> Làm xong -> Cộng điểm Progress & Activity**.
