# DEMIF - Progress Tracker

## ✅ Đã Hoàn Thành

### Sprint 1: Foundation ✅
- [x] Clean Architecture setup (Domain, Application, Infrastructure, Api)
- [x] Entity Framework Core + PostgreSQL
- [x] JWT Authentication (Email + Password)
- [x] Firebase Google Login
- [x] Role-based Authorization (Admin, Staff, User, Premium)
- [x] User CRUD APIs
- [x] Role management APIs

### Sprint 2: Dictation - Backend ✅
- [x] Lesson entity & repository
- [x] GET /api/lessons - List lessons
- [x] GET /api/lessons/{id} - Lesson detail
- [x] Admin: Lesson CRUD APIs
- [x] Premium content access control

### Sprint 4: Payment ✅ (Partial)
- [x] SubscriptionPlan entity & APIs
- [x] UserSubscription entity
- [x] Payment entity with SEPay fields
- [x] Subscribe API
- [x] SEPay Webhook handler
- [x] Auto-assign Premium role on payment
- [x] Admin: Subscription plan management

---

## 🔄 Đang Làm

### Sprint 2: Dictation - Completion
- [ ] DictationTemplateGenerator service
- [ ] POST /api/exercises/dictation/submit
- [ ] DictationScorer (Levenshtein)
- [ ] UserExercise tracking

---

## 📋 TODO - Roadmap

### Sprint 3: Shadowing (Week 5-6)
- [ ] POST /api/exercises/shadowing/compare
- [ ] TextComparisonService
- [ ] Word-by-word diff algorithm
- [ ] Accuracy calculation
- [ ] Frontend: Web Speech API integration

### Sprint 4: YouTube Integration
- [ ] YouTube Data API v3 setup
- [ ] YouTubeCaptionService
- [ ] VTT/SRT parser
- [ ] POST /api/youtube/create-lesson
- [ ] Sync transcript with video time

### Sprint 5: AI/RAG + N8N (Week 9-10)
- [ ] N8N deployment
- [ ] Learning roadmap generation
- [ ] AI pronunciation feedback
- [ ] Personalized recommendations

### Sprint 6: Mobile + Polish
- [ ] PWA manifest
- [ ] Mobile responsive
- [ ] Leaderboard API
- [ ] Gamification system

---

## 📊 Thống Kê

| Category | Done | Total | Progress |
|----------|------|-------|----------|
| Auth APIs | 8 | 8 | 100% |
| User APIs | 10 | 10 | 100% |
| Lesson APIs | 5 | 8 | 62% |
| Subscription APIs | 8 | 8 | 100% |
| Exercise APIs | 0 | 6 | 0% |
| YouTube APIs | 0 | 4 | 0% |
| **Total** | **31** | **44** | **70%** |
