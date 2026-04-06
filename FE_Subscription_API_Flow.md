# Hướng Dẫn Tích Hợp Flow Subscription & Payment (Dành cho Frontend Team)

Tài liệu này mô tả chi tiết luồng xử lý và cách gọi các endpoint API liên quan đến Subscription, Thanh toán (SEPay) và Quản trị Payment mà Backend vừa hoàn thành.

---

## 1. Flow Đăng Ký Premium (User App)

### Bước 1: Hiển thị các gói đăng ký
Frontend gọi API lấy danh sách Subscription Plan (chỉ hiển thị những gói `isActive: true`).

- **Endpoint:** `GET /api/subscription-plans`
- **Auth:** Không bắt buộc (hoặc Authorization tùy cấu hình)

### Bước 2: User chọn gói và thanh toán
Khi user ấn "Mua gói", Frontend tiến hành tạo một yêu cầu đăng ký.

- **Endpoint:** `POST /api/subscription-plans/subscribe`
- **Auth:** Bắt buộc (Bearer Token)
- **Body:**
```json
{
  "planId": "guid-cua-goi-dang-ky",
  "autoRenew": false,
  "paymentMethod": "sepay_bank"
}
```
- **Response trả về (Thành công 200 OK):**
```json
{
  "subscriptionId": "guid",
  "paymentId": "guid",
  "paymentReference": "DEMIF123456789", 
  "amount": 99000,
  "currency": "VND",
  "planName": "Premium 1 Tháng",
  "status": "PendingPayment"
}
```

> **🔥 LƯU Ý DÀNH CHO FE:** 
> - Nếu user đã có gói đang `Active` -> API sẽ báo lỗi HTTP 409 (Conflict).
> - Nếu user đang có 1 giao dịch treo `PendingPayment` chờ gửi tiền (chưa quá 24h) -> API trả HTTP 409 yêu cầu hoàn tất giao dịch cũ hoặc chờ 24h để BE tự động hủy rác.
> - **Mã `paymentReference` (bắt đầu bằng `DEMIF...`) cực kỳ quan trọng.** FE cần hướng dẫn/chèn mã này vào phần **Nội Dung Chuyển Khoản** để Backend ghi nhận Webhook thành công. Cú pháp QR code SePay nên đính kèm mã này.

### Bước 3: Đợi Webhook xử lý (SEPay)
- Quá trình chuyển khoản thành công, SEPay sẽ bắn Webhook xuống Backend (BE tự xử lý).
- FE có thể **Polling (5s/lần)** lấy thông tin Subscription hiện tại của user để update giao diện sang chữ "Premium", hoặc yêu cầu User ấn nút "Tôi đã thanh toán xong" để FE fetch lại Data.

### Bước 4 (Tùy chọn): User Hủy Giao Dịch
- Trong lúc đang chờ thanh toán (Pending), nếu user không muốn chuyển tiền nữa / gặp lỗi và muốn tạo đơn mới, FE gọi API Hủy để gỡ bỏ trạng thái Pending.
- **Endpoint:** `POST /api/payments/{referenceCode}/cancel`
- **Auth:** Bắt buộc (Bearer Token)
- **Response trả về (Thành công 200 OK):**
```json
{
  "success": true,
  "message": "Đã hủy đơn thanh toán thành công."
}
```
*(Sau khi gọi thành công, User có thể vào lại danh sách Plan để đăng ký gói mới bình thường).*

---

## 2. API Quản Lý Tài Khoản Cá Nhân (Me)

### A. Lấy thông tin gói Data hiện tại
- **Endpoint:** `GET /api/me/subscription`
- **Auth:** Bearer Token
- **Response:**
```json
{
  "hasActiveSubscription": true,
  "subscription": {
    "planName": "Premium 1 Tháng",
    "tier": "Premium",
    "billingCycle": "Monthly",
    "startDate": "2026-04-06T15:00:00Z",
    "endDate": "2026-05-06T15:00:00Z",
    "status": "Active", // Hoặc "PendingPayment"
    "daysRemaining": 30
  }
}
```
> **Đã fix bug:** Bây giờ nếu đang `PendingPayment`, thuộc tính Subscription này vẫn sẽ trả ra dữ liệu (với `status: PendingPayment`) kèm thông tin cho FE biết. FE có thể khéo léo thông báo "Bạn có 1 giao dịch chưa xong".

### B. Lịch sử giao dịch của user
- **Endpoint:** `GET /api/me/payment-history`
- **Auth:** Bearer Token
- **Response update:** Mảng mảng Item giờ đã có thêm cờ `"planName"` theo từng Payment.
```json
{
  "items": [
    {
      "id": "guid",
      "referenceCode": "DEMIF...",
      "amount": 99000,
      "planName": "Premium 1 Tháng", /* NEW */
      "status": "Completed", 
      "createdAt": "...",
      "completedAt": "..."
    }
  ]
}
```

---

## 3. Quản Trị Hệ Thống Thanh Toán (Admin UI)

Chỉ có tài khoản quyền `Admin` mới gọi được các Endpoint này.

### A. Lấy danh sách giao dịch toàn hệ thống có Filter
- **Endpoint:** `GET /api/admin/payments`
- **Query Params (Tất cả đều Optional):**
  - `page` (int, default: 1)
  - `pageSize` (int, default: 20)
  - `status` (string: Pending, Completed, Failed, Refunded, Cancelled)
  - `search` (string: Tìm theo Email User, Username, hoặc PaymentReference Code)
  - `dateFrom` (ISO Date: vd `2026-01-01T00:00:00`)
  - `dateTo` (ISO Date)

- **Response:**
```json
{
  "items": [
    {
      "id": "guid",
      "userEmail": "user@gmail.com",
      "userName": "tester1",
      "planName": "Premium 6 Tháng",
      "amount": 490000,
      "currency": "VND",
      "status": "Completed", // Enum text
      "paymentReference": "DEMIF...",
      "createdAt": "..."
    }
  ],
  "totalCount": 100,
  "totalPages": 5,
  "page": 1,
  "pageSize": 20
}
```

### B. Xem chi tiết giao dịch 
- **Endpoint:** `GET /api/admin/payments/{id}`

### C. Hoàn Tiền (Refund thủ công)
Khi khách khiếu nại, Admin lên app Bank cá nhân chuyển khoản trả khách, sau đó vào CMS Admin gọi API này để hệ thống đánh dấu đơn đã Refund + Lưu Note.

- **Endpoint:** `POST /api/admin/payments/{id}/refund`
- **Body:**
```json
{
  "reason": "Chuyển nhầm / Khách yêu cầu hủy"
}
```
*(Chỉ được refund các đơn vị có status = `Completed`)*

---

## 4. Các Lưu Ý Về Fix Bug Mới Cho FE team

✅ **Sửa triệt để vụ Lỗi `401 Unauthorized` ở API Dication / Shadowing / Lesson:**
Lỗi ngày xưa do Frontend lấy JWT token chuẩn xài Claim Type tên là `sub` nhưng Controller cũ của BE (như LessonController, DictationController...) lại đọc sai biến. Hôm nay BE đã gom lại dùng Core `ICurrentUserService` thống nhất.
* FE **không cần thay đổi cách login và cách truyền Token**, mọi request về Lesson/Shadowing giờ sẽ được Auth pass suôn sẻ 100%.

✅ **Dọn dẹp bộ đệm:** BE đã tích hợp **Background Job chạy ngầm**. FE không cần lo user treo Payment rác làm kẹt Database. Đúng 24h BackgroundJob sẽ vào tự chém sạch thẻ PendingPayment của user đi. User Role Premium cũng bị vô hiệu hóa chuẩn xác theo giờ EndDate mà không dư ra giây nào.
