# 🎯 DEMIF — Phân Tích Chuyên Sâu & Kế Hoạch Cải Tiến Chiến Lược

> **Vai trò:** Senior System Architect  
> **Mục tiêu:** Phân tích hệ thống hiện tại, xác định gaps, đề xuất kiến trúc MVP thu hút người dùng  
> **Ngày:** 31/03/2026

---

## 📊 1. PHÂN TÍCH HIỆN TRẠNG HỆ THỐNG

### 1.1 Kiến Trúc Tổng Thể — Đánh Giá

```
✅ ĐIỂM MẠNH:
├── Clean Architecture chuẩn (Domain → Application → Infrastructure → Api)
├── Entity Framework Core + PostgreSQL (scalable)
├── JWT + Google Auth (đủ cho production)
├── Role-based Authorization (Admin, Staff, User, Premium)
├── SEPay integration (đã có webhook)
└── Code organization tốt (Feature-based folders)

⚠️ ĐIỂM CẦN CẢI THIỆN:
├── Thiếu comprehensive user analytics
├── Chưa có daily streak visualization
├── Thiếu learning path recommendations
├── Chưa có achievement/badge system
└── Thiếu admin dashboard analytics
```

### 1.2 Hệ Thống Lesson — Phân Tích Chi Tiết

#### **Dictation System (Hiện Tại)**

```
Flow: YouTube URL → yt-dlp → Whisper API → TimedTranscript → DictationTemplate
      ↓
      User chọn Level (Beginner/Intermediate/Advanced/Expert)
      ↓
      GetDictationExercise → trả blanks (không có đáp án)
      ↓
      User điền từ → SubmitDictation → chấm điểm → lưu UserExercise
      ↓
      Hoặc CheckSegment (từng segment) → LCS word-by-word diff
```

**Điểm mạnh:**
- ✅ DictationTemplateGenerator tự động tạo template từ transcript
- ✅ 4 levels với blankPercentage khác nhau (15% → 80%)
- ✅ LCS algorithm xử lý skip/insert gracefully
- ✅ UPSERT pattern tránh duplicate UserExercise
- ✅ Hint system 3 cấp (FirstLetterAndLength, FirstLetter, LengthOnly, None)

**Điểm yếu:**
- ❌ Không có DictationSession (user đóng app → mất tiến độ)
- ❌ Không có per-lesson statistics cho user
- ❌ Không có hint endpoint (chỉ có hint type trong template)
- ❌ Không có vocabulary extraction từ sai lầm

#### **Shadowing System (Hiện Tại)**

```
Flow: User nghe audio segment
      ↓
      Browser Web Speech API transcribes → gửi UserText lên backend
      ↓
      CheckShadowingService → LCS word-diff vs transcript
      ↓
      Trả accuracy + word-level feedback → lưu UserExercise
```

**Điểm mạnh:**
- ✅ Text-fallback mode (không cần whisper.cpp cho MVP)
- ✅ Reuse LCS algorithm từ Dictation
- ✅ Word-level feedback (correct/wrong/skipped)
- ✅ Pass threshold 70%

**Điểm yếu:**
- ❌ Không có audio mode (whisper.cpp chưa integrate)
- ❌ Không có pronunciation scoring (chỉ word accuracy)
- ❌ Không có fluency/intonation analysis
- ❌ Không có recording playback để user tự review

### 1.3 Hệ Thống Progress & Streak — Phân Tích

#### **UserProgress (Hiện Tại)**

```csharp
// Đã có:
TotalPoints, TotalMinutes, LessonsCompleted
DictationCompleted, ShadowingCompleted
AvgDictationScore, AvgShadowingScore
CurrentLevel, LevelProgress (%)
Skills JSON (listening, speaking, vocabulary, grammar)
```

**Đánh giá:** ✅ Đủ cho MVP, nhưng thiếu:
- Weekly/monthly trends
- Skill breakdown chi tiết
- Comparison với community average

#### **UserStreak (Hiện Tại)**

```csharp
// Đã có:
CurrentStreak, LongestStreak
LastActiveDate, TotalActiveDays
FreezeCount, FreezesAvailable
```

**Đánh giá:** ✅ Core logic đã có, nhưng thiếu:
- Streak freeze endpoint (chưa implement)
- Award freeze theo milestone (mỗi 7 ngày)
- Streak calendar visualization data
- Streak reminder notifications

#### **RecordActivityService (Hiện Tại)**

```csharp
// Logic:
BasePointsPerSession = 10
PointsPerScorePercent = 1
Level thresholds: Beginner(0), Intermediate(500), Advanced(1500), Expert(3500)

// Cập nhật:
- Cộng điểm vào TotalPoints
- Tăng LessonsCompleted
- Update AvgDictationScore/AvgShadowingScore (rolling average)
- ComputeLevel + LevelProgress
- Update streak (giữ/tăng/reset)
```

**Đánh giá:** ✅ Logic tốt, nhưng thiếu:
- Bonus points cho perfect score
- Bonus points cho streak milestones
- Daily goal tracking (DailyGoalMinutes)
- Activity details (loại bài nào, category nào)

---

## 🔴 2. GAP ANALYSIS — NHỮNG GÌ CÒN THIẾU

### 2.1 Critical Gaps (Ảnh hưởng đến Revenue & Retention)

| # | Gap | Impact | Effort | Priority |
|---|-----|--------|--------|----------|
| 1 | **User Analytics Dashboard** | 🔴 Critical | Medium | **Sprint 1** |
| 2 | **Daily Streak Visualization** | 🔴 Critical | Low | **Sprint 1** |
| 3 | **Per-Lesson Statistics** | 🔴 High | Low | **Sprint 1** |
| 4 | **Dictation Session/Resume** | 🟠 UX Retention | Medium | **Sprint 2** |
| 5 | **Streak Freeze Endpoint** | 🟠 Retention | Very Low | **Sprint 2** |

### 2.2 Important Gaps (Ảnh hưởng đến Engagement)

| # | Gap | Impact | Effort | Priority |
|---|-----|--------|--------|----------|
| 6 | **Achievement/Badge System** | 🟡 Engagement | Medium | **Sprint 3** |
| 7 | **Leaderboard** | 🟡 Social | Medium | **Sprint 3** |
| 8 | **Learning Recommendations** | 🟡 Personalization | Medium | **Sprint 3** |
| 9 | **Vocabulary Extraction** | 🟡 Retention | Low | **Sprint 3** |
| 10 | **Admin Dashboard Analytics** | 🟡 Operations | Medium | **Sprint 4** |

### 2.3 Nice-to-Have Gaps

| # | Gap | Impact | Effort | Priority |
|---|-----|--------|--------|----------|
| 11 | **Hint System Endpoint** | 🟢 UX | Low | **Sprint 4** |
| 12 | **Audio Mode (whisper.cpp)** | 🟢 Feature | High | **Sprint 5** |
| 13 | **Pronunciation Scoring** | 🟢 Feature | High | **Sprint 5** |
| 14 | **Notifications System** | 🟢 Nice | Medium | **Sprint 5** |

---

## 🏗️ 3. KIẾN TRÚC ĐỀ XUẤT — MVP THU HÚT NGƯỜI DÙNG

### 3.1 Chiến Lược MVP — "Quick Wins, Big Impact"

```
Mục tiêu: Thu hút người dùng với features visible, có giá trị ngay
Nguyên tắc: Đơn giản → Hiệu quả → Scale sau

Sprint 1 (1-2 tuần): "User Analytics & Streak" ← QUAN TRỌNG NHẤT
Sprint 2 (1-2 tuần): "Session & Retention"
Sprint 3 (2-3 tuần): "Gamification & Social"
Sprint 4 (1-2 tuần): "Admin & Operations"
```

### 3.2 Kiến Trúc Hệ Thống Analytics Mới

```
┌─────────────────────────────────────────────────────────────┐
│                    User Analytics Layer                       │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ UserStats   │  │ DailyGoal   │  │ SkillBreak  │         │
│  │ Service     │  │ Tracker     │  │ down        │         │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘         │
│         │                │                │                  │
│         ▼                ▼                ▼                  │
│  ┌─────────────────────────────────────────────────────┐   │
│  │           UserAnalytics Aggregate Root                │   │
│  │  - WeeklyStats                                        │   │
│  │  - MonthlyStats                                       │   │
│  │  - SkillProgress                                      │   │
│  │  - CategoryBreakdown                                  │   │
│  │  - AccuracyTrends                                     │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 Database Schema Mới — Bổ Sung

```sql
-- =====================================================
-- USER ANALYTICS (Bảng tổng hợp cho analytics)
-- =====================================================

CREATE TABLE UserAnalytics (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    
    -- Weekly stats (rolling 7 days)
    WeeklyMinutes INT DEFAULT 0,
    WeeklyExercises INT DEFAULT 0,
    WeeklyPoints INT DEFAULT 0,
    WeeklyAvgScore DECIMAL(5,2) DEFAULT 0,
    
    -- Monthly stats (rolling 30 days)
    MonthlyMinutes INT DEFAULT 0,
    MonthlyExercises INT DEFAULT 0,
    MonthlyPoints INT DEFAULT 0,
    MonthlyAvgScore DECIMAL(5,2) DEFAULT 0,
    
    -- Skill breakdown (JSON)
    -- {"dictation_accuracy": 85, "shadowing_accuracy": 78, 
    --  "vocabulary_strength": 70, "listening_comprehension": 80}
    SkillBreakdown NVARCHAR(MAX),
    
    -- Category performance (JSON)
    -- {"conversation": 85, "business": 72, "travel": 90}
    CategoryPerformance NVARCHAR(MAX),
    
    -- Accuracy trends (JSON array last 30 days)
    -- [85, 82, 88, 90, 87, 92, 95, ...]
    AccuracyTrend NVARCHAR(MAX),
    
    -- Daily streak calendar (JSON)
    -- {"2026-03-01": true, "2026-03-02": true, ...}
    StreakCalendar NVARCHAR(MAX),
    
    UpdatedAt DATETIME2,
    
    CONSTRAINT UQ_UserAnalytics_UserId UNIQUE (UserId)
);

CREATE INDEX IX_UserAnalytics_UserId ON UserAnalytics(UserId);

-- =====================================================
-- ACHIEVEMENTS (Hệ thống thành tích)
-- =====================================================

CREATE TABLE Achievements (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(50) NOT NULL,
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    IconUrl NVARCHAR(500),
    Points INT DEFAULT 0,
    Category NVARCHAR(30), -- streak, lessons, accuracy, vocabulary
    Requirement NVARCHAR(MAX), -- JSON condition
    Rarity NVARCHAR(20), -- common, rare, epic, legendary
    
    CONSTRAINT UQ_Achievements_Code UNIQUE (Code)
);

CREATE TABLE UserAchievements (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    AchievementId UNIQUEIDENTIFIER NOT NULL,
    UnlockedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT UQ_UserAchievements UNIQUE (UserId, AchievementId)
);

CREATE INDEX IX_UserAchievements_UserId ON UserAchievements(UserId);

-- =====================================================
-- DAILY GOALS (Mục tiêu hàng ngày)
-- =====================================================

CREATE TABLE UserDailyGoals (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    GoalDate DATE NOT NULL,
    
    TargetMinutes INT DEFAULT 30,
    ActualMinutes INT DEFAULT 0,
    TargetExercises INT DEFAULT 3,
    ActualExercises INT DEFAULT 0,
    
    GoalAchieved BIT DEFAULT 0,
    AchievedAt DATETIME2,
    
    CONSTRAINT UQ_UserDailyGoals UNIQUE (UserId, GoalDate)
);

CREATE INDEX IX_UserDailyGoals_User_Date ON UserDailyGoals(UserId, GoalDate);
```

---

## 📋 4. KẾ HOẠCH IMPLEMENTATION CHI TIẾT

### Sprint 1: "User Analytics & Streak" (1-2 tuần)

#### **Task 1.1: UserAnalytics Service**
```
Mục đích: Tổng hợp analytics cho user dashboard

Endpoints mới:
GET /api/me/analytics
→ Weekly stats, monthly stats, skill breakdown
→ Category performance, accuracy trends
→ Streak calendar (last 30 days)

Response:
{
  "weekly": {
    "minutesSpent": 120,
    "exercisesCompleted": 15,
    "pointsEarned": 450,
    "avgScore": 85.5
  },
  "monthly": {
    "minutesSpent": 480,
    "exercisesCompleted": 60,
    "pointsEarned": 1800,
    "avgScore": 82.3
  },
  "skills": {
    "dictation_accuracy": 85,
    "shadowing_accuracy": 78,
    "vocabulary_strength": 70,
    "listening_comprehension": 80
  },
  "categories": {
    "conversation": 85,
    "business": 72,
    "travel": 90
  },
  "accuracyTrend": [85, 82, 88, 90, 87, 92, 95],
  "streakCalendar": {
    "2026-03-01": true,
    "2026-03-02": true,
    "2026-03-03": false,
    ...
  }
}
```

**Implementation:**
1. Tạo `UserAnalytics` entity
2. Tạo `UserAnalyticsRepository`
3. Tạo `GetUserAnalyticsService`
4. Cập nhật `RecordActivityService` để update analytics
5. Tạo migration

#### **Task 1.2: Daily Goal Tracking**
```
Mục đích: Track daily goals và motivation

Endpoints mới:
GET /api/me/daily-goal
→ Hôm nay đã đạt goal chưa?

POST /api/me/daily-goal/check
→ Kiểm tra và cập nhật goal status

Response:
{
  "date": "2026-03-31",
  "targetMinutes": 30,
  "actualMinutes": 25,
  "targetExercises": 3,
  "actualExercises": 2,
  "goalAchieved": false,
  "progress": 83.3
}
```

**Implementation:**
1. Tạo `UserDailyGoal` entity
2. Cập nhật `RecordActivityService` để track daily goal
3. Tạo `GetDailyGoalService`

#### **Task 1.3: Enhanced Streak Visualization**
```
Mục đích: Hiển thị streak calendar đẹp cho FE

Endpoint mới:
GET /api/me/streak/calendar?days=30
→ Trả về streak data cho calendar view

Response:
{
  "currentStreak": 7,
  "longestStreak": 15,
  "totalActiveDays": 45,
  "freezesAvailable": 2,
  "calendar": [
    {"date": "2026-03-01", "active": true, "minutes": 35},
    {"date": "2026-03-02", "active": true, "minutes": 28},
    {"date": "2026-03-03", "active": false, "minutes": 0},
    ...
  ],
  "milestones": [
    {"days": 7, "achieved": true, "reward": "+1 Freeze"},
    {"days": 14, "achieved": false, "reward": "+2 Freeze"},
    {"days": 30, "achieved": false, "reward": "+3 Freeze"}
  ]
}
```

**Implementation:**
1. Tạo `GetStreakCalendarService`
2. Cập nhật `RecordActivityService` để award freeze theo milestone
3. Tạo streak calendar data từ UserDailyActivity

#### **Task 1.4: Per-Lesson Statistics**
```
Mục đích: User biết mình học bài này thế nào

Endpoint mới:
GET /api/lessons/{id}/my-stats

Response:
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

**Implementation:**
1. Tạo `GetLessonStatsService`
2. Query UserExercises theo (UserId, LessonId)
3. Tính toán statistics

---

### Sprint 2: "Session & Retention" (1-2 tuần)

#### **Task 2.1: Dictation Session/Resume**
```
Mục đích: User đóng app → vào lại → tiếp tục từ segment đang dở

Endpoints mới:
GET  /api/lessons/{id}/dictation/session
→ Lấy tiến độ dở (segment hiện tại, answers đã điền)

POST /api/lessons/{id}/dictation/session/save
→ Auto-save từng segment

POST /api/lessons/{id}/dictation/session/complete
→ Đánh dấu hoàn thành, xóa session

POST /api/lessons/{id}/dictation/session/abandon
→ Hủy session

Domain mới:
public class DictationSession : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public string Level { get; set; }
    public int CurrentSegmentIndex { get; set; }
    public string? SavedAnswers { get; set; } // JSON
    public DateTime StartedAt { get; set; }
    public DateTime LastSavedAt { get; set; }
    public bool IsCompleted { get; set; }
}
```

**Implementation:**
1. Tạo `DictationSession` entity
2. Tạo migration
3. Tạo `DictationSessionService`
4. Cập nhật `DictationController`

#### **Task 2.2: Streak Freeze Endpoint**
```
Mục đích: User dùng freeze để bảo vệ streak khi bận

Endpoint mới:
POST /api/me/streak/freeze

Logic:
- Điều kiện: FreezesAvailable > 0, chưa học hôm nay
- FreezeCount++, FreezesAvailable--
- LastActiveDate = hôm nay (để không break streak)

Award freeze:
- Mỗi 7 ngày streak liên tiếp → +1 FreezesAvailable (tối đa 3)
```

**Implementation:**
1. Tạo `FreezeStreakService`
2. Cập nhật `RecordActivityService` để award freeze
3. Tạo endpoint trong `MeController`

---

### Sprint 3: "Gamification & Social" (2-3 tuần)

#### **Task 3.1: Achievement System**
```
Mục đích: Tạo động lực với badges và thành tích

Achievements mẫu:
- "First Lesson" - Hoàn thành bài học đầu tiên
- "Perfect Score" - Đạt 100% trong 1 bài
- "Streak 7" - Học 7 ngày liên tiếp
- "Streak 30" - Học 30 ngày liên tiếp
- "Dictation Master" - Hoàn thành 50 bài dictation
- "Shadowing Pro" - Hoàn thành 50 bài shadowing
- "Speed Learner" - Hoàn thành 10 bài trong 1 ngày
- "Accuracy King" - Đạt avg score > 90%

Endpoints:
GET  /api/me/achievements
→ Danh sách achievements đã unlock

POST /api/me/achievements/check
→ Kiểm tra và unlock achievements mới

GET  /api/achievements
→ Danh sách tất cả achievements (cho FE hiển thị)
```

**Implementation:**
1. Tạo `Achievement` và `UserAchievement` entities
2. Tạo migration
3. Tạo `AchievementService` với check logic
4. Seed achievements vào DB
5. Cập nhật `RecordActivityService` để check achievements

#### **Task 3.2: Leaderboard**
```
Mục đích: Tạo cạnh tranh xã hội

Endpoints:
GET /api/leaderboard/weekly
→ Top 50 điểm tuần này (reset mỗi thứ 2)

GET /api/leaderboard/alltime
→ Top 100 mọi thời đại

GET /api/lessons/{id}/leaderboard
→ Top 10 accuracy của bài học cụ thể

Response:
{
  "myRank": 12,
  "myScore": 3240,
  "entries": [
    {"rank": 1, "username": "user1", "avatarUrl": "...", "score": 5200},
    ...
  ],
  "resetAt": "2026-03-09T00:00:00Z"
}
```

**Implementation:**
1. Tạo `LeaderboardService`
2. Cache leaderboard trong Redis (hoặc DB)
3. Cron job để tính toán leaderboard hàng tuần

#### **Task 3.3: Vocabulary Extraction**
```
Mục đích: Tự động extract từ vựng từ sai lầm

Logic:
- User sai từ trong dictation → extract từ đó
- Lưu vào UserVocabulary với context
- SRS (Spaced Repetition) để ôn tập

Endpoints:
GET  /api/me/vocabulary
→ Từ điển cá nhân (pagination)

POST /api/me/vocabulary/{id}/review
→ Báo cáo kết quả ôn (correct/wrong)

GET  /api/me/vocabulary/review
→ Lấy N từ cần ôn hôm nay (SRS)
```

**Implementation:**
1. Tạo `UserVocabulary` entity (đã có trong schema)
2. Tạo `VocabularyService`
3. Cập nhật `SubmitDictationService` để extract vocabulary
4. Implement SM-2 algorithm cho SRS

---

### Sprint 4: "Admin & Operations" (1-2 tuần)

#### **Task 4.1: Admin Dashboard Analytics**
```
Mục đích: Admin theo dõi hệ thống

Endpoints:
GET /api/admin/analytics
→ Tổng quan: users, revenue, engagement

Response:
{
  "users": {
    "total": 1250,
    "activeToday": 85,
    "activeThisWeek": 320,
    "newThisWeek": 45
  },
  "revenue": {
    "thisMonth": 15000000,
    "lastMonth": 12000000,
    "growth": 25.0
  },
  "engagement": {
    "avgSessionMinutes": 25,
    "avgExercisesPerUser": 8,
    "completionRate": 72.5
  },
  "topLessons": [
    {"id": "...", "title": "...", "completions": 450, "avgScore": 85.2},
    ...
  ],
  "conversionRate": {
    "freeToPremium": 8.5
  }
}
```

**Implementation:**
1. Tạo `AdminAnalyticsService`
2. Query tổng hợp từ các bảng
3. Cache results

#### **Task 4.2: Hint System Endpoint**
```
Mục đích: User có thể xin hint khi bí

Endpoint mới:
POST /api/lessons/{id}/segments/{i}/hint
Body: { "level": "Beginner", "hintLevel": 1 }

Logic hint 3 cấp:
Level 1: Hé lộ chữ cái đầu    → "H___ y__ are ___king"     (-5% score)
Level 2: Hé lộ 50% ký tự      → "He__o you are tal___g"    (-15% score)
Level 3: Reveal toàn bộ từ    → "Hello you are talking"    (-30% score)
```

**Implementation:**
1. Tạo `GetHintService`
2. Cập nhật `DictationTemplateGenerator` để include hint data
3. Tạo endpoint

---

## 🎯 5. MỤC TIÊU MVP — THU HÚT NGƯỜI DÙNG

### 5.1 Features cho MVP Launch

```
✅ Đã có:
├── Auth (Email + Google)
├── Dictation (4 levels, auto-template)
├── Shadowing (text-fallback)
├── Progress tracking
├── Streak system
├── Payment (SEPay)
└── Premium subscription

🆕 Cần thêm cho MVP:
├── User Analytics Dashboard (Sprint 1)
├── Daily Goal Tracking (Sprint 1)
├── Streak Calendar Visualization (Sprint 1)
├── Per-Lesson Statistics (Sprint 1)
├── Dictation Session/Resume (Sprint 2)
├── Streak Freeze (Sprint 2)
├── Achievement Badges (Sprint 3)
└── Leaderboard (Sprint 3)
```

### 5.2 User Journey — Thu Hút & Giữ Chân

```
Ngày 1: Đăng ký → Học bài đầu tiên → Nhận badge "First Lesson"
Ngày 2: Học tiếp → Thấy streak "2 days" → Động lực tiếp tục
Ngày 3: Học → Đạt daily goal → Nhận bonus points
Ngày 7: Streak 7 ngày → Nhận +1 Freeze → Badge "Week Warrior"
Ngày 14: Thấy leaderboard → Muốn leo rank → Học nhiều hơn
Ngày 30: Streak 30 ngày → Badge "Month Master" → Premium upgrade
```

### 5.3 Metrics để đo lường thành công

```
Retention:
- D1 Retention: > 40%
- D7 Retention: > 20%
- D30 Retention: > 10%

Engagement:
- Avg session time: > 15 phút
- Exercises per week: > 10
- Streak length: > 7 ngày

Conversion:
- Free → Premium: > 5%
- Premium retention: > 80%
```

---

## 🔧 6. TECHNICAL DEBT & REFACTORING

### 6.1 Code Duplication — Cần Fix

```
Vấn đề: CheckSegmentService và CheckShadowingService có ~80% code giống nhau

Giải pháp: Tạo BaseCheckService abstract class
```

```csharp
// Abstract base cho cả Dictation và Shadowing
public abstract class BaseCheckService
{
    // Shared: LCS algorithm, word splitting, normalization
    protected static List<WordCheckResult> CompareWords(string target, string user);
    protected static string[] SplitWords(string text);
    protected static string Normalize(string word);
    
    // Abstract: mỗi subclass implement riêng
    protected abstract Task SaveExerciseAsync(...);
    protected abstract ExerciseType GetExerciseType();
}
```

### 6.2 Missing Error Handling

```
Vấn đề: Nhiều service không có proper error handling

Giải pháp: 
- Thêm try-catch cho tất cả DB operations
- Log errors với structured logging
- Return meaningful error messages
```

### 6.3 Performance Issues

```
Vấn đề: 
- GetUserAnalytics query có thể chậm với nhiều data
- Leaderboard calculation không có cache

Giải pháp:
- Thêm indexes cho UserExercises (UserId, LessonId, CompletedAt)
- Cache leaderboard trong Redis
- Background job để tính toán analytics
```

---

## 📊 7. SUMMARY — ƯU TIÊN & TIMELINE

### Sprint 1 (1-2 tuần): "User Analytics & Streak"
- [ ] UserAnalytics entity + service
- [ ] Daily Goal tracking
- [ ] Streak Calendar visualization
- [ ] Per-Lesson statistics
- **Impact:** 🔴 Critical cho retention

### Sprint 2 (1-2 tuần): "Session & Retention"
- [ ] Dictation Session/Resume
- [ ] Streak Freeze endpoint
- [ ] Fix code duplication (BaseCheckService)
- **Impact:** 🟠 High cho UX

### Sprint 3 (2-3 tuần): "Gamification & Social"
- [ ] Achievement system
- [ ] Leaderboard
- [ ] Vocabulary extraction
- **Impact:** 🟡 Medium cho engagement

### Sprint 4 (1-2 tuần): "Admin & Operations"
- [ ] Admin Dashboard Analytics
- [ ] Hint System endpoint
- [ ] Performance optimization
- **Impact:** 🟢 Nice-to-have

---

## 🚀 8. HÀNH ĐỘNG NGAY HÔM NAY

### Bước 1: Tạo UserAnalytics Entity
```bash
# Tạo entity file
touch src/Demif.Domain/Entities/UserAnalytics.cs

# Tạo repository
touch src/Demif.Application/Abstractions/Repositories/IUserAnalyticsRepository.cs

# Tạo service
touch src/Demif.Application/Features/Me/GetUserAnalytics/GetUserAnalyticsService.cs
```

### Bước 2: Tạo Migration
```bash
dotnet ef migrations add AddUserAnalytics -p src/Demif.Infrastructure -s src/Demif.Api
dotnet ef database update -p src/Demif.Infrastructure -s src/Demif.Api
```

### Bước 3: Implement GetUserAnalytics Endpoint
```csharp
// MeController.cs
[HttpGet("analytics")]
public async Task<IActionResult> GetUserAnalytics(CancellationToken ct)
{
    // Implementation
}
```

### Bước 4: Test với Postman
```bash
# Import collection
# Test GET /api/me/analytics
# Verify response format
```

---

## 📝 9. KẾT LUẬN

### Đánh giá tổng thể:

| Aspect | Score | Notes |
|--------|-------|-------|
| Architecture | 9/10 | Clean Architecture chuẩn |
| Code Quality | 8/10 | Tốt, cần refactor duplication |
| Feature Completeness | 6/10 | Core đã có, thiếu analytics |
| User Experience | 5/10 | Thiếu visualization, session |
| Scalability | 8/10 | PostgreSQL + Redis ready |
| **Overall** | **7.2/10** | **Cần Sprint 1-2 để đạt MVP** |

### Khuyến nghị:

1. **Focus Sprint 1** vào User Analytics & Streak Visualization
2. **Sprint 2** làm Dictation Session để giảm churn
3. **Sprint 3** thêm Gamification để tăng engagement
4. **Sprint 4** làm Admin Dashboard cho operations

### Expected Impact sau 4 Sprints:

```
D1 Retention: 30% → 45% (+15%)
D7 Retention: 15% → 25% (+10%)
D30 Retention: 5% → 12% (+7%)
Avg Session Time: 10 phút → 20 phút (+100%)
Free → Premium Conversion: 3% → 8% (+5%)
```

---

> **Tài liệu này được tạo bởi Senior System Architect**  
> **Mục tiêu:** Hướng dẫn implementation chi tiết cho team  
> **Next step:** Review và approve plan, sau đó bắt đầu Sprint 1
