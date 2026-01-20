# DEMIF Backend - Leader Tasks

## Role: Team Leader
Setup core architecture, algorithms, external integrations, security, payment.

---

## Week 1: Project Foundation

| ID | Task | Hours | Status |
|----|------|-------|--------|
| L-001 | Create ASP.NET Core 8 Web API project structure | 2 | [ ] |
| L-002 | Setup Clean Architecture (Domain, Application, Infrastructure, API layers) | 4 | [ ] |
| L-003 | Configure Entity Framework Core with SQL Server connection | 3 | [ ] |
| L-004 | Setup Repository pattern and Unit of Work | 4 | [ ] |
| L-005 | Setup FluentValidation for input validation | 2 | [ ] |
| L-006 | Create Global Exception Handler middleware | 3 | [ ] |
| L-007 | Run SQL migration script to create all tables | 2 | [ ] |

**Week 1 Total: 20 hours**

---

## Week 2: Authentication & Security

| ID | Task | Hours | Status |
|----|------|-------|--------|
| L-008 | Setup Firebase Admin SDK for .NET | 4 | [ ] |
| L-009 | Implement JWT token generation service | 3 | [ ] |
| L-010 | Implement JWT validation middleware | 3 | [ ] |
| L-011 | Add rate limiting middleware | 3 | [ ] |

**Week 2 Total: 13 hours**

---

## Week 3: Dictation Core Algorithms

| ID | Task | Hours | Status |
|----|------|-------|--------|
| L-012 | Implement Levenshtein Distance algorithm | 3 | [ ] |
| L-013 | Implement TextComparer.CalculateSimilarity method | 2 | [ ] |
| L-014 | Implement DictationTemplateGenerator service | 6 | [ ] |
| L-015 | Implement blank selection algorithm by level (15%, 35%, 60%, 100%) | 4 | [ ] |
| L-016 | API: GET /lessons/{id}/template - Generate dictation template | 3 | [ ] |
| L-017 | Create DictationScoringService | 6 | [ ] |
| L-018 | Implement penalty calculations (extra plays, overtime) | 3 | [ ] |
| L-019 | API: POST /exercises/dictation/submit - Score dictation | 4 | [ ] |

**Week 3 Total: 31 hours**

---

## Week 4: Progress & Anti-Cheat

| ID | Task | Hours | Status |
|----|------|-------|--------|
| L-020 | Implement UserProgressService | 4 | [ ] |
| L-021 | Update progress after exercise completion | 3 | [ ] |
| L-022 | Implement StreakCalculator service | 4 | [ ] |
| L-023 | Add anti-cheat validation for submissions | 4 | [ ] |

**Week 4 Total: 15 hours**

---

## Week 5: Shadowing Core Algorithms

| ID | Task | Hours | Status |
|----|------|-------|--------|
| L-024 | Implement WordComparisonService | 6 | [ ] |
| L-025 | Implement word-by-word diff algorithm | 4 | [ ] |
| L-026 | Calculate accuracy percentage | 2 | [ ] |
| L-027 | Generate difference highlights (correct, wrong, missed) | 3 | [ ] |
| L-028 | Generate feedback messages | 3 | [ ] |
| L-029 | API: POST /exercises/shadowing/compare | 4 | [ ] |
| L-030 | API: POST /exercises/shadowing/submit | 3 | [ ] |

**Week 5 Total: 25 hours**

---

## Week 6: YouTube Integration

| ID | Task | Hours | Status |
|----|------|-------|--------|
| L-031 | Setup YouTube Data API v3 credentials | 3 | [ ] |
| L-032 | Implement YouTubeCaptionService | 6 | [ ] |
| L-033 | Parse VTT/SRT caption format to segments | 4 | [ ] |
| L-034 | API: GET /youtube/transcript | 3 | [ ] |
| L-035 | API: POST /youtube/create-lesson | 4 | [ ] |
| L-036 | Implement SM-2 spaced repetition algorithm for vocabulary | 5 | [ ] |

**Week 6 Total: 25 hours**

---

## Week 7: Payment Integration

| ID | Task | Hours | Status |
|----|------|-------|--------|
| L-037 | Setup SEPay account and API keys | 2 | [ ] |
| L-038 | Implement PaymentService | 4 | [ ] |
| L-039 | Generate unique payment reference code | 2 | [ ] |
| L-040 | API: POST /payment/create | 4 | [ ] |
| L-041 | API: POST /payment/webhook (SEPay callback) | 6 | [ ] |
| L-042 | Verify SEPay signature for security | 3 | [ ] |
| L-043 | Activate subscription after successful payment | 4 | [ ] |
| L-044 | Implement subscription expiry check | 3 | [ ] |
| L-045 | Add premium feature access control | 4 | [ ] |

**Week 7 Total: 32 hours**

---

## Week 8: Testing & Optimization

| ID | Task | Hours | Status |
|----|------|-------|--------|
| L-046 | Performance optimization (caching with Redis) | 4 | [ ] |
| L-047 | Security audit and fixes | 4 | [ ] |
| L-048 | Write unit tests for Levenshtein algorithm | 2 | [ ] |
| L-049 | Write unit tests for DictationScoringService | 3 | [ ] |
| L-050 | Write unit tests for WordComparisonService | 3 | [ ] |
| L-051 | Write integration tests for core APIs | 5 | [ ] |
| L-052 | E2E testing coordination | 2 | [ ] |

**Week 8 Total: 23 hours**

---

## Summary

| Week | Focus | Hours |
|------|-------|-------|
| 1 | Project Foundation | 20 |
| 2 | Authentication & Security | 13 |
| 3 | Dictation Core Algorithms | 31 |
| 4 | Progress & Anti-Cheat | 15 |
| 5 | Shadowing Core Algorithms | 25 |
| 6 | YouTube Integration | 25 |
| 7 | Payment Integration | 32 |
| 8 | Testing & Optimization | 23 |
| **Total** | | **184 hours** |

---

## Key Deliverables

1. Clean Architecture project structure
2. Levenshtein similarity algorithm
3. Dictation template generator and scorer
4. Word comparison for shadowing
5. YouTube caption parser
6. SEPay payment integration
7. JWT authentication system
8. Anti-cheat system
