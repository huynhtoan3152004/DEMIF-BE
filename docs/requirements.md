# DEMIF - Product Requirements Document (PRD)

## 1. Product Overview

### 1.1 Vision
DEMIF (Dictation & English Mastery Interactive Platform) là nền tảng học tiếng Anh tập trung vào kỹ năng nghe và nói thông qua phương pháp Dictation (nghe chép chính tả) và Shadowing (luyện nói theo).

### 1.2 Problem Statement
- Người học tiếng Anh Việt Nam thiếu công cụ luyện nghe hiệu quả
- Các nền tảng hiện tại (DailyDictation) không có AI feedback
- Thiếu personalization và gamification để duy trì động lực

### 1.3 Target Users
- **Primary**: Học sinh/sinh viên Việt Nam (16-25 tuổi)
- **Secondary**: Người đi làm cần cải thiện tiếng Anh giao tiếp

---

## 2. Core Features (MVP)

### 2.1 User Authentication
| ID | Feature | Priority | Description |
|----|---------|----------|-------------|
| AUTH-01 | Email Registration | P0 | Đăng ký bằng email/password |
| AUTH-02 | Google Login | P0 | Firebase Google OAuth |
| AUTH-03 | Profile Setup | P0 | Chọn level, daily goal |
| AUTH-04 | Password Reset | P1 | Email reset link |

### 2.2 Dictation Exercises
| ID | Feature | Priority | Description |
|----|---------|----------|-------------|
| DICT-01 | Lesson List | P0 | Danh sách bài học theo level/category |
| DICT-02 | Audio Player | P0 | Play/pause, tua, giới hạn 3 lần nghe |
| DICT-03 | Fill Blanks | P0 | Điền từ vào chỗ trống |
| DICT-04 | Auto Scoring | P0 | Tự động chấm điểm |
| DICT-05 | Show Corrections | P0 | Hiển thị đáp án đúng/sai |
| DICT-06 | Difficulty Filter | P1 | Lọc theo beginner/intermediate/advanced |
| DICT-07 | AI Hints | P2 | Gợi ý từ AI khi sai |

### 2.3 Shadowing Exercises
| ID | Feature | Priority | Description |
|----|---------|----------|-------------|
| SHAD-01 | Listen Native | P0 | Nghe audio gốc |
| SHAD-02 | Record Voice | P0 | Ghi âm giọng user |
| SHAD-03 | AI Transcription | P0 | Chuyển speech-to-text |
| SHAD-04 | Compare Texts | P0 | So sánh transcript |
| SHAD-05 | Pronunciation Score | P1 | Azure Speech scoring |
| SHAD-06 | Fluency Analysis | P1 | Phân tích độ trôi chảy |
| SHAD-07 | Playback Recording | P0 | Nghe lại bản ghi âm |

### 2.4 User Progress & Dashboard
| ID | Feature | Priority | Description |
|----|---------|----------|-------------|
| PROG-01 | Progress Overview | P0 | Tổng điểm, lessons hoàn thành |
| PROG-02 | Streak Counter | P0 | Đếm chuỗi ngày học |
| PROG-03 | Skill Breakdown | P1 | Chart kỹ năng |
| PROG-04 | Weekly Chart | P1 | Biểu đồ tuần |
| PROG-05 | Streak Freeze | P2 | Giữ streak khi bận |

### 2.5 Vocabulary
| ID | Feature | Priority | Description |
|----|---------|----------|-------------|
| VOCAB-01 | Word List | P0 | Danh sách từ vựng |
| VOCAB-02 | Add to List | P0 | Thêm từ vào danh sách cá nhân |
| VOCAB-03 | Spaced Repetition | P1 | Ôn tập SM-2 algorithm |
| VOCAB-04 | Flashcard Mode | P2 | Học bằng flashcard |

### 2.6 Gamification
| ID | Feature | Priority | Description |
|----|---------|----------|-------------|
| GAME-01 | Leaderboard | P0 | Bảng xếp hạng weekly |
| GAME-02 | Points System | P0 | Tích điểm khi hoàn thành |
| GAME-03 | Achievements | P2 | Huy hiệu thành tựu |
| GAME-04 | Daily Challenges | P2 | Thử thách hàng ngày |

### 2.7 Subscription & Payment
| ID | Feature | Priority | Description |
|----|---------|----------|-------------|
| PAY-01 | Plan Display | P0 | Hiển thị các gói |
| PAY-02 | SEPay Integration | P0 | Bank transfer Việt Nam |
| PAY-03 | Webhook Handler | P0 | Xử lý callback SEPay |
| PAY-04 | Subscription Status | P0 | Kiểm tra premium status |

---

## 3. Non-Functional Requirements

### 3.1 Performance
- Page load < 3s
- Audio streaming start < 1s
- API response < 500ms

### 3.2 Scalability
- Support 10K concurrent users (MVP)
- Horizontal scaling ready

### 3.3 Security
- HTTPS everywhere
- JWT authentication
- Input sanitization
- Rate limiting

### 3.4 Reliability
- 99.5% uptime SLA
- Daily database backup
- Error monitoring (Sentry)

---

## 4. Technical Requirements

### 4.1 Frontend
- Next.js 15 (existing)
- TypeScript
- TailwindCSS
- PWA support

### 4.2 Backend
- ASP.NET Core 8
- Entity Framework Core
- SQL Server
- Redis caching

### 4.3 External Services
- Firebase Auth
- Cloudflare R2 (storage)
- Azure Speech Services
- SEPay (payment)

---

## 5. Success Metrics

| Metric | Target (3 months) |
|--------|-------------------|
| Registered Users | 5,000 |
| Daily Active Users | 500 |
| Avg Session Duration | 15 min |
| Lesson Completion Rate | 60% |
| Paid Conversion | 3% |
| NPS Score | > 40 |

---

## 6. Timeline

### Phase 1: MVP (8 weeks)
- Week 1-2: Backend setup, Auth
- Week 3-4: Dictation core
- Week 5-6: Shadowing + AI
- Week 7: Payment integration
- Week 8: Testing + Polish

### Phase 2: Enhancement (4 weeks)
- Achievements
- Advanced analytics
- Mobile optimization

### Phase 3: Scale (ongoing)
- More content
- Community features
- Mobile app (React Native)
