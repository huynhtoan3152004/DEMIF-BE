# DEMIF - Updated Jira Backlog (C# Only)

## 📌 Sprint Overview

| Sprint | Duration | Focus | Key Deliverables |
|--------|----------|-------|------------------|
| Sprint 1 | Week 1-2 | Foundation | Backend setup, Auth, DB |
| Sprint 2 | Week 3-4 | Dictation | Core dictation feature |
| Sprint 3 | Week 5-6 | Shadowing | Voice compare feature |
| Sprint 4 | Week 7-8 | YouTube + Polish | YouTube lessons, Payment |
| Sprint 5 | Week 9-10 | AI/RAG | N8N, Roadmap, Recommendations |
| Sprint 6 | Week 11-12 | Mobile | PWA, Mobile optimization |

---

## Sprint 1: Foundation (Week 1-2)

### 🔧 Backend Setup
| ID | Task | Size | Priority |
|----|------|------|----------|
| S1-01 | Tạo ASP.NET Core 8 Web API project | S | P0 |
| S1-02 | Setup Clean Architecture (Domain, Application, Infrastructure) | M | P0 |
| S1-03 | Configure Entity Framework Core + SQL Server | S | P0 |
| S1-04 | Tạo tất cả Entity models từ database schema | M | P0 |
| S1-05 | Setup Repository pattern | M | P0 |
| S1-06 | Configure Serilog logging | S | P1 |
| S1-07 | Setup CORS cho frontend | S | P0 |
| S1-08 | Configure Swagger/OpenAPI | S | P1 |

### 🔐 Authentication
| ID | Task | Size | Priority |
|----|------|------|----------|
| S1-09 | Setup Firebase Admin SDK cho .NET | M | P0 |
| S1-10 | API: POST /auth/register (email) | M | P0 |
| S1-11 | API: POST /auth/login (email) | M | P0 |
| S1-12 | API: POST /auth/firebase (Google login) | M | P0 |
| S1-13 | JWT token generation và validation | M | P0 |
| S1-14 | API: GET /auth/me (current user) | S | P0 |
| S1-15 | API: POST /auth/refresh-token | S | P1 |

### 🗄️ Database
| ID | Task | Size | Priority |
|----|------|------|----------|
| S1-16 | Chạy SQL migration script | S | P0 |
| S1-17 | Seed SubscriptionPlans data | S | P0 |
| S1-18 | Seed sample Lessons data (5 lessons) | S | P0 |
| S1-19 | Seed sample Vocabulary data | S | P1 |

### 🎨 Frontend Connection
| ID | Task | Size | Priority |
|----|------|------|----------|
| S1-20 | Setup API service layer trong Next.js | M | P0 |
| S1-21 | Implement login/register flow | M | P0 |
| S1-22 | Store JWT token (localStorage/cookies) | S | P0 |
| S1-23 | Protected routes middleware | M | P0 |

---

## Sprint 2: Dictation Feature (Week 3-4)

### 📝 Dictation Backend
| ID | Task | Size | Priority |
|----|------|------|----------|
| S2-01 | API: GET /lessons - List with filters | M | P0 |
| S2-02 | API: GET /lessons/{id} | S | P0 |
| S2-03 | API: GET /lessons/recommended | M | P1 |
| S2-04 | Service: DictationTemplateGenerator | L | P0 |
| S2-05 | Algorithm: Smart blank selection by level | M | P0 |
| S2-06 | API: POST /exercises/dictation/submit | L | P0 |
| S2-07 | Service: DictationScorer (Levenshtein) | M | P0 |
| S2-08 | Save UserExercises result | S | P0 |
| S2-09 | Update UserProgress after exercise | M | P0 |
| S2-10 | Update UserDailyActivity | S | P0 |

### 🎮 Dictation Frontend
| ID | Task | Size | Priority |
|----|------|------|----------|
| S2-11 | Replace mock lessons với API data | M | P0 |
| S2-12 | Implement blanks input component | M | P0 |
| S2-13 | Audio player với play count limit | M | P0 |
| S2-14 | Submit answers flow | M | P0 |
| S2-15 | Display scoring results | M | P0 |
| S2-16 | Show correct answers highlight | M | P0 |
| S2-17 | Hint feature (show first letter) | S | P1 |

### 📊 Progress Tracking
| ID | Task | Size | Priority |
|----|------|------|----------|
| S2-18 | API: GET /progress/me | S | P0 |
| S2-19 | API: GET /progress/streak | S | P0 |
| S2-20 | Service: StreakCalculator | M | P0 |
| S2-21 | Dashboard: Connect với real progress data | M | P0 |

---

## Sprint 3: Shadowing Feature (Week 5-6)

### 🎤 Voice Recording (Frontend)
| ID | Task | Size | Priority |
|----|------|------|----------|
| S3-01 | Web Speech API integration | M | P0 |
| S3-02 | MediaRecorder for backup recording | M | P1 |
| S3-03 | Real-time transcript display | M | P0 |
| S3-04 | Recording start/stop controls | S | P0 |
| S3-05 | Audio playback of recording | S | P1 |

### 🔄 Text Comparison (Backend)
| ID | Task | Size | Priority |
|----|------|------|----------|
| S3-06 | API: POST /exercises/shadowing/compare | M | P0 |
| S3-07 | Service: TextComparisonService | L | P0 |
| S3-08 | Algorithm: Word-by-word diff | M | P0 |
| S3-09 | Calculate accuracy percentage | S | P0 |
| S3-10 | Generate difference highlights | M | P0 |
| S3-11 | Generate feedback messages | M | P1 |
| S3-12 | Save shadowing results | S | P0 |

### 🎯 Shadowing UI
| ID | Task | Size | Priority |
|----|------|------|----------|
| S3-13 | Update ShadowingExercise component | L | P0 |
| S3-14 | Display comparison results | M | P0 |
| S3-15 | Highlight correct/wrong words | M | P0 |
| S3-16 | Show accuracy score | S | P0 |
| S3-17 | Retry button | S | P0 |

---

## Sprint 4: YouTube + Payment (Week 7-8)

### 📺 YouTube Integration
| ID | Task | Size | Priority |
|----|------|------|----------|
| S4-01 | Setup YouTube Data API v3 | M | P0 |
| S4-02 | Service: YouTubeCaptionService (C#) | L | P0 |
| S4-03 | Parse VTT/SRT captions | M | P0 |
| S4-04 | API: POST /youtube/create-lesson | L | P0 |
| S4-05 | Store YouTube lessons in DB | M | P0 |
| S4-06 | API: GET /youtube/lessons | S | P0 |

### 📺 YouTube Frontend
| ID | Task | Size | Priority |
|----|------|------|----------|
| S4-07 | YouTube URL input form | S | P0 |
| S4-08 | YouTube IFrame player integration | M | P0 |
| S4-09 | Sync transcript với video time | L | P0 |
| S4-10 | Highlight current sentence | M | P0 |
| S4-11 | Practice mode (pause + speak) | L | P0 |
| S4-12 | Per-segment scoring | M | P0 |

### 💳 Payment (SEPay)
| ID | Task | Size | Priority |
|----|------|------|----------|
| S4-13 | SEPay account registration | S | P0 |
| S4-14 | API: POST /payments/create | M | P0 |
| S4-15 | Generate payment reference | S | P0 |
| S4-16 | API: POST /payments/webhook | M | P0 |
| S4-17 | Verify SEPay signature | M | P0 |
| S4-18 | Activate subscription after payment | M | P0 |
| S4-19 | Upgrade page UI | M | P0 |
| S4-20 | Display QR code/bank info | M | P0 |

---

## Sprint 5: AI/RAG + N8N (Week 9-10)

### 🤖 N8N Setup
| ID | Task | Size | Priority |
|----|------|------|----------|
| S5-01 | Deploy N8N on VPS (Docker) | M | P0 |
| S5-02 | Configure N8N webhooks | S | P0 |
| S5-03 | Connect N8N với OpenAI API | M | P0 |

### 📈 Learning Roadmap
| ID | Task | Size | Priority |
|----|------|------|----------|
| S5-04 | N8N workflow: Generate roadmap | L | P0 |
| S5-05 | API: POST /roadmap/generate | M | P0 |
| S5-06 | Store roadmap in DB | S | P0 |
| S5-07 | API: GET /roadmap/me | S | P0 |
| S5-08 | Roadmap UI page | L | P0 |

### 💡 AI Feedback
| ID | Task | Size | Priority |
|----|------|------|----------|
| S5-09 | N8N workflow: Pronunciation feedback | L | P1 |
| S5-10 | Integrate AI feedback in shadowing | M | P1 |
| S5-11 | Personalized improvement tips | M | P1 |

### 📊 Recommendations
| ID | Task | Size | Priority |
|----|------|------|----------|
| S5-12 | N8N workflow: Daily recommendations | M | P1 |
| S5-13 | API: GET /lessons/recommended | M | P1 |
| S5-14 | Homepage recommended lessons | M | P1 |

---

## Sprint 6: Mobile + Polish (Week 11-12)

### 📱 Mobile Optimization
| ID | Task | Size | Priority |
|----|------|------|----------|
| S6-01 | PWA manifest.json | S | P0 |
| S6-02 | Service worker for offline | M | P1 |
| S6-03 | Mobile responsive layouts | M | P0 |
| S6-04 | Touch-friendly controls | M | P0 |
| S6-05 | Mobile audio player | M | P0 |
| S6-06 | Mobile recording button | M | P0 |

### 🎮 Gamification
| ID | Task | Size | Priority |
|----|------|------|----------|
| S6-07 | API: GET /leaderboard | M | P0 |
| S6-08 | Leaderboard calculation job | M | P0 |
| S6-09 | Leaderboard UI | M | P0 |
| S6-10 | Streak freeze feature | M | P1 |
| S6-11 | Points/XP system | M | P1 |

### 🔧 Polish
| ID | Task | Size | Priority |
|----|------|------|----------|
| S6-12 | Error handling improvements | M | P1 |
| S6-13 | Loading states | S | P1 |
| S6-14 | Performance optimization | M | P1 |
| S6-15 | SEO meta tags | S | P1 |
| S6-16 | Analytics integration | S | P2 |

---

## 📋 Backlog (Future Sprints)

### Vocabulary & SRS
| ID | Task | Size | Priority |
|----|------|------|----------|
| BL-01 | API: CRUD /vocabulary | M | P2 |
| BL-02 | API: /user-vocabulary | M | P2 |
| BL-03 | SM-2 spaced repetition | L | P2 |
| BL-04 | Flashcard UI | M | P2 |
| BL-05 | Vocabulary quiz | M | P2 |

### Achievements
| ID | Task | Size | Priority |
|----|------|------|----------|
| BL-06 | Achievement definitions | M | P2 |
| BL-07 | Achievement unlock logic | M | P2 |
| BL-08 | Achievement notification | S | P2 |
| BL-09 | Achievement showcase UI | M | P2 |

### Placement Test
| ID | Task | Size | Priority |
|----|------|------|----------|
| BL-10 | Placement test questions | L | P2 |
| BL-11 | Adaptive testing algorithm | L | P2 |
| BL-12 | Level assignment | M | P2 |
| BL-13 | Placement test UI | L | P2 |

### Admin Panel
| ID | Task | Size | Priority |
|----|------|------|----------|
| BL-14 | Admin: Lesson CRUD | L | P2 |
| BL-15 | Admin: User management | M | P2 |
| BL-16 | Admin: Analytics dashboard | L | P3 |
| BL-17 | Admin: Content moderation | M | P3 |

---

## 📏 Size Reference

| Size | Story Points | Time Estimate |
|------|--------------|---------------|
| S (Small) | 1-2 | 2-4 hours |
| M (Medium) | 3-5 | 4-8 hours |
| L (Large) | 8-13 | 1-2 days |
| XL (Extra Large) | 13+ | 3+ days |

---

## 🎯 MVP Definition (Sprint 1-4)

After Sprint 4, you will have:
- ✅ User authentication (Email + Google)
- ✅ Dictation exercises with scoring
- ✅ Shadowing with voice recognition
- ✅ YouTube video lessons
- ✅ Payment integration
- ✅ Basic progress tracking
- ✅ Leaderboard

**Total: ~80 tasks, 8 weeks**
