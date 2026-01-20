# DEMIF - Complete Project Task Breakdown (8 Weeks)

## Team Structure
- Leader (L): Core features, algorithms, integrations, security
- Member (M): CRUD, UI components, basic features

---

# WEEK 1: Project Setup & Foundation

## Backend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| BE-001 | Create ASP.NET Core 8 Web API project | L | 2 |
| BE-002 | Setup Clean Architecture (Domain, Application, Infrastructure, API) | L | 4 |
| BE-003 | Configure Entity Framework Core with SQL Server | L | 3 |
| BE-004 | Create all Entity models from database schema | M | 6 |
| BE-005 | Setup Repository pattern and Unit of Work | L | 4 |
| BE-006 | Configure Serilog logging | M | 2 |
| BE-007 | Setup CORS for frontend | M | 1 |
| BE-008 | Configure Swagger/OpenAPI documentation | M | 2 |
| BE-009 | Setup FluentValidation | L | 2 |
| BE-010 | Create Global Exception Handler | L | 3 |

## Frontend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| FE-001 | Setup API service layer (axios/fetch wrapper) | M | 3 |
| FE-002 | Create environment config (.env) | M | 1 |
| FE-003 | Setup auth context/provider | M | 3 |
| FE-004 | Create protected route middleware | M | 2 |

## Database Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| DB-001 | Run SQL migration script to create all tables | L | 2 |
| DB-002 | Seed SubscriptionPlans data | M | 1 |
| DB-003 | Seed 10 sample Lessons data | M | 3 |
| DB-004 | Seed 50 sample Vocabulary data | M | 2 |

**Week 1 Total: L=20h, M=26h**

---

# WEEK 2: Authentication & User Management

## Backend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| BE-011 | Setup Firebase Admin SDK for .NET | L | 4 |
| BE-012 | Implement JWT token generation | L | 3 |
| BE-013 | Implement JWT validation middleware | L | 3 |
| BE-014 | API: POST /auth/register | M | 4 |
| BE-015 | API: POST /auth/login | M | 4 |
| BE-016 | API: POST /auth/firebase (Google login) | M | 4 |
| BE-017 | API: GET /users/profile | M | 2 |
| BE-018 | API: PUT /users/profile | M | 3 |
| BE-019 | API: POST /auth/refresh-token | M | 3 |
| BE-020 | Add rate limiting middleware | L | 3 |

## Frontend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| FE-005 | Implement login page with API integration | M | 4 |
| FE-006 | Implement register page with API integration | M | 4 |
| FE-007 | Add Google login button with Firebase | M | 3 |
| FE-008 | Store JWT token (cookies/localStorage) | M | 2 |
| FE-009 | Add auth state persistence on refresh | M | 2 |
| FE-010 | Create profile page | M | 3 |
| FE-011 | Create profile edit form | M | 3 |

**Week 2 Total: L=13h, M=41h**

---

# WEEK 3: Lesson Management & Dictation Backend

## Backend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| BE-021 | API: GET /lessons (with filters) | M | 4 |
| BE-022 | API: GET /lessons/{id} | M | 2 |
| BE-023 | Implement Levenshtein Distance algorithm | L | 3 |
| BE-024 | Implement TextComparer.CalculateSimilarity | L | 2 |
| BE-025 | Implement DictationTemplateGenerator service | L | 6 |
| BE-026 | Implement blank selection algorithm by level | L | 4 |
| BE-027 | API: GET /lessons/{id}/template | L | 3 |
| BE-028 | Create DictationScoringService | L | 6 |
| BE-029 | Implement penalty calculations (plays, time) | L | 3 |
| BE-030 | API: POST /exercises/dictation/submit | L | 4 |
| BE-031 | Save UserExercise to database | M | 2 |
| BE-032 | API: GET /exercises/history | M | 3 |

## Frontend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| FE-012 | Create lessons list page with filters | M | 4 |
| FE-013 | Create lesson card component | M | 2 |
| FE-014 | Replace mock lessons with API data | M | 3 |

**Week 3 Total: L=31h, M=20h**

---

# WEEK 4: Dictation Frontend & User Progress

## Backend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| BE-033 | Implement UserProgressService | L | 4 |
| BE-034 | Update progress after exercise completion | L | 3 |
| BE-035 | Implement StreakCalculator service | L | 4 |
| BE-036 | API: GET /progress/summary | M | 3 |
| BE-037 | API: GET /streak/check | M | 3 |
| BE-038 | API: POST /streak/freeze | M | 2 |
| BE-039 | Add anti-cheat validation for submissions | L | 4 |

## Frontend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| FE-015 | Implement dictation blanks input component | M | 5 |
| FE-016 | Implement audio player with play count limit | M | 4 |
| FE-017 | Implement hint feature (show first letter) | M | 2 |
| FE-018 | Implement submit answers flow | M | 3 |
| FE-019 | Display scoring results with correct/wrong highlights | M | 4 |
| FE-020 | Create retry button functionality | M | 2 |
| FE-021 | Connect dashboard with real progress data | M | 4 |
| FE-022 | Display streak information on dashboard | M | 2 |

**Week 4 Total: L=15h, M=34h**

---

# WEEK 5: Shadowing Feature

## Backend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| BE-040 | Implement WordComparisonService | L | 6 |
| BE-041 | Implement word-by-word diff algorithm | L | 4 |
| BE-042 | Calculate accuracy percentage | L | 2 |
| BE-043 | Generate difference highlights | L | 3 |
| BE-044 | Generate feedback messages | L | 3 |
| BE-045 | API: POST /exercises/shadowing/compare | L | 4 |
| BE-046 | API: POST /exercises/shadowing/submit | L | 3 |
| BE-047 | Save shadowing results to database | M | 2 |

## Frontend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| FE-023 | Implement Web Speech API integration | M | 5 |
| FE-024 | Create recording start/stop controls | M | 3 |
| FE-025 | Display real-time transcript while speaking | M | 4 |
| FE-026 | Implement MediaRecorder for backup recording | M | 4 |
| FE-027 | Update ShadowingExercise component with API | M | 4 |
| FE-028 | Display comparison results | M | 3 |
| FE-029 | Highlight correct/wrong words | M | 3 |
| FE-030 | Show accuracy score and feedback | M | 2 |

**Week 5 Total: L=25h, M=30h**

---

# WEEK 6: YouTube Integration & Vocabulary

## Backend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| BE-048 | Setup YouTube Data API v3 | L | 3 |
| BE-049 | Implement YouTubeCaptionService | L | 6 |
| BE-050 | Parse VTT/SRT caption format | L | 4 |
| BE-051 | API: GET /youtube/transcript | L | 3 |
| BE-052 | API: POST /youtube/create-lesson | L | 4 |
| BE-053 | Store YouTube lessons in database | M | 2 |
| BE-054 | API: POST /vocabulary/save | M | 2 |
| BE-055 | API: GET /vocabulary/review | M | 3 |
| BE-056 | Implement SM-2 spaced repetition algorithm | L | 5 |
| BE-057 | API: POST /vocabulary/review-result | M | 3 |

## Frontend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| FE-031 | Create YouTube URL input form | M | 2 |
| FE-032 | Integrate YouTube IFrame player | M | 4 |
| FE-033 | Sync transcript with video time | M | 5 |
| FE-034 | Highlight current sentence in transcript | M | 3 |
| FE-035 | Implement practice mode (pause and speak) | M | 5 |
| FE-036 | Per-segment scoring display | M | 3 |
| FE-037 | Create vocabulary list page | M | 3 |
| FE-038 | Create vocabulary review flashcard UI | M | 4 |

**Week 6 Total: L=25h, M=34h**

---

# WEEK 7: Payment & Subscription

## Backend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| BE-058 | Setup SEPay account and API keys | L | 2 |
| BE-059 | API: GET /plans | M | 2 |
| BE-060 | Implement PaymentService | L | 4 |
| BE-061 | Generate unique payment reference | L | 2 |
| BE-062 | API: POST /payment/create | L | 4 |
| BE-063 | API: POST /payment/webhook (SEPay callback) | L | 6 |
| BE-064 | Verify SEPay signature | L | 3 |
| BE-065 | Activate subscription after payment | L | 4 |
| BE-066 | API: GET /payment/history | M | 2 |
| BE-067 | Implement subscription expiry check | L | 3 |
| BE-068 | Add premium feature access control | L | 4 |

## Frontend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| FE-039 | Create pricing/plans page | M | 4 |
| FE-040 | Create payment page with QR code display | M | 4 |
| FE-041 | Display bank transfer information | M | 2 |
| FE-042 | Implement payment status polling | M | 3 |
| FE-043 | Show payment success/failure message | M | 2 |
| FE-044 | Display subscription status on profile | M | 2 |
| FE-045 | Add premium badge indicators | M | 2 |

**Week 7 Total: L=32h, M=23h**

---

# WEEK 8: Leaderboard, Polish & Testing

## Backend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| BE-069 | Implement LeaderboardService | M | 4 |
| BE-070 | Leaderboard calculation scheduled job | M | 4 |
| BE-071 | API: GET /leaderboard (daily, weekly) | M | 3 |
| BE-072 | Performance optimization (caching) | L | 4 |
| BE-073 | Security audit and fixes | L | 4 |
| BE-074 | Write unit tests for scoring algorithms | L | 5 |
| BE-075 | Write integration tests for APIs | L | 5 |
| BE-076 | API documentation review | M | 2 |

## Frontend Tasks

| ID | Task | Owner | Hours |
|----|------|-------|-------|
| FE-046 | Create leaderboard page | M | 4 |
| FE-047 | Mobile responsive layout fixes | M | 6 |
| FE-048 | Touch-friendly controls for mobile | M | 4 |
| FE-049 | PWA manifest.json setup | M | 2 |
| FE-050 | Add loading states and skeletons | M | 3 |
| FE-051 | Error handling improvements | M | 3 |
| FE-052 | Performance optimization | M | 3 |
| FE-053 | E2E testing with Playwright | L | 5 |

**Week 8 Total: L=23h, M=38h**

---

# Summary

## Workload by Week

| Week | Leader (L) | Member (M) | Focus |
|------|------------|------------|-------|
| 1 | 20h | 26h | Project Setup |
| 2 | 13h | 41h | Authentication |
| 3 | 31h | 20h | Dictation Backend |
| 4 | 15h | 34h | Dictation Frontend |
| 5 | 25h | 30h | Shadowing |
| 6 | 25h | 34h | YouTube + Vocabulary |
| 7 | 32h | 23h | Payment |
| 8 | 23h | 38h | Polish + Testing |
| **Total** | **184h** | **246h** | |

## Workload by Role

| Role | Total Hours | Per Week Average |
|------|-------------|------------------|
| Leader | 184h | 23h/week |
| Member | 246h | 31h/week |

## Task Count

| Category | Leader | Member | Total |
|----------|--------|--------|-------|
| Backend | 45 | 31 | 76 |
| Frontend | 5 | 48 | 53 |
| Database | 1 | 3 | 4 |
| **Total** | **51** | **82** | **133** |

---

# Milestones

| Week | Milestone | Definition of Done |
|------|-----------|-------------------|
| 2 | Auth Complete | User can login, register, Google login working |
| 4 | Dictation MVP | User can complete dictation exercises with scoring |
| 5 | Shadowing MVP | User can record voice and get comparison results |
| 6 | YouTube MVP | User can create lessons from YouTube videos |
| 7 | Payment Live | User can purchase subscription via bank transfer |
| 8 | Production Ready | All features tested, mobile responsive, deployed |

---

# Notes

1. Hours are estimates, actual time may vary
2. Leader should review all Member PRs before merge
3. Weekly sync meetings recommended (Mon + Thu)
4. Use feature branches, merge to develop, then main
5. Deploy to staging after each sprint for testing
