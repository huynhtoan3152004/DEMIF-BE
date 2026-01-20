                                                                                                                                          # DEMIF - Business Analysis Document

## 1. Phân Tích Đối Thủ: DailyDictation.com

### 1.1 Tính Năng Hiện Có

| Feature | DailyDictation | Đánh Giá |
|---------|----------------|----------|
| Dictation exercises | ✅ Đầy đủ | Tốt, nhiều chủ đề |
| Multiple topics | ✅ 12+ categories | TOEIC, IELTS, News, TED |
| Video integration | ✅ YouTube | Nội dung phong phú |
| Mobile responsive | ✅ OK | Cơ bản |
| Free to use | ✅ 100% free | Ad-supported |

### 1.2 Điểm Yếu Cần Khai Thác

| Gap | Opportunity |
|-----|-------------|
| ❌ Không có Shadowing | DEMIF có luyện nói |
| ❌ Không AI feedback | DEMIF có pronunciation scoring |
| ❌ Không personalization | DEMIF có adaptive learning |
| ❌ Không gamification mạnh | DEMIF có streak, achievements |
| ❌ Không mobile app | DEMIF PWA first |
| ❌ UI tiếng Anh | DEMIF Vietnamese native |

---

## 2. DEMIF Core Differentiation

### 2.1 Unique Value Propositions

```mermaid
mindmap
  root((DEMIF))
    AI-Powered
      Speech-to-Text
      Pronunciation Scoring
      Personalized Feedback
    Gamification
      Daily Streak
      Leaderboard
      Achievements
      XP System
    Personalization
      Placement Test
      Adaptive Difficulty
      Learning Roadmap
    Vietnamese-First
      Native UI
      VN Payment
      Local Support
```

### 2.2 Feature Priority Matrix

| Feature | Impact | Effort | Priority |
|---------|--------|--------|----------|
| Dictation core | High | Medium | P0 |
| Shadowing + AI | High | High | P0 |
| Streak system | High | Low | P0 |
| Leaderboard | Medium | Low | P0 |
| Vocabulary SRS | Medium | Medium | P1 |
| Achievements | Medium | Medium | P2 |
| Placement test | High | High | P2 |
| Learning roadmap | High | High | P3 |

---

## 3. Chi Tiết Nghiệp Vụ

### 3.1 Dictation Flow

```mermaid
sequenceDiagram
    participant U as User
    participant FE as Frontend
    participant API as Backend
    participant DB as Database
    participant R2 as Cloudflare R2

    U->>FE: Chọn bài học
    FE->>API: GET /lessons/{id}
    API->>DB: Query Lessons
    DB-->>API: Lesson data
    API-->>FE: {lesson, audioUrl, dictationTemplate}
    
    FE->>R2: Stream audio
    R2-->>FE: Audio blob
    
    U->>FE: Nghe audio (max 3 lần)
    U->>FE: Điền blanks
    U->>FE: Submit
    
    FE->>API: POST /exercises/dictation/submit
    API->>API: Score calculation
    API->>DB: Save UserExercises
    API->>DB: Update UserProgress
    API->>DB: Update UserDailyActivity
    API-->>FE: {score, corrections, feedback}
    
    FE->>U: Hiển thị kết quả
```

#### Dictation Template Structure

```json
{
  "lessonId": "uuid",
  "segments": [
    {"text": "Hello, my name is ", "isBlank": false},
    {"text": "___", "isBlank": true, "answer": "Sarah", "hint": "S____", "index": 0},
    {"text": ". Nice to ", "isBlank": false},
    {"text": "___", "isBlank": true, "answer": "meet", "hint": "m___", "index": 1},
    {"text": " you.", "isBlank": false}
  ],
  "totalBlanks": 2
}
```

#### Scoring Algorithm

```csharp
public class DictationScorer
{
    public DictationResult Calculate(List<string> userAnswers, List<BlankAnswer> correctAnswers, int playsUsed)
    {
        int correct = 0;
        var details = new List<BlankResult>();
        
        for (int i = 0; i < correctAnswers.Count; i++)
        {
            var userAns = userAnswers.ElementAtOrDefault(i)?.Trim().ToLower() ?? "";
            var correctAns = correctAnswers[i].Answer.ToLower();
            
            bool isCorrect = userAns == correctAns;
            if (isCorrect) correct++;
            
            details.Add(new BlankResult
            {
                Index = i,
                UserAnswer = userAnswers.ElementAtOrDefault(i),
                CorrectAnswer = correctAnswers[i].Answer,
                IsCorrect = isCorrect
            });
        }
        
        // Base score
        decimal baseScore = (decimal)correct / correctAnswers.Count * 100;
        
        // Penalty for extra plays
        int playPenalty = (playsUsed - 1) * 5; // -5 per extra play
        
        int finalScore = Math.Max(0, (int)baseScore - playPenalty);
        
        return new DictationResult
        {
            Score = finalScore,
            TotalBlanks = correctAnswers.Count,
            CorrectBlanks = correct,
            PlaysUsed = playsUsed,
            Details = details
        };
    }
}
```

### 3.2 Shadowing Flow

```mermaid
sequenceDiagram
    participant U as User
    participant FE as Frontend
    participant API as Backend
    participant R2 as Cloudflare R2
    participant Azure as Azure Speech

    U->>FE: Chọn bài shadowing
    FE->>API: GET /lessons/{id}
    API-->>FE: Lesson data
    
    U->>FE: Nghe native audio
    U->>FE: Bắt đầu ghi âm
    FE->>FE: MediaRecorder API
    U->>FE: Dừng ghi âm
    
    FE->>R2: Upload recording
    R2-->>FE: Recording URL
    
    FE->>API: POST /exercises/shadowing/submit
    API->>R2: Download recording
    API->>Azure: Speech-to-Text (Whisper)
    Azure-->>API: Transcribed text
    
    API->>Azure: Pronunciation Assessment
    Azure-->>API: Scores (accuracy, fluency, pronunciation)
    
    API->>API: Compare transcripts
    API->>DB: Save UserExercises
    API->>DB: Update UserProgress
    API-->>FE: {scores, feedback, comparison}
    
    FE->>U: Hiển thị feedback chi tiết
```

#### AI Scoring Components

| Component | Source | Description |
|-----------|--------|-------------|
| Word Accuracy | Local algorithm | Compare user vs original transcript |
| Pronunciation | Azure Speech | PronunciationAssessment API |
| Fluency | Azure Speech | Words per minute, pauses |
| Intonation | Azure Speech | Prosody analysis |

#### Shadowing Result Structure

```json
{
  "exerciseId": "uuid",
  "score": 81,
  "breakdown": {
    "wordAccuracy": 85,
    "pronunciation": 78,
    "fluency": 82,
    "intonation": 79
  },
  "comparison": {
    "originalText": "Hello, my name is Sarah.",
    "userText": "Hello, my name is Sara.",
    "differences": [
      {"word": "Sarah", "userSaid": "Sara", "type": "mispronunciation"}
    ]
  },
  "feedback": [
    {"type": "positive", "message": "Phát âm tốt từ 'Hello'"},
    {"type": "improvement", "message": "Cần cải thiện: Sarah → Sara"}
  ],
  "recordingUrl": "https://r2.demif.app/recordings/xxx.webm"
}
```

### 3.3 Streak Logic

```mermaid
flowchart TD
    A[User hoàn thành exercise] --> B{Kiểm tra ngày}
    B -->|Same day| C[Update DailyActivity]
    B -->|Yesterday| D[CurrentStreak++ ]
    B -->|2+ days ago| E{Có Freeze?}
    E -->|Yes| F[Use Freeze, Keep Streak]
    E -->|No| G[Reset Streak = 1]
    
    D --> H[Update LongestStreak if needed]
    F --> H
    G --> H
    H --> I[Save to UserStreaks]
```

#### Streak Rules

1. **Maintain streak**: Hoàn thành ít nhất 1 exercise/ngày
2. **Streak freeze**: Premium users có 2-5 freezes/tháng
3. **Reset rules**: Miss 2+ ngày = reset về 1
4. **Bonus points**: 7-day streak = +50 XP, 30-day = +200 XP

---

## 4. Technology Deep Dive

### 4.1 Audio Processing Pipeline

```mermaid
flowchart LR
    subgraph Content Creation
        A[Raw Audio MP3] --> B[Upload to R2]
        B --> C[Generate Transcript]
        C --> D[Create DictationTemplate]
    end
    
    subgraph User Recording
        E[Browser MediaRecorder] --> F[WebM/Opus format]
        F --> G[Upload to R2]
        G --> H[Azure Speech API]
    end
```

#### Audio Format Recommendations

| Use Case | Format | Why |
|----------|--------|-----|
| Lesson audio | MP3 | Wide compatibility |
| User recording | WebM/Opus | Smaller size, browser native |
| Storage | Original | No re-encoding |

### 4.2 AI Integration Architecture

```mermaid
flowchart TB
    subgraph "Azure Speech Services"
        A[Speech-to-Text] 
        B[Pronunciation Assessment]
    end
    
    subgraph "Backend Processing"
        C[Audio Normalization]
        D[Text Comparison]
        E[Score Calculation]
    end
    
    C --> A
    A --> D
    D --> B
    B --> E
```

#### Azure Speech Pricing (Free Tier)

| Service | Free Tier | Over Limit |
|---------|-----------|------------|
| Speech-to-Text | 5 hours/month | $1/hour |
| Pronunciation Assessment | 5 hours/month | $1/hour |

**Strategy**: Cache results, limit attempts per user/day

### 4.3 Caching Strategy với Redis

```mermaid
flowchart LR
    A[API Request] --> B{Redis Cache?}
    B -->|Hit| C[Return cached]
    B -->|Miss| D[Query DB]
    D --> E[Set Cache]
    E --> C
```

#### Cache Keys

| Key Pattern | TTL | Data |
|-------------|-----|------|
| `lesson:{id}` | 1 hour | Lesson details |
| `leaderboard:{period}` | 5 min | Top 100 |
| `progress:{userId}` | 10 min | User progress |
| `streak:{userId}` | 1 hour | Streak info |

### 4.4 SEPay Integration

```mermaid
sequenceDiagram
    participant U as User
    participant FE as Frontend
    participant API as Backend
    participant SEPay as SEPay Gateway
    participant Bank as VN Bank

    U->>FE: Chọn gói Premium
    FE->>API: POST /payments/create
    API->>API: Generate unique reference
    API->>DB: Create Payment (pending)
    API-->>FE: {paymentUrl, qrCode, reference}
    
    FE->>U: Hiển thị QR/Bank info
    U->>Bank: Chuyển khoản với reference
    Bank->>SEPay: Transaction notification
    SEPay->>API: POST /payments/webhook
    API->>API: Verify signature
    API->>DB: Update Payment (completed)
    API->>DB: Activate subscription
    API-->>SEPay: 200 OK
    
    Note over FE,API: Polling hoặc WebSocket
    FE->>API: GET /payments/{id}/status
    API-->>FE: {status: "completed"}
    FE->>U: Thông báo thành công
```

#### SEPay Webhook Payload

```json
{
  "id": 123456,
  "gateway": "SEPAY",
  "transactionDate": "2024-01-15 10:30:00",
  "accountNumber": "123456789",
  "code": null,
  "content": "DEMIF-PAY-ABC123XYZ", // Our reference
  "transferType": "in",
  "transferAmount": 199000,
  "accumulated": 199000,
  "subAccount": null,
  "referenceCode": "SEP123456",
  "description": "Chuyen tien"
}
```

---

## 5. Điểm Khó và Giải Pháp

### 5.1 Technical Challenges

| Challenge | Solution |
|-----------|----------|
| Audio latency | CDN (Cloudflare), chunked streaming |
| AI cost | Rate limiting, caching, free tier optimization |
| Recording quality | Client-side audio normalization |
| Real-time scoring | Async processing, optimistic UI |

### 5.2 Business Challenges

| Challenge | Solution |
|-----------|----------|
| Content creation | Start with 50 lessons, community contribution |
| User retention | Gamification, streak, notifications |
| Conversion to paid | Freemium model, trial period |
| Competition | Focus on Vietnamese market, AI features |

---

## 6. Metrics & KPIs

### 6.1 North Star Metric
**Weekly Active Learners (WAL)** - Users completing ≥3 exercises/week

### 6.2 Supporting Metrics

| Category | Metric | Target |
|----------|--------|--------|
| Acquisition | Sign-ups/week | 200 |
| Activation | First lesson completion | 70% |
| Retention | Week 1 retention | 40% |
| Engagement | Exercises/user/week | 5 |
| Revenue | MRR | $500 (month 3) |
| NPS | Score | +40 |

---

## 7. Implementation Roadmap

```mermaid
gantt
    title DEMIF Development Roadmap
    dateFormat  YYYY-MM-DD
    section Phase 1 MVP
    Database & Backend Setup    :a1, 2024-02-01, 14d
    Dictation Core              :a2, after a1, 14d
    Shadowing + AI              :a3, after a2, 14d
    Payment & Polish            :a4, after a3, 14d
    section Phase 2
    Vocabulary SRS              :b1, after a4, 7d
    Achievements                :b2, after b1, 7d
    Mobile Optimization         :b3, after b2, 14d
    section Phase 3
    Placement Test              :c1, after b3, 14d
    Community Features          :c2, after c1, 14d
```
