-- =====================================================
-- DEMIF - OPTIMIZED DATABASE SCHEMA FOR SQL SERVER
-- Version: 1.0
-- Minimal FK, Maximum Flexibility, Easy Debug
-- =====================================================

-- 1. USERS (Authentication + Profile - gộp chung)
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(255) NOT NULL,
    PasswordHash NVARCHAR(255),
    Username NVARCHAR(50) NOT NULL,
    AvatarUrl NVARCHAR(500),
    Status NVARCHAR(20) DEFAULT 'active', -- active, suspended, banned
    
    -- Profile info (embedded để giảm joins)
    Country NVARCHAR(100),
    NativeLanguage NVARCHAR(50) DEFAULT N'Vietnamese',
    TargetLanguage NVARCHAR(50) DEFAULT N'English',
    CurrentLevel NVARCHAR(20) DEFAULT 'beginner', -- beginner, intermediate, advanced
    DailyGoalMinutes INT DEFAULT 30,
    
    -- Firebase Auth Integration
    FirebaseUid NVARCHAR(128),
    AuthProvider NVARCHAR(30) DEFAULT 'email', -- email, google, facebook
    
    -- Settings (JSON để linh hoạt)
    Settings NVARCHAR(MAX), -- JSON: {notifications: {}, privacy: {}}
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    LastLoginAt DATETIME2,
    
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT UQ_Users_FirebaseUid UNIQUE (FirebaseUid)
);

CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_FirebaseUid ON Users(FirebaseUid);
CREATE INDEX IX_Users_Status ON Users(Status);

-- 1.5. ROLES (Quản lý vai trò người dùng)
CREATE TABLE Roles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500),
    IsDefault BIT DEFAULT 0, -- Role mặc định cho user mới
    IsActive BIT DEFAULT 1,
    
    -- Permissions (JSON để linh hoạt)
    -- VD: {"canManageUsers": true, "canViewReports": false}
    Permissions NVARCHAR(MAX),
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);

CREATE INDEX IX_Roles_Name ON Roles(Name);
CREATE INDEX IX_Roles_IsDefault ON Roles(IsDefault);

-- Seed default roles
INSERT INTO Roles (Id, Name, Description, IsDefault, IsActive, Permissions)
VALUES 
(NEWID(), N'Admin', N'Quản trị viên hệ thống - full quyền', 0, 1, 
 '{"canManageUsers": true, "canManageContent": true, "canViewReports": true, "canManagePayments": true}'),
(NEWID(), N'User', N'Người dùng thông thường', 1, 1,
 '{"canAccessLessons": true, "canSubmitExercises": true}'),
(NEWID(), N'Premium', N'Người dùng Premium - không giới hạn bài học', 0, 1,
 '{"canAccessLessons": true, "canSubmitExercises": true, "canAccessPremiumContent": true, "unlimitedLessons": true, "aiFeatures": true}'),
(NEWID(), N'Moderator', N'Điều phối viên - quản lý nội dung', 0, 1,
 '{"canManageContent": true, "canViewReports": true}');

-- 1.6. USER_ROLES (Quan hệ nhiều-nhiều User-Role)
CREATE TABLE UserRoles (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL, -- Ref to Users.Id
    RoleId UNIQUEIDENTIFIER NOT NULL, -- Ref to Roles.Id
    
    AssignedAt DATETIME2 DEFAULT GETUTCDATE(),
    AssignedBy UNIQUEIDENTIFIER, -- Người gán role (null = hệ thống)
    ExpiresAt DATETIME2, -- Ngày hết hạn role (null = vĩnh viễn)
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    
    CONSTRAINT UQ_UserRoles_User_Role UNIQUE (UserId, RoleId)
);

CREATE INDEX IX_UserRoles_UserId ON UserRoles(UserId);
CREATE INDEX IX_UserRoles_RoleId ON UserRoles(RoleId);
CREATE INDEX IX_UserRoles_ExpiresAt ON UserRoles(ExpiresAt);


-- 2. LESSONS (Bài học - Dictation & Shadowing)
CREATE TABLE Lessons (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),
    
    -- Phân loại
    LessonType NVARCHAR(20) NOT NULL, -- dictation, shadowing
    Level NVARCHAR(20) NOT NULL, -- beginner, intermediate, advanced
    Category NVARCHAR(50), -- conversation, business, travel, academic
    
    -- Media (stored in Cloudflare R2 / any cloud storage)
    AudioUrl NVARCHAR(500) NOT NULL,
    DurationSeconds INT NOT NULL,
    ThumbnailUrl NVARCHAR(500),
    MediaUrl NVARCHAR(500), -- URL video/audio linh hoạt (thay thế dần AudioUrl)
    MediaType NVARCHAR(20), -- audio, video
    
    -- Content 
    FullTranscript NVARCHAR(MAX) NOT NULL,
    
    -- Cho DICTATION: Template với chỗ trống
    -- JSON: {"segments": [{"text": "I", "isBlank": false}, {"text": "___", "isBlank": true, "answer": "went", "hint": "w___"}]}
    DictationTemplate NVARCHAR(MAX),
    
    -- Premium & Ordering
    IsPremiumOnly BIT DEFAULT 0, -- Chỉ user Premium mới xem được
    DisplayOrder INT DEFAULT 0, -- Thứ tự hiển thị
    Tags NVARCHAR(MAX), -- JSON array: ["business", "conversation"]
    
    -- Stats (denormalized for performance)
    CompletionsCount INT DEFAULT 0,
    AvgScore DECIMAL(5,2) DEFAULT 0,
    
    Status NVARCHAR(20) DEFAULT 'published', -- draft, published, archived
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2
);

CREATE INDEX IX_Lessons_Type_Level ON Lessons(LessonType, Level);
CREATE INDEX IX_Lessons_Status ON Lessons(Status);
CREATE INDEX IX_Lessons_Category ON Lessons(Category);
CREATE INDEX IX_Lessons_IsPremiumOnly ON Lessons(IsPremiumOnly);
CREATE INDEX IX_Lessons_DisplayOrder ON Lessons(DisplayOrder);

-- 3. USER_EXERCISES (Kết quả làm bài - KHÔNG CÓ FK)
CREATE TABLE UserExercises (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL, -- Soft ref to Users.Id
    LessonId UNIQUEIDENTIFIER NOT NULL, -- Soft ref to Lessons.Id
    
    ExerciseType NVARCHAR(20) NOT NULL, -- dictation, shadowing
    
    -- User's submission
    UserInput NVARCHAR(MAX), -- Dictation: câu trả lời | Shadowing: transcript
    RecordingUrl NVARCHAR(500), -- URL file ghi âm (Cloudflare R2)
    
    -- Results (JSON để linh hoạt cho cả 2 loại)
    -- Dictation: {"totalBlanks": 5, "correctBlanks": 4, "answers": [{...}]}
    -- Shadowing: {"wordAccuracy": 85, "pronunciation": 78, "fluency": 80, "feedback": [...]}
    ResultDetails NVARCHAR(MAX),
    
    Score INT NOT NULL, -- 0-100
    TimeSpentSeconds INT,
    Attempts INT DEFAULT 1,
    PlaysUsed INT DEFAULT 1, -- Số lần nghe đã dùng
    
    CompletedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_UserExercises_UserId ON UserExercises(UserId);
CREATE INDEX IX_UserExercises_LessonId ON UserExercises(LessonId);
CREATE INDEX IX_UserExercises_CompletedAt ON UserExercises(CompletedAt);
CREATE INDEX IX_UserExercises_Type ON UserExercises(ExerciseType);

-- 4. USER_PROGRESS (Tiến độ học tập - 1 row per user)
CREATE TABLE UserProgress (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    
    -- Tổng quan
    TotalPoints INT DEFAULT 0,
    TotalMinutes INT DEFAULT 0,
    LessonsCompleted INT DEFAULT 0,
    DictationCompleted INT DEFAULT 0,
    ShadowingCompleted INT DEFAULT 0,
    
    -- Accuracy
    AvgDictationScore DECIMAL(5,2) DEFAULT 0,
    AvgShadowingScore DECIMAL(5,2) DEFAULT 0,
    
    -- Skills (JSON để linh hoạt thêm skill mới)
    -- {"listening": 75, "speaking": 60, "vocabulary": 80, "grammar": 70}
    Skills NVARCHAR(MAX),
    
    -- Level progression
    CurrentLevel NVARCHAR(20) DEFAULT 'beginner',
    LevelProgress INT DEFAULT 0, -- % to next level (0-100)
    
    UpdatedAt DATETIME2,
    
    CONSTRAINT UQ_UserProgress_UserId UNIQUE (UserId)
);

CREATE INDEX IX_UserProgress_UserId ON UserProgress(UserId);

-- 5. USER_STREAKS (Chuỗi ngày học)
CREATE TABLE UserStreaks (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    
    CurrentStreak INT DEFAULT 0,
    LongestStreak INT DEFAULT 0,
    LastActiveDate DATE,
    TotalActiveDays INT DEFAULT 0,
    
    -- Streak freeze (giữ streak khi bận)
    FreezeCount INT DEFAULT 0,
    FreezesAvailable INT DEFAULT 1,
    
    UpdatedAt DATETIME2,
    
    CONSTRAINT UQ_UserStreaks_UserId UNIQUE (UserId)
);

CREATE INDEX IX_UserStreaks_UserId ON UserStreaks(UserId);

-- 6. VOCABULARY (Từ vựng master)
CREATE TABLE Vocabulary (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Word NVARCHAR(100) NOT NULL,
    Phonetic NVARCHAR(100), -- IPA: /həˈloʊ/
    AudioUrl NVARCHAR(500),
    Translation NVARCHAR(255),
    Definition NVARCHAR(MAX),
    ExampleSentence NVARCHAR(MAX),
    PartOfSpeech NVARCHAR(30), -- noun, verb, adj, adv...
    Difficulty NVARCHAR(20) DEFAULT 'medium', -- easy, medium, hard
    Tags NVARCHAR(MAX), -- JSON: ["business", "daily", "travel"]
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Vocabulary_Word ON Vocabulary(Word);
CREATE INDEX IX_Vocabulary_Difficulty ON Vocabulary(Difficulty);

-- 7. USER_VOCABULARY (Từ vựng của user - Spaced Repetition SM-2)
CREATE TABLE UserVocabulary (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    VocabularyId UNIQUEIDENTIFIER NOT NULL,
    
    -- Spaced Repetition (SM-2 algorithm)
    MasteryLevel INT DEFAULT 0, -- 0-100
    ReviewCount INT DEFAULT 0,
    CorrectCount INT DEFAULT 0,
    NextReviewAt DATETIME2,
    EaseFactor DECIMAL(3,2) DEFAULT 2.5, -- SM-2 ease factor
    IntervalDays INT DEFAULT 1, -- Days until next review
    
    Status NVARCHAR(20) DEFAULT 'learning', -- new, learning, mastered
    AddedAt DATETIME2 DEFAULT GETUTCDATE(),
    LastReviewedAt DATETIME2
);

CREATE INDEX IX_UserVocabulary_UserId ON UserVocabulary(UserId);
CREATE INDEX IX_UserVocabulary_NextReview ON UserVocabulary(NextReviewAt);
CREATE INDEX IX_UserVocabulary_Status ON UserVocabulary(Status);

-- 8. SUBSCRIPTION_PLANS (Gói đăng ký)
CREATE TABLE SubscriptionPlans (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL, -- Premium Tháng, Premium Năm, Premium Vĩnh viễn
    Tier NVARCHAR(20) NOT NULL, -- free, basic, premium
    
    Price DECIMAL(18,2) NOT NULL,
    Currency NVARCHAR(10) DEFAULT 'VND',
    BillingCycle NVARCHAR(20), -- monthly, yearly, lifetime
    DurationDays INT, -- null = lifetime (vĩnh viễn)
    
    -- Features (JSON linh hoạt)
    Features NVARCHAR(MAX), -- JSON: ["Unlimited lessons", "AI feedback", "No ads"]
    Limits NVARCHAR(MAX), -- JSON: {"lessonsPerDay": 5, "aiRequests": 10}
    
    BadgeText NVARCHAR(50),
    BadgeColor NVARCHAR(20),
    IsActive BIT DEFAULT 1,
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    
    CONSTRAINT UQ_SubscriptionPlans_Tier_Cycle UNIQUE (Tier, BillingCycle)
);

CREATE INDEX IX_SubscriptionPlans_IsActive ON SubscriptionPlans(IsActive);

-- 9. USER_SUBSCRIPTIONS (Đăng ký của user)
CREATE TABLE UserSubscriptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PlanId UNIQUEIDENTIFIER NOT NULL,
    
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2,
    Status NVARCHAR(20) DEFAULT 'active', -- active, expired, cancelled
    AutoRenew BIT DEFAULT 0,
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_UserSubscriptions_UserId ON UserSubscriptions(UserId);
CREATE INDEX IX_UserSubscriptions_Status ON UserSubscriptions(Status);

-- 10. PAYMENTS (Thanh toán - SEPay Bank Transfer)
CREATE TABLE Payments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PlanId UNIQUEIDENTIFIER NOT NULL,
    SubscriptionId UNIQUEIDENTIFIER, -- Ref to UserSubscriptions created after success
    
    -- Amount
    Amount DECIMAL(12,2) NOT NULL,
    Currency NVARCHAR(3) DEFAULT 'VND',
    
    -- SEPay Integration
    PaymentMethod NVARCHAR(30) DEFAULT 'sepay_bank', -- sepay_bank, momo, zalopay
    TransactionId NVARCHAR(255), -- SEPay transaction ID
    BankCode NVARCHAR(20), -- VCB, TCB, MB, ACB...
    BankTransactionNo NVARCHAR(100),
    
    -- Unique payment reference for SEPay webhook matching
    PaymentReference NVARCHAR(50) NOT NULL,
    
    -- Status
    Status NVARCHAR(20) DEFAULT 'pending', -- pending, completed, failed, refunded
    
    -- SEPay webhook response
    GatewayResponse NVARCHAR(MAX), -- JSON response from SEPay
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2,
    RefundedAt DATETIME2,
    RefundReason NVARCHAR(MAX)
);

CREATE INDEX IX_Payments_UserId ON Payments(UserId);
CREATE INDEX IX_Payments_Status ON Payments(Status);
CREATE INDEX IX_Payments_Reference ON Payments(PaymentReference);

-- 11. LEADERBOARD (Bảng xếp hạng - Cache/Snapshot)
CREATE TABLE Leaderboard (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    
    -- Denormalized for fast query
    Username NVARCHAR(50),
    AvatarUrl NVARCHAR(500),
    Country NVARCHAR(100),
    
    TotalScore INT NOT NULL,
    LessonsCompleted INT,
    CurrentStreak INT,
    Rank INT,
    
    Period NVARCHAR(20) NOT NULL, -- daily, weekly, monthly, alltime
    CalculatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Leaderboard_Period_Rank ON Leaderboard(Period, Rank);
CREATE INDEX IX_Leaderboard_Period_Score ON Leaderboard(Period, TotalScore DESC);

-- 12. USER_DAILY_ACTIVITY (Hoạt động hàng ngày)
CREATE TABLE UserDailyActivity (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    ActivityDate DATE NOT NULL,
    
    MinutesSpent INT DEFAULT 0,
    ExercisesCompleted INT DEFAULT 0,
    PointsEarned INT DEFAULT 0,
    GoalAchieved BIT DEFAULT 0,
    
    -- Chi tiết (JSON)
    Details NVARCHAR(MAX), -- {"dictation": 3, "shadowing": 2, "vocab": 10}
    
    CONSTRAINT UQ_UserDailyActivity UNIQUE (UserId, ActivityDate)
);

CREATE INDEX IX_UserDailyActivity_User_Date ON UserDailyActivity(UserId, ActivityDate);

-- =====================================================
-- PHASE 2: FUTURE TABLES (Add when needed)
-- =====================================================

-- 13. PLACEMENT_TESTS
CREATE TABLE PlacementTests (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    
    TotalScore INT,
    MaxScore INT,
    AssignedLevel NVARCHAR(20),
    
    Answers NVARCHAR(MAX), -- JSON
    SkillBreakdown NVARCHAR(MAX), -- JSON
    
    CompletedAt DATETIME2
);

-- 14. ACHIEVEMENTS
CREATE TABLE Achievements (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(50) NOT NULL,
    Title NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    IconUrl NVARCHAR(500),
    Points INT DEFAULT 0,
    Category NVARCHAR(30), -- streak, lessons, accuracy
    Requirement NVARCHAR(MAX), -- JSON condition
    Rarity NVARCHAR(20), -- common, rare, epic, legendary
    
    CONSTRAINT UQ_Achievements_Code UNIQUE (Code)
);

-- 15. USER_ACHIEVEMENTS
CREATE TABLE UserAchievements (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    AchievementId UNIQUEIDENTIFIER NOT NULL,
    UnlockedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_UserAchievements_UserId ON UserAchievements(UserId);

-- 16. NOTIFICATIONS
CREATE TABLE Notifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    
    Type NVARCHAR(50) NOT NULL, -- daily_reminder, streak_warning, achievement
    Title NVARCHAR(255) NOT NULL,
    Body NVARCHAR(MAX) NOT NULL,
    
    TargetType NVARCHAR(30) DEFAULT 'all', -- all, segment, individual
    TargetSegment NVARCHAR(MAX), -- JSON
    
    ScheduledAt DATETIME2,
    IsRecurring BIT DEFAULT 0,
    RecurringPattern NVARCHAR(50),
    
    ActionUrl NVARCHAR(500),
    ImageUrl NVARCHAR(500),
    Data NVARCHAR(MAX),
    
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 17. USER_NOTIFICATIONS
CREATE TABLE UserNotifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    NotificationId UNIQUEIDENTIFIER,
    
    Type NVARCHAR(50) NOT NULL,
    Title NVARCHAR(255) NOT NULL,
    Body NVARCHAR(MAX) NOT NULL,
    ActionUrl NVARCHAR(500),
    Data NVARCHAR(MAX),
    
    Channel NVARCHAR(20) DEFAULT 'push',
    Status NVARCHAR(20) DEFAULT 'pending',
    
    ScheduledAt DATETIME2,
    SentAt DATETIME2,
    DeliveredAt DATETIME2,
    ReadAt DATETIME2,
    
    ErrorMessage NVARCHAR(MAX),
    RetryCount INT DEFAULT 0,
    
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE INDEX IX_UserNotifications_UserId ON UserNotifications(UserId);
CREATE INDEX IX_UserNotifications_Status ON UserNotifications(Status);

-- =====================================================
-- SEED DATA
-- =====================================================

-- Insert default subscription plans
INSERT INTO SubscriptionPlans (Id, Name, Tier, Price, Currency, BillingCycle, Features, Limits, IsActive)
VALUES 
(NEWID(), N'Miễn phí', 'free', 0, 'VND', NULL, 
 '["5 bài học/ngày", "Dictation cơ bản"]', 
 '{"lessonsPerDay": 5, "aiRequests": 0}', 1),
(NEWID(), N'Cơ bản', 'basic', 99000, 'VND', 'monthly', 
 '["20 bài học/ngày", "AI feedback cơ bản", "Streak freeze x2"]', 
 '{"lessonsPerDay": 20, "aiRequests": 50}', 1),
(NEWID(), N'Premium', 'premium', 199000, 'VND', 'monthly', 
 '["Không giới hạn bài học", "AI feedback nâng cao", "Streak freeze x5", "Không quảng cáo"]', 
 '{"lessonsPerDay": -1, "aiRequests": -1}', 1);

-- =====================================================
-- END OF SCHEMA
-- =====================================================
