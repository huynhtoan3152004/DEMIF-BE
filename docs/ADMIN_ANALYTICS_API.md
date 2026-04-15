# Admin Analytics API

Base URL: `/api/admin/analytics`

Authentication: `Bearer JWT` with `Admin` policy.

## Endpoints

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/admin/analytics` | Full dashboard payload |
| GET | `/api/admin/analytics/overview` | Summary cards, alerts, top users, popular/difficult lessons |
| GET | `/api/admin/analytics/users` | User status and engagement breakdown |
| GET | `/api/admin/analytics/lessons` | Lesson/exercise participation and performance |
| GET | `/api/admin/analytics/lessons/access` | Lesson access / open tracking analytics |
| GET | `/api/admin/analytics/vocabulary` | Vocabulary review analytics |
| GET | `/api/admin/analytics/payments` | Subscription and payment analytics |
| GET | `/api/admin/analytics/content` | Blog, notification, and engagement analytics |

---

## 1. Full Dashboard

### Request

```http
GET /api/admin/analytics
Authorization: Bearer <token>
```

No request body.

### Response

```json
{
  "generatedAt": "2026-04-16T10:00:00Z",
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
    "totalUsers": 1500,
    "activeUsers": 930,
    "pendingUsers": 48,
    "inactiveUsers": 22,
    "suspendedUsers": 7,
    "bannedUsers": 3,
    "verifiedUsers": 1375,
    "newUsersToday": 24,
    "newUsersThisMonth": 310,
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
    ],
    "byCountry": [
      { "key": "Vietnam", "count": 1190 },
      { "key": "United States", "count": 180 }
    ]
  },
  "lessons": {
    "totalLessons": 180,
    "publishedLessons": 160,
    "draftLessons": 12,
    "archivedLessons": 8,
    "dictationLessons": 94,
    "shadowingLessons": 86,
    "premiumLessons": 35,
    "audioLessons": 128,
    "youtubeLessons": 52,
    "totalCompletions": 50200,
    "averageScore": 74.3,
    "byStatus": [
      { "key": "published", "count": 160 },
      { "key": "draft", "count": 12 }
    ],
    "byType": [
      { "key": "Dictation", "count": 94 },
      { "key": "Shadowing", "count": 86 }
    ],
    "byLevel": [
      { "key": "Beginner", "count": 80 },
      { "key": "Intermediate", "count": 60 },
      { "key": "Advanced", "count": 30 },
      { "key": "Expert", "count": 10 }
    ],
    "byCategory": [
      { "key": "travel", "count": 50 },
      { "key": "business", "count": 45 }
    ],
    "byMediaType": [
      { "key": "audio", "count": 128 },
      { "key": "youtube", "count": 52 }
    ],
    "popularLessons": [
      {
        "lessonId": "b1b222aa-1111-2222-3333-444444444444",
        "title": "Daily Greeting",
        "status": "published",
        "lessonType": "Dictation",
        "level": "Beginner",
        "category": "daily",
        "avgScore": 8,
        "completionsCount": 350,
        "createdAt": "2026-04-01T00:00:00Z",
        "authorId": "00000000-0000-0000-0000-000000000000"
      }
    ],
    "difficultLessons": [
      {
        "lessonId": "b1b85f64-aaaa-bbbb-cccc-111111111111",
        "title": "Business Negotiation 01",
        "status": "published",
        "lessonType": "Shadowing",
        "level": "Advanced",
        "category": "business",
        "avgScore": 2.5,
        "completionsCount": 50,
        "createdAt": "2026-04-01T00:00:00Z",
        "authorId": "00000000-0000-0000-0000-000000000000"
      }
    ],
    "recentLessons": [
      {
        "lessonId": "11111111-2222-3333-4444-555555555555",
        "title": "New Lesson",
        "status": "draft",
        "lessonType": "Dictation",
        "level": "Beginner",
        "category": "travel",
        "avgScore": 0,
        "completionsCount": 0,
        "createdAt": "2026-04-16T00:00:00Z",
        "authorId": "00000000-0000-0000-0000-000000000000"
      }
    ],
    "accessStats": {
      "generatedAt": "2026-04-16T10:00:00Z",
      "totalAccessEvents": 3,
      "totalTrackedLessons": 2,
      "totalTrackedUsers": 3,
      "completedTrackers": 1,
      "inProgressTrackers": 1,
      "startedTrackers": 1,
      "topAccessedLessons": [
        {
          "lessonId": "b1b222aa-1111-2222-3333-444444444444",
          "title": "Daily Greeting",
          "lessonType": "Dictation",
          "level": "Beginner",
          "category": "daily",
          "accessCount": 2,
          "uniqueUsers": 2,
          "completedCount": 1,
          "inProgressCount": 1,
          "startedCount": 0,
          "completionRate": 50,
          "firstAccessedAt": "2026-04-10T00:00:00Z",
          "lastAccessedAt": "2026-04-16T09:00:00Z",
          "createdAt": "2026-04-01T00:00:00Z"
        }
      ],
      "recentAccessedLessons": [],
      "byStatus": [
        { "key": "Completed", "count": 1 },
        { "key": "InProgress", "count": 1 },
        { "key": "Started", "count": 1 }
      ]
    }
  },
  "exercises": {
    "totalExercises": 12040,
    "dictationExercises": 8400,
    "shadowingExercises": 3640,
    "averageScore": 76.2,
    "highestScore": 100,
    "perfectScores": 120,
    "averageTimeSpentSeconds": 142.4,
    "exercisesToday": 320,
    "exercisesThisMonth": 8400,
    "byType": [
      { "key": "Dictation", "count": 8400 },
      { "key": "Shadowing", "count": 3640 }
    ]
  },
  "vocabulary": {
    "totalVocabulary": 8600,
    "dueVocabulary": 420,
    "overdueVocabulary": 112,
    "newVocabulary": 1300,
    "masteredVocabulary": 4300,
    "learningVocabulary": 3000,
    "recentVocabulary": 420,
    "vocabularyLessons": 180,
    "vocabularyTopics": 24,
    "topTopics": [
      { "key": "travel", "count": 2400 },
      { "key": "business", "count": 1900 }
    ],
    "topLessons": [
      {
        "lessonId": "b1b222aa-1111-2222-3333-444444444444",
        "title": "Daily Greeting",
        "status": "",
        "lessonType": "",
        "level": "",
        "category": null,
        "avgScore": 0,
        "completionsCount": 80,
        "createdAt": "2026-04-01T00:00:00Z",
        "authorId": null
      }
    ],
    "recentItems": [
      {
        "vocabularyId": "22222222-3333-4444-5555-666666666666",
        "lessonId": "b1b222aa-1111-2222-3333-444444444444",
        "lessonTitle": "Daily Greeting",
        "topic": "travel",
        "word": "airport",
        "reviewStatus": "due",
        "nextReviewAt": "2026-04-16T08:00:00Z",
        "createdAt": "2026-04-10T00:00:00Z"
      }
    ]
  },
  "subscriptions": {
    "totalPlans": 4,
    "activePlans": 4,
    "freePlans": 1,
    "basicPlans": 1,
    "premiumPlans": 2,
    "lifetimePlans": 1,
    "totalSubscriptions": 300,
    "activeSubscriptions": 280,
    "pendingSubscriptions": 6,
    "expiredSubscriptions": 10,
    "cancelledSubscriptions": 4,
    "autoRenewSubscriptions": 170,
    "expiringSoonSubscriptions": 18,
    "byStatus": [
      { "key": "Active", "count": 280 },
      { "key": "PendingPayment", "count": 6 }
    ],
    "byTier": [
      { "key": "Premium", "count": 220 },
      { "key": "Basic", "count": 80 }
    ],
    "byBillingCycle": [
      { "key": "Monthly", "count": 240 },
      { "key": "Lifetime", "count": 60 }
    ]
  },
  "payments": {
    "totalPayments": 320,
    "completedPayments": 300,
    "pendingPayments": 6,
    "failedPayments": 8,
    "refundedPayments": 6,
    "totalRevenue": 20000000,
    "todayRevenue": 1200000,
    "monthlyRevenue": 8000000,
    "averagePaymentAmount": 66666.7,
    "paymentsToday": 12,
    "paymentsThisMonth": 120,
    "byStatus": [
      { "key": "Completed", "count": 300 },
      { "key": "Pending", "count": 6 }
    ],
    "byMethod": [
      { "key": "sepay_bank", "count": 180 },
      { "key": "momo", "count": 80 }
    ],
    "revenueByTier": [
      { "key": "Premium", "amount": 15000000 },
      { "key": "Basic", "amount": 5000000 }
    ],
    "revenueByBillingCycle": [
      { "key": "Monthly", "amount": 18000000 },
      { "key": "Lifetime", "amount": 2000000 }
    ],
    "recentPayments": [
      {
        "paymentId": "33333333-4444-5555-6666-777777777777",
        "amount": 200000,
        "currency": "VND",
        "status": "Completed",
        "paymentMethod": "sepay_bank",
        "planName": "Premium Monthly",
        "planTier": "Premium",
        "createdAt": "2026-04-16T10:00:00Z",
        "completedAt": "2026-04-16T10:01:00Z"
      }
    ]
  },
  "blogs": {
    "totalBlogs": 42,
    "publishedBlogs": 38,
    "draftBlogs": 3,
    "archivedBlogs": 1,
    "totalViews": 120000,
    "averageViews": 2857.1,
    "byStatus": [
      { "key": "published", "count": 38 },
      { "key": "draft", "count": 3 }
    ],
    "popularBlogs": [
      {
        "blogId": "44444444-5555-6666-7777-888888888888",
        "title": "Tips for Daily Listening",
        "status": "published",
        "viewCount": 15000,
        "createdAt": "2026-04-01T00:00:00Z",
        "authorId": "00000000-0000-0000-0000-000000000000"
      }
    ],
    "recentBlogs": [
      {
        "blogId": "55555555-6666-7777-8888-999999999999",
        "title": "New Study Guide",
        "status": "draft",
        "viewCount": 0,
        "createdAt": "2026-04-16T00:00:00Z",
        "authorId": "00000000-0000-0000-0000-000000000000"
      }
    ]
  },
  "notifications": {
    "totalNotifications": 5000,
    "unreadNotifications": 1400,
    "readNotifications": 3600,
    "notificationsToday": 120,
    "notificationsThisMonth": 2400,
    "byType": [
      { "key": "system_announcement", "count": 1200 },
      { "key": "reminder", "count": 800 }
    ],
    "byChannel": [
      { "key": "email", "count": 3000 },
      { "key": "push", "count": 1800 }
    ]
  },
  "engagement": {
    "usersWithProgress": 1300,
    "usersWithStreak": 1200,
    "usersWithAnalytics": 1100,
    "averagePoints": 640.4,
    "averageMinutes": 210.7,
    "averageLessonsCompleted": 8.4,
    "averageDictationScore": 80.1,
    "averageShadowingScore": 72.8,
    "averageCurrentStreak": 6.3,
    "longestStreak": 52,
    "topUsers": [
      {
        "userId": "66666666-7777-8888-9999-aaaaaaaaaaaa",
        "username": "learner.one",
        "email": "learner1@example.com",
        "currentLevel": "Intermediate",
        "totalPoints": 800,
        "totalMinutes": 120,
        "engagementScore": 95,
        "currentStreak": 7,
        "longestStreak": 12,
        "lessonsCompleted": 10,
        "lastLoginAt": "2026-04-16T10:00:00Z"
      }
    ]
  },
  "alerts": [
    {
      "code": "draft_lessons",
      "title": "Bài học nháp cần duyệt",
      "message": "Các bài học draft chưa được xuất bản.",
      "count": 12,
      "severity": "warning"
    }
  ],
  "topUsers": [],
  "popularLessons": [],
  "difficultLessons": [],
  "recentLessons": [],
  "recentPayments": []
}
```

### FE notes

- `summary` is for KPI cards.
- `users.byStatus`, `users.byAuthProvider`, `users.byLevel`, `users.byCountry` are good chart datasets.
- `lessons.byStatus`, `lessons.byType`, `lessons.byLevel`, `lessons.byCategory`, `lessons.byMediaType` are chart datasets.
- `payments.byStatus`, `payments.byMethod`, `payments.revenueByTier`, `payments.revenueByBillingCycle` should be charted.
- `alerts` should be shown as actionable cards or a warning list.

---

## 2. Overview

### Request

```http
GET /api/admin/analytics/overview
Authorization: Bearer <token>
```

### Response

```json
{
  "generatedAt": "2026-04-16T10:00:00Z",
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
  "alerts": [],
  "topUsers": [],
  "popularLessons": [],
  "difficultLessons": [],
  "recentPayments": []
}
```

---

## 3. Users

### Request

```http
GET /api/admin/analytics/users
Authorization: Bearer <token>
```

### Response

```json
{
  "totalUsers": 1500,
  "activeUsers": 930,
  "pendingUsers": 48,
  "inactiveUsers": 22,
  "suspendedUsers": 7,
  "bannedUsers": 3,
  "verifiedUsers": 1375,
  "newUsersToday": 24,
  "newUsersThisMonth": 310,
  "dailyActiveUsers": 142,
  "monthlyActiveUsers": 500,
  "usersActiveInLast7Days": 310,
  "byStatus": [],
  "byAuthProvider": [],
  "byLevel": [],
  "byCountry": []
}
```

---

## 4. Lessons

### Request

```http
GET /api/admin/analytics/lessons
Authorization: Bearer <token>
```

### Response

```json
{
  "totalLessons": 180,
  "publishedLessons": 160,
  "draftLessons": 12,
  "archivedLessons": 8,
  "dictationLessons": 94,
  "shadowingLessons": 86,
  "premiumLessons": 35,
  "audioLessons": 128,
  "youtubeLessons": 52,
  "totalCompletions": 50200,
  "averageScore": 74.3,
  "byStatus": [],
  "byType": [],
  "byLevel": [],
  "byCategory": [],
  "byMediaType": [],
  "popularLessons": [],
  "difficultLessons": [],
  "recentLessons": []
}
```

### Suggested chart use

- `popularLessons` and `difficultLessons` should be rendered as horizontal bar charts or ranked lists.
- `byType`, `byLevel`, `byCategory`, `byMediaType` should be rendered as donut/pie charts.
- `accessStats.topAccessedLessons` should be rendered as a ranked list or bar chart.
- `accessStats.byStatus` should be rendered as a small donut chart or stacked bar.

---

## 4.1 Lesson Access Analytics

### Request

```http
GET /api/admin/analytics/lessons/access
Authorization: Bearer <token>
```

### Response

```json
{
  "generatedAt": "2026-04-16T10:00:00Z",
  "totalAccessEvents": 3,
  "totalTrackedLessons": 2,
  "totalTrackedUsers": 3,
  "completedTrackers": 1,
  "inProgressTrackers": 1,
  "startedTrackers": 1,
  "topAccessedLessons": [
    {
      "lessonId": "b1b222aa-1111-2222-3333-444444444444",
      "title": "Daily Greeting",
      "lessonType": "Dictation",
      "level": "Beginner",
      "category": "daily",
      "accessCount": 2,
      "uniqueUsers": 2,
      "completedCount": 1,
      "inProgressCount": 1,
      "startedCount": 0,
      "completionRate": 50,
      "firstAccessedAt": "2026-04-10T00:00:00Z",
      "lastAccessedAt": "2026-04-16T09:00:00Z",
      "createdAt": "2026-04-01T00:00:00Z"
    }
  ],
  "recentAccessedLessons": [],
  "byStatus": [
    { "key": "Completed", "count": 1 },
    { "key": "InProgress", "count": 1 },
    { "key": "Started", "count": 1 }
  ]
}
```

### Interpretation

- `accessCount` is based on lesson tracker rows created when a logged-in user opens lesson detail or lesson segments.
- `uniqueUsers` is the number of distinct users who accessed that lesson.
- Use `accessCount` for popularity and `completionRate` for engagement quality.

---

## 5. Vocabulary

### Request

```http
GET /api/admin/analytics/vocabulary
Authorization: Bearer <token>
```

### Response

```json
{
  "totalVocabulary": 8600,
  "dueVocabulary": 420,
  "overdueVocabulary": 112,
  "newVocabulary": 1300,
  "masteredVocabulary": 4300,
  "learningVocabulary": 3000,
  "recentVocabulary": 420,
  "vocabularyLessons": 180,
  "vocabularyTopics": 24,
  "topTopics": [],
  "topLessons": [],
  "recentItems": []
}
```

---

## 6. Payments

### Request

```http
GET /api/admin/analytics/payments
Authorization: Bearer <token>
```

### Response

```json
{
  "totalPayments": 320,
  "completedPayments": 300,
  "pendingPayments": 6,
  "failedPayments": 8,
  "refundedPayments": 6,
  "totalRevenue": 20000000,
  "todayRevenue": 1200000,
  "monthlyRevenue": 8000000,
  "averagePaymentAmount": 66666.7,
  "paymentsToday": 12,
  "paymentsThisMonth": 120,
  "byStatus": [],
  "byMethod": [],
  "revenueByTier": [],
  "revenueByBillingCycle": [],
  "recentPayments": []
}
```

---

## 7. Content

### Request

```http
GET /api/admin/analytics/content
Authorization: Bearer <token>
```

### Response

```json
{
  "blogs": {
    "totalBlogs": 42,
    "publishedBlogs": 38,
    "draftBlogs": 3,
    "archivedBlogs": 1,
    "totalViews": 120000,
    "averageViews": 2857.1,
    "byStatus": [],
    "popularBlogs": [],
    "recentBlogs": []
  },
  "notifications": {
    "totalNotifications": 5000,
    "unreadNotifications": 1400,
    "readNotifications": 3600,
    "notificationsToday": 120,
    "notificationsThisMonth": 2400,
    "byType": [],
    "byChannel": []
  },
  "engagement": {
    "usersWithProgress": 1300,
    "usersWithStreak": 1200,
    "usersWithAnalytics": 1100,
    "averagePoints": 640.4,
    "averageMinutes": 210.7,
    "averageLessonsCompleted": 8.4,
    "averageDictationScore": 80.1,
    "averageShadowingScore": 72.8,
    "averageCurrentStreak": 6.3,
    "longestStreak": 52,
    "topUsers": []
  }
}
```

---

## Validation rules

- Admin endpoints require authenticated admin tokens.
- All breakdown groups are server-side computed; FE should not re-derive them from names.
- For charts, prefer:
  - Bar chart for ranked lists
  - Donut/pie chart for categorical distributions
  - KPI card for summary metrics

---

## Request format reminder

These endpoints are `GET` only, so there is no JSON request body.

Example request:

```http
GET /api/admin/analytics/lessons
Authorization: Bearer <token>
```
