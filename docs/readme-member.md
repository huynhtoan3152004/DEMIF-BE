# DEMIF Backend - Member Tasks

## Role: Team Member
CRUD operations, API endpoints, basic services, UI integration support.

---

## Week 1: Entity Models & Basic Setup

| ID | Task | Hours | Status |
|----|------|-------|--------|
| M-001 | Create User entity model | 1 | [ ] |
| M-002 | Create Lesson entity model | 1 | [ ] |
| M-003 | Create UserExercise entity model | 1 | [ ] |
| M-004 | Create UserProgress entity model | 1 | [ ] |
| M-005 | Create UserStreak entity model | 1 | [ ] |
| M-006 | Create Vocabulary entity model | 1 | [ ] |
| M-007 | Create UserVocabulary entity model | 1 | [ ] |
| M-008 | Create SubscriptionPlan entity model | 1 | [ ] |
| M-009 | Create UserSubscription entity model | 1 | [ ] |
| M-010 | Create Payment entity model | 1 | [ ] |
| M-011 | Create Leaderboard entity model | 1 | [ ] |
| M-012 | Create UserDailyActivity entity model | 1 | [ ] |
| M-013 | Configure Serilog logging | 2 | [ ] |
| M-014 | Setup CORS for frontend | 1 | [ ] |
| M-015 | Configure Swagger/OpenAPI documentation | 2 | [ ] |
| M-016 | Seed SubscriptionPlans data | 1 | [ ] |
| M-017 | Seed 10 sample Lessons data | 3 | [ ] |
| M-018 | Seed 50 sample Vocabulary data | 2 | [ ] |

**Week 1 Total: 23 hours**

---

## Week 2: Authentication APIs

| ID | Task | Hours | Status |
|----|------|-------|--------|
| M-019 | Create RegisterRequest DTO | 1 | [ ] |
| M-020 | Create LoginRequest DTO | 1 | [ ] |
| M-021 | Create AuthResponse DTO | 1 | [ ] |
| M-022 | Create UserProfileDto | 1 | [ ] |
| M-023 | API: POST /auth/register | 4 | [ ] |
| M-024 | API: POST /auth/login | 4 | [ ] |
| M-025 | API: POST /auth/firebase (Google login) | 4 | [ ] |
| M-026 | API: GET /users/profile | 2 | [ ] |
| M-027 | API: PUT /users/profile | 3 | [ ] |
| M-028 | API: POST /auth/refresh-token | 3 | [ ] |
| M-029 | Create RegisterValidator | 1 | [ ] |
| M-030 | Create LoginValidator | 1 | [ ] |

**Week 2 Total: 26 hours**

---

## Week 3: Lesson Management APIs

| ID | Task | Hours | Status |
|----|------|-------|--------|
| M-031 | Create LessonListDto | 1 | [ ] |
| M-032 | Create LessonDetailDto | 1 | [ ] |
| M-033 | Create LessonFilterRequest | 1 | [ ] |
| M-034 | Implement ILessonRepository | 2 | [ ] |
| M-035 | API: GET /lessons (with level, category filters) | 4 | [ ] |
| M-036 | API: GET /lessons/{id} | 2 | [ ] |
| M-037 | Create ExerciseHistoryDto | 1 | [ ] |
| M-038 | API: GET /exercises/history | 3 | [ ] |
| M-039 | Save UserExercise to database | 2 | [ ] |

**Week 3 Total: 17 hours**

---

## Week 4: Progress & Streak APIs

| ID | Task | Hours | Status |
|----|------|-------|--------|
| M-040 | Create ProgressSummaryDto | 1 | [ ] |
| M-041 | Create StreakDto | 1 | [ ] |
| M-042 | Implement IUserProgressRepository | 2 | [ ] |
| M-043 | Implement IUserStreakRepository | 2 | [ ] |
| M-044 | API: GET /progress/summary | 3 | [ ] |
| M-045 | API: GET /streak/check | 3 | [ ] |
| M-046 | API: POST /streak/freeze | 2 | [ ] |
| M-047 | Create DictationSubmissionValidator | 2 | [ ] |

**Week 4 Total: 16 hours**

---

## Week 5: Shadowing Support

| ID | Task | Hours | Status |
|----|------|-------|--------|
| M-048 | Create ShadowingCompareRequest DTO | 1 | [ ] |
| M-049 | Create ShadowingResultDto | 1 | [ ] |
| M-050 | Create WordDifferenceDto | 1 | [ ] |
| M-051 | Implement IUserExerciseRepository for shadowing | 2 | [ ] |
| M-052 | Save shadowing results to database | 2 | [ ] |
| M-053 | Create ShadowingSubmissionValidator | 2 | [ ] |

**Week 5 Total: 9 hours**

---

## Week 6: Vocabulary APIs

| ID | Task | Hours | Status |
|----|------|-------|--------|
| M-054 | Create VocabularyDto | 1 | [ ] |
| M-055 | Create SaveVocabularyRequest | 1 | [ ] |
| M-056 | Create ReviewResultRequest | 1 | [ ] |
| M-057 | Implement IVocabularyRepository | 2 | [ ] |
| M-058 | Implement IUserVocabularyRepository | 2 | [ ] |
| M-059 | API: POST /vocabulary/save | 2 | [ ] |
| M-060 | API: GET /vocabulary/review | 3 | [ ] |
| M-061 | API: POST /vocabulary/review-result | 3 | [ ] |
| M-062 | Store YouTube lessons in database | 2 | [ ] |

**Week 6 Total: 17 hours**

---

## Week 7: Payment & Subscription APIs

| ID | Task | Hours | Status |
|----|------|-------|--------|
| M-063 | Create SubscriptionPlanDto | 1 | [ ] |
| M-064 | Create PaymentHistoryDto | 1 | [ ] |
| M-065 | Implement ISubscriptionPlanRepository | 2 | [ ] |
| M-066 | Implement IPaymentRepository | 2 | [ ] |
| M-067 | API: GET /plans | 2 | [ ] |
| M-068 | API: GET /payment/history | 2 | [ ] |

**Week 7 Total: 10 hours**

---

## Week 8: Leaderboard & Documentation

| ID | Task | Hours | Status |
|----|------|-------|--------|
| M-069 | Create LeaderboardEntryDto | 1 | [ ] |
| M-070 | Implement ILeaderboardRepository | 2 | [ ] |
| M-071 | Implement LeaderboardService | 4 | [ ] |
| M-072 | Create leaderboard calculation logic | 4 | [ ] |
| M-073 | API: GET /leaderboard (daily, weekly) | 3 | [ ] |
| M-074 | API documentation review and update | 2 | [ ] |
| M-075 | Code cleanup and refactoring | 2 | [ ] |

**Week 8 Total: 18 hours**

---

## Summary

| Week | Focus | Hours |
|------|-------|-------|
| 1 | Entity Models & Basic Setup | 23 |
| 2 | Authentication APIs | 26 |
| 3 | Lesson Management APIs | 17 |
| 4 | Progress & Streak APIs | 16 |
| 5 | Shadowing Support | 9 |
| 6 | Vocabulary APIs | 17 |
| 7 | Payment & Subscription APIs | 10 |
| 8 | Leaderboard & Documentation | 18 |
| **Total** | | **136 hours** |

---

## Key Deliverables

1. All entity models matching database schema
2. All repository implementations
3. All DTO classes for API requests/responses
4. All validators for input validation
5. CRUD APIs for:
   - Users/Profile
   - Lessons
   - Exercises history
   - Progress/Streak
   - Vocabulary
   - Plans/Payment history
   - Leaderboard

---

## Notes

1. Follow Clean Architecture structure set by Leader
2. Use FluentValidation for all request validation
3. Use AutoMapper for entity-to-DTO mapping
4. Write XML comments for Swagger documentation
5. Create PR for Leader review before merging
