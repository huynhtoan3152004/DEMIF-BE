# 🗺️ DEMIF Backend — Roadmap & Gap Analysis

> Phân tích dựa trên source code thực tế tại `src/`. Cập nhật: 05/03/2026.
> Shadowing MVP được **loại trừ** khỏi scope hiện tại theo quyết định của team.

---

## 📦 HIỆN TRẠNG — Đã Implement

### ✅ Auth (`/api/auth`)
| Endpoint | Service | Ghi chú |
|---|---|---|
| `POST /register` | `RegisterService` | Email + password |
| `POST /login` | `LoginService` | JWT + Refresh Token |
| `POST /google` | `GoogleAuthService` | OAuth 2.0 |
| `POST /refresh` | `RefreshTokenService` | |
| `POST /logout` | `LogoutService` | |
| `GET /verify-email` | `VerifyEmailService` | Token qua email |

### ✅ Lessons (`/api/lessons`)
| Endpoint | Ghi chú |
|---|---|
| `GET /api/lessons` | Danh sách, pagination, filter level/type/category |
| `GET /api/lessons/{id}` | Chi tiết, kiểm tra premium access |
| `GET /api/lessons/{id}/segments?level=` | Timed segments, LevelConfig (maxReplays, showTranscript) |
| `GET /api/lessons/{id}/dictation?level=` | Exercise với blanks (answers đã bị strip) |
| `POST /api/lessons/{id}/dictation/submit` | Submit toàn bài, chấm điểm, lưu UserExercise |
| `POST /api/lessons/{id}/segments/{i}/check` | Check 1 segment tự do (LCS word-by-word diff, luôn trả transcript) |

**Core algorithm**: LCS (Longest Common Subsequence) word alignment — handle từ bị skip/thêm thừa.
**Lưu ý**: Mỗi lần gọi `CheckSegment` tạo 1 bản ghi `UserExercise` mới (xem Gap #2).

### ✅ Me (`/api/me`)
| Endpoint | Ghi chú |
|---|---|
| `GET /api/me/progress` | TotalPoints, Level, LessonsCompleted, AvgScore |
| `GET /api/me/streak` | CurrentStreak, LongestStreak, FreezesAvailable |
| `POST /api/me/activity` | Cộng điểm + cập nhật streak sau khi hoàn thành bài |

### ✅ Subscriptions & Payments
| Endpoint | Ghi chú |
|---|---|
| `GET /api/subscription-plans` | Danh sách gói |
| `POST /api/subscription-plans/{id}/subscribe` | Tạo `PendingPayment` + trả `referenceCode` |
| `GET /api/subscription-plans/my-subscription` | Subscription hiện tại của user |
| `DELETE /api/subscription-plans/cancel` | Hủy subscription |
| `POST /api/payments/sepay/webhook` | SEPay webhook callback → activate subscription |

### ✅ Profile (`/api/profile`)
| Endpoint | Ghi chú |
|---|---|
| `GET /api/profile/me` | Xem profile |
| `PUT /api/profile/me` | Cập nhật profile |

### ✅ Admin
| Group | Endpoints |
|---|---|
| `AdminBlogsController` | CRUD `/api/admin/blogs` |
| `AdminLessonsController` | CRUD + YouTube import `/api/admin/lessons` |
| `AdminSubscriptionPlansController` | CRUD gói subscription |
| `AdminUserSubscriptionsController` | List, detail, extend, cancel user subscriptions |
| `UsersController` | `/api/admin/users` — quản lý người dùng |

---

## 🔴 GAP ANALYSIS — Những Gì Còn Thiếu

### 1. 🎯 DICTATION — Không Có Session / Resume

**Vấn đề hiện tại:**
```
User đang làm bài tới segment 7/15 → đóng app → vào lại → phải làm lại từ đầu
```

Hiện tại `CheckSegment` lưu `UserExercise` cho mỗi lần check, nhưng không có cơ chế lưu "phiên làm bài đang dở" riêng biệt. Frontend không có endpoint để hỏi *"user đang làm dở bài này đến segment nào rồi?"*

**Cần thêm:**
```
GET  /api/lessons/{id}/dictation/session           → Lấy tiến độ dở (segment hiện tại, answers đã điền)
POST /api/lessons/{id}/dictation/session/save      → Auto-save từng segment
POST /api/lessons/{id}/dictation/session/complete  → Đánh dấu hoàn thành, xóa session
POST /api/lessons/{id}/dictation/session/abandon   → Hủy session
```

**Domain thêm:**
```csharp
// New entity
public class DictationSession : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public string Level { get; set; }   // Beginner/Intermediate/...
    public int CurrentSegmentIndex { get; set; }
    public string? SavedAnswers { get; set; }  // JSON: { "segmentIndex": { "1": "answer", ... } }
    public DateTime StartedAt { get; set; }
    public DateTime LastSavedAt { get; set; }
    public bool IsCompleted { get; set; }
}
```

---

### 2. 📊 DICTATION — Duplicate UserExercise Records

**Vấn đề hiện tại:**
```
User check segment 3 → lần 1 sai → check lại → lần 2 đúng
→ Trong DB có 2 bản ghi UserExercise cho cùng user + lesson + segment
→ Statistics bị sai, không biết kết quả cuối cùng của segment là gì
```

**Nguyên nhân**: `CheckSegmentService.SaveExerciseAsync()` tạo `new UserExercise` mỗi lần, không check existing.

**Cần fix:**
```csharp
// Trong CheckSegmentService.SaveExerciseAsync():
// Thay vì INSERT luôn, cần UPSERT theo (UserId, LessonId, SegmentIndex, ExerciseType)
// Hoặc lưu attempts count + best score riêng biệt
var existing = await _dbContext.UserExercises
    .FirstOrDefaultAsync(e => e.UserId == userId 
        && e.LessonId == lessonId 
        && e.SegmentIndex == segmentIndex  // cần thêm field này vào entity
        && e.ExerciseType == ExerciseType.Dictation);

if (existing != null)
{
    existing.Attempts++;
    existing.Score = Math.Max(existing.Score, newScore); // giữ best score
    _dbContext.UserExercises.Update(existing);
}
else
{
    _dbContext.UserExercises.Add(newExercise);
}
```

**Domain cần thêm field:**
```csharp
// UserExercise.cs — thêm:
public int? SegmentIndex { get; set; }  // null = toàn bài, có số = check từng segment
```

---

### 3. 📈 LESSON — Không Có Per-Lesson Statistics

**Vấn đề hiện tại:**
```
User làm xong bài → không biết mình đang ở đâu:
- Lần tốt nhất đạt bao nhiêu điểm?
- Accuracy trend có tăng không?
- Segment nào khó nhất?
```

**Cần thêm:**
```
GET /api/lessons/{id}/my-stats
```

**Response:**
```json
{
  "lessonId": "...",
  "totalAttempts": 5,
  "bestScore": 92.5,
  "lastScore": 88.0,
  "averageScore": 85.2,
  "firstCompletedAt": "2026-02-01T10:00:00Z",
  "lastCompletedAt": "2026-03-05T15:30:00Z",
  "totalTimeSpentSeconds": 1240,
  "accuracyTrend": [65.0, 72.0, 80.0, 88.0, 92.5],
  "hardestSegmentIndex": 4,
  "segmentAccuracies": [95, 88, 90, 72, 65, 98]
}
```

---

### 4. 💡 DICTATION — Không Có Hint System

**Vấn đề hiện tại:**
```
User bí từ khó → không có gợi ý → bỏ cuộc hoặc cheat bằng cách tua audio liên tục
```

**Cần thêm:**
```
POST /api/lessons/{id}/segments/{i}/hint
Body: { "level": "Beginner", "hintLevel": 1 }
```

**Logic hint 3 cấp:**
```
Level 1: Hé lộ chữ cái đầu    → "H___ y__ are ___king"     (-5% score)
Level 2: Hé lộ 50% ký tự      → "He__o you are tal___g"    (-15% score)
Level 3: Reveal toàn bộ từ    → "Hello you are talking"    (-30% score)
```

---

### 5. 💳 PAYMENT — Không Có Flow Tạo Payment Link

**Vấn đề nghiêm trọng nhất:**
```
Flow hiện tại:
1. User gọi POST /api/subscription-plans/{id}/subscribe
2. Backend tạo Payment với status=PendingPayment, trả về referenceCode
3. User... không biết làm gì tiếp theo ❌

SEPay cần: User chuyển khoản đúng nội dung = referenceCode
Nhưng app không hiển thị QR/số tài khoản/hướng dẫn!
```

**Cần thêm:**
```
GET /api/payments/sepay/info/{referenceCode}
→ Trả về: số tài khoản ngân hàng, tên người nhận, số tiền, nội dung CK, QR code URL
→ Frontend dùng để hiển thị màn hình "Thanh toán"

GET /api/payments/{referenceCode}/status
→ User polling để biết thanh toán đã được xác nhận chưa
→ Hoặc dùng WebSocket/SSE để push

GET /api/me/payment-history
→ Lịch sử tất cả giao dịch của user
```

**Cấu hình SEPay cần thêm vào appsettings:**
```json
"SEPay": {
  "BankCode": "VCB",
  "AccountNumber": "1234567890",
  "AccountName": "CONG TY DEMIF",
  "WebhookSecret": "...",
  "QrBaseUrl": "https://img.vietqr.io/image/{bank}-{account}-compact.png"
}
```

---

### 6. 🏷️ ME — Thiếu Endpoint Subscription

**Vấn đề hiện tại:**
```
MeController chỉ có: progress, streak, activity
Không có: subscription status, payment history
```

`GetMySubscriptionService` đã tồn tại nhưng được gọi từ `SubscriptionPlansController`, không phải `MeController`.

**Cần thêm vào MeController:**
```
GET /api/me/subscription         → Subscription hiện tại (tier, endDate, autoRenew, daysLeft)
GET /api/me/payment-history      → Lịch sử thanh toán
PATCH /api/me/preferences        → Cập nhật settings (dailyGoalMinutes, defaultPlaybackSpeed)
GET /api/me/profile              → Tổng hợp profile + subscription + stats trong 1 lần gọi
```

---

### 7. ❄️ STREAK FREEZE — Chưa Implement

**Vấn đề hiện tại:**
```
Entity UserStreak đã có:
  public int FreezeCount { get; set; }
  public int FreezesAvailable { get; set; } = 1;

Nhưng không có endpoint nào để dùng freeze!
RecordActivityService cũng không award freeze theo milestone streak.
```

**Cần thêm:**
```
POST /api/me/streak/freeze       → Dùng 1 freeze để bảo vệ streak hôm nay
                                   Điều kiện: FreezesAvailable > 0, chưa học hôm nay
```

**Logic award freeze:**
```csharp
// Trong RecordActivityService, sau khi update streak:
// Mỗi 7 ngày streak liên tiếp → +1 FreezesAvailable (tối đa 3)
if (streak.CurrentStreak % 7 == 0 && streak.FreezesAvailable < 3)
    streak.FreezesAvailable++;
```

---

### 8. 🏆 LEADERBOARD — Chưa Có Gì

**Vấn đề:**
```
Không có yếu tố cạnh tranh → user thiếu động lực học lại
```

**Cần thêm:**
```
GET /api/leaderboard/weekly      → Top 50 điểm tuần này (reset mỗi thứ 2)
GET /api/leaderboard/alltime     → Top 100 mọi thời đại
GET /api/lessons/{id}/leaderboard → Top 10 accuracy của bài học cụ thể
```

**Response:**
```json
{
  "rank": 12,
  "myScore": 3240,
  "entries": [
    { "rank": 1, "username": "user1", "avatarUrl": "...", "score": 5200 },
    ...
  ],
  "resetAt": "2026-03-09T00:00:00Z"
}
```

---

### 9. 📚 VOCABULARY — Chưa Có

**Mô tả:**
```
User học bài → có từ khó muốn lưu lại → không có chỗ lưu
User sai từ trong dictation → từ đó không được track để ôn lại
```

**Domain cần thêm:**
```csharp
public class UserVocabulary : BaseEntity
{
    public Guid UserId { get; set; }
    public string Word { get; set; }
    public string? Definition { get; set; }
    public string? ExampleSentence { get; set; }
    public Guid? LessonId { get; set; }     // nguồn gốc từ bài nào
    public int CorrectCount { get; set; }   // spaced repetition
    public int WrongCount { get; set; }
    public DateTime? NextReviewAt { get; set; }  // SRS schedule
}
```

**Endpoints:**
```
GET  /api/me/vocabulary                    → Từ điển cá nhân (pagination)
POST /api/me/vocabulary                    → Lưu từ mới
DELETE /api/me/vocabulary/{id}             → Xóa từ
GET  /api/me/vocabulary/review             → Lấy N từ cần ôn hôm nay (SRS)
POST /api/me/vocabulary/{id}/review        → Báo cáo kết quả ôn (correct/wrong)
GET  /api/lessons/{id}/vocabulary          → Từ vựng tự động extract từ transcript
```

---

### 10. 🔔 NOTIFICATIONS — Chưa Có

**Mô tả:**
```
Admin gia hạn subscription → user không biết
Streak sắp hết → user không nhận cảnh báo (trong app)
```

**Domain cần thêm:**
```csharp
public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public string Type { get; set; }      // streak_warning, subscription_expiring, lesson_recommended...
    public string Title { get; set; }
    public string Message { get; set; }
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
```

**Endpoints:**
```
GET  /api/me/notifications               → Danh sách (unread first)
PATCH /api/me/notifications/{id}/read   → Đánh dấu đã đọc
POST /api/me/notifications/read-all     → Đọc tất cả
GET  /api/me/notifications/unread-count → Badge count cho FE
```

---

### 11. 📊 ADMIN — Dashboard Analytics

**Cần thêm:**
```
GET /api/admin/analytics
→ Summary cards: users, lessons, exercises, vocabulary, subscriptions, payments, blogs
→ Breakdown: users by status/auth/level, lessons by status/type/level/category, payments by status/method/tier
→ Top difficult/popular lessons, top vocabulary topics, top users, alerts
```

---

### 12. 🤖 RECOMMENDATIONS — Chưa Có Table / Endpoint

**Từ kế hoạch n8n**: n8n sẽ fill bảng `RecommendedLessons` mỗi đêm, backend chỉ cần đọc.

**Domain cần thêm:**
```csharp
public class RecommendedLesson : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public string Reason { get; set; }      // "vocabulary_weakness", "level_match", "popular"
    public decimal Score { get; set; }      // relevance score từ n8n
    public DateTime GeneratedAt { get; set; }
    public bool IsShown { get; set; }
}
```

**Endpoint:**
```
GET /api/me/recommendations    → Trả về 5-10 bài học được gợi ý hôm nay
```

---

## 📊 Ma Trận Ưu Tiên

| # | Gap | Impact | Effort | Ưu tiên |
|---|---|---|---|---|
| 5 | Payment link / QR info + status polling | 🔴 Critical (ra tiền) | Low | **Sprint 1** |
| 6 | `GET /api/me/subscription` + payment history | 🔴 High | Very Low | **Sprint 1** |
| 2 | Fix duplicate UserExercise (UPSERT) | 🔴 Data integrity | Low | **Sprint 1** |
| 1 | Dictation session / resume | 🟠 UX retention | Medium | **Sprint 2** |
| 3 | Per-lesson statistics | 🟠 User engagement | Low | **Sprint 2** |
| 7 | Streak freeze endpoint | 🟠 Retention | Very Low | **Sprint 2** |
| 12 | RecommendedLessons table + endpoint | 🟠 Personalization | Low | **Sprint 2** |
| 4 | Hint system | 🟡 UX completion rate | Low | **Sprint 3** |
| 8 | Leaderboard | 🟡 Engagement | Medium | **Sprint 3** |
| 11 | Admin dashboard stats | 🟡 Operations | Medium | **Sprint 3** |
| 9 | Vocabulary + SRS | 🟢 Retention long-term | High | **Sprint 4** |
| 10 | Notifications | 🟢 Nice to have | Medium | **Sprint 4** |

---

## 🚀 Sprint Breakdown

### Sprint 1 — "Ra Tiền" (1-2 ngày)
> Mục tiêu: Flow thanh toán hoạt động end-to-end + data integrity

**1. Fix duplicate UserExercise**
- Thêm field `SegmentIndex` vào `UserExercise`
- Sửa `CheckSegmentService` → UPSERT thay vì INSERT
- Migration: `dotnet ef migrations add AddSegmentIndexToUserExercise`

**2. Payment info endpoint**
```
GET  /api/payments/info/{referenceCode}   → QR, số TK, nội dung CK
GET  /api/payments/{referenceCode}/status → Polling kết quả
```

**3. Me subscription endpoints**
```
GET /api/me/subscription
GET /api/me/payment-history
```

---

### Sprint 2 — "Retention" (3-4 ngày)
> Mục tiêu: User quay trở lại học mỗi ngày

**4. Dictation Session/Resume**
- Entity `DictationSession`, migration
- 4 endpoints: get/save/complete/abandon session

**5. Per-lesson stats**
- `GET /api/lessons/{id}/my-stats`

**6. Streak freeze**
- `POST /api/me/streak/freeze`
- Award logic trong `RecordActivityService`

**7. Recommendations table + endpoint**
- Entity `RecommendedLesson`, migration
- `GET /api/me/recommendations`

---

### Sprint 3 — "Engagement" (3-4 ngày)
> Mục tiêu: Social + gamification

**8. Hint system**
- `POST /api/lessons/{id}/segments/{i}/hint`
- 3 cấp hint, trừ điểm tương ứng

**9. Leaderboard**
- `GET /api/leaderboard/weekly`
- `GET /api/leaderboard/alltime`
- `GET /api/lessons/{id}/leaderboard`

**10. Admin stats**
- `GET /api/admin/stats`

---

### Sprint 4 — "Long-term Retention" (1 tuần)
> Mục tiêu: User học đều đặn mỗi ngày

**11. Vocabulary + Spaced Repetition**
- Entity `UserVocabulary`
- 5 endpoints CRUD + review

**12. Notifications**
- Entity `Notification`
- 4 endpoints

---

## 🏗️ Database Changes Required

| Migration | Tables Thêm/Sửa | Sprint |
|---|---|---|
| `AddSegmentIndexToUserExercise` | `UserExercises.SegmentIndex` (nullable int) | 1 |
| `AddDictationSession` | `DictationSessions` (new table) | 2 |
| `AddRecommendedLessons` | `RecommendedLessons` (new table) | 2 |
| `AddUserVocabulary` | `UserVocabularies` (new table) | 4 |
| `AddNotifications` | `Notifications` (new table) | 4 |

---

## 📝 Ghi Chú Kỹ Thuật

### Settings cần bổ sung (SEPay)
```json
// appsettings.json
"SEPay": {
  "BankCode": "VCB",
  "AccountNumber": "...",
  "AccountName": "...",
  "WebhookSecret": "...",
  "QrTemplate": "https://img.vietqr.io/image/{bank}-{account}-compact.png?amount={amount}&addInfo={content}&accountName={name}"
}
```

### Pattern hiện tại cần giữ nguyên
- `Result.Failure<T>(error)` — static helper, KHÔNG phải `Result<T>.Failure`
- `Error.Validation(msg)`, `Error.NotFound(msg)`, `Error.Forbidden(msg)`
- `Error.Code` + `Error.Message` (không có `.Description`)
- Controller: `if (_currentUserService.UserId is not { } userId) return Unauthorized();`

### Shadowing (Đã loại khỏi scope)
Flow: `FE record audio → POST /api/lessons/{id}/segments/{i}/shadowing → Forward to Whisper API on VPS → LCS compare → Save UserExercise (ExerciseType.Shadowing)`
Cần deploy `whisper-api` Docker container trước khi implement.

---

*Generated from source code analysis — `src/` as of 05/03/2026*
