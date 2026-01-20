# DEMIF - Technology Stack & Free Tier Strategy

## 🎯 Key Decision: 100% C# (No Python Required)

| Component | Solution | Python? |
|-----------|----------|---------|
| Speech-to-Text | Web Speech API (browser) | ❌ NO |
| YouTube Captions | YouTube Data API v3 (.NET) | ❌ NO |
| AI/RAG | N8N + OpenAI | ❌ NO |
| All Backend | ASP.NET Core 8 | ❌ NO |

## 1. Tổng Quan Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│              FRONTEND (Next.js / Mobile PWA)                    │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │  Web Speech API (Browser-native, FREE, No Backend)          ││
│  │  - User speaks → Browser transcribes → Send text to API    ││
│  └─────────────────────────────────────────────────────────────┘│
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    BACKEND (100% C#)                            │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  ASP.NET Core 8 Web API                                  │  │
│  │  - Text Comparison Service (compare spoken vs original)  │  │
│  │  - YouTube Data API v3 (fetch captions, no Python)       │  │
│  │  - N8N webhook calls (AI/RAG features)                   │  │
│  └──────────────────────────────────────────────────────────┘  │
│         ┌────────────────────┼────────────────────┐            │
│         ▼                    ▼                    ▼            │
│  ┌────────────┐      ┌────────────┐      ┌────────────┐       │
│  │ SQL Server │      │    N8N     │      │ Cloudflare │       │
│  │  Express   │      │ (AI/RAG)   │      │     R2     │       │
│  └────────────┘      └────────────┘      └────────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Chi Tiết Từng Service

### 2.1 Frontend Hosting: Vercel (FREE)

| Feature | Free Tier | Limit |
|---------|-----------|-------|
| Bandwidth | 100GB/month | Đủ cho MVP |
| Serverless Functions | 100GB-Hours | Đủ |
| Builds | 6000 min/month | Đủ |
| SSL | ✅ Included | Auto |
| CDN | ✅ Global | Fast |

**Điểm mạnh:**
- Zero config deployment
- Automatic CI/CD từ GitHub
- Edge functions cho performance
- Preview deployments

**Điểm khó:**
- Cold start cho functions
- Limited function runtime (10s free tier)

### 2.2 Authentication: Firebase (FREE)

| Feature | Free Tier | Notes |
|---------|-----------|-------|
| MAU (Monthly Active Users) | 50,000 | Đủ cho năm đầu |
| Phone Auth | 10K/month | Không dùng |
| Anonymous Auth | Unlimited | Có thể dùng |
| OAuth (Google/FB) | Unlimited | Main feature |

**Điểm mạnh:**
- Google login ready
- Secure token management
- SDK tốt cho cả web và mobile
- Free SSL certificates

**Điểm khó:**
- Vendor lock-in nhẹ
- Cần sync với database

**Implementation:**
```typescript
// Frontend: Firebase init
import { initializeApp } from 'firebase/app';
import { getAuth, signInWithPopup, GoogleAuthProvider } from 'firebase/auth';

const firebaseConfig = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY,
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN,
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID,
};

const app = initializeApp(firebaseConfig);
const auth = getAuth(app);

// Google Sign In
const provider = new GoogleAuthProvider();
const result = await signInWithPopup(auth, provider);
const idToken = await result.user.getIdToken();

// Send to backend
await fetch('/api/auth/firebase-login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ idToken })
});
```

### 2.3 Storage: Cloudflare R2 (FREE 10GB)

| Feature | Free Tier | Notes |
|---------|-----------|-------|
| Storage | 10GB | Đủ cho ~1000 lessons |
| Operations (Class A) | 1M/month | PUT, POST |
| Operations (Class B) | 10M/month | GET |
| Egress | **FREE** | Major savings |

**Điểm mạnh:**
- KHÔNG tính phí egress (khác S3)
- S3-compatible API
- Global CDN built-in
- Cheap khi scale

**Điểm khó:**
- Cần Cloudflare account
- UI admin hơi basic

**Use Cases:**
| Content | Estimated Size | Quantity |
|---------|---------------|----------|
| Lesson audio (MP3) | ~500KB | 100 lessons = 50MB |
| User recordings (WebM) | ~200KB | 1000/day = 200MB |
| Thumbnails | ~50KB | 100 = 5MB |
| Total MVP | | ~500MB |

### 2.4 AI/Speech: Azure Cognitive Services (FREE 5hr/month)

| Service | Free Tier | Rate |
|---------|-----------|------|
| Speech-to-Text | 5 hours/month | $1/hr after |
| Pronunciation Assessment | 5 hours/month | $1/hr after |

**Điểm mạnh:**
- Best Vietnamese language support
- Real-time và batch processing
- Pronunciation scoring built-in
- SDK tốt cho .NET

**Điểm khó:**
- 5 hours = ~300 exercises/month (1 min each)
- Cần rate limiting chặt

**Optimization Strategy:**
1. Cache transcription results
2. Limit shadowing attempts (3/lesson/day)
3. Batch processing off-peak hours
4. Fallback to OpenAI Whisper when exceeded

**Alternative: OpenAI Whisper**
- $0.006/minute
- Cheaper at scale
- Less accurate for Vietnamese accents

### 2.5 Caching: Upstash Redis (FREE)

| Feature | Free Tier | Notes |
|---------|-----------|-------|
| Commands | 10,000/day | OK for MVP |
| Storage | 256MB | Đủ |
| Regions | 1 | Global available |

**Điểm mạnh:**
- Serverless Redis
- REST API (no connection issues)
- Pay-per-use pricing
- Good .NET SDK

**Điểm khó:**
- 10K commands/day tight
- Need to optimize cache usage

**Cache Strategy:**
```csharp
// High-value caches only
"leaderboard:weekly"     // TTL: 5 min, ~100 users
"lesson:{id}"            // TTL: 1 hour
"user:progress:{id}"     // TTL: 10 min
"user:streak:{id}"       // TTL: 1 hour
```

### 2.6 Payment: SEPay (1% fee)

| Feature | Details |
|---------|---------|
| Fee | 1% per transaction |
| Banks | All major VN banks (VCB, TCB, MB, ACB...) |
| Integration | Webhook + API |
| Settlement | T+1 |

**Điểm mạnh:**
- Native VN bank transfer
- QR code support
- Instant webhook
- No monthly fee

**Điểm khó:**
- Manual reconciliation
- No recurring billing (must implement)
- VND only

**Flow:**
```
1. User selects plan
2. Backend creates Payment with unique reference: "DEMIF-{planId}-{random}"
3. Display bank account + QR
4. User transfers with reference in description
5. SEPay webhook triggers
6. Backend verifies and activates subscription
```

### 2.7 Database: SQL Server Express (FREE)

| Feature | Limit | Notes |
|---------|-------|-------|
| Database size | 10GB | Đủ cho 100K users |
| RAM | 1GB | OK for small-medium |
| CPU | 1 socket/4 cores | Đủ |

**Điểm mạnh:**
- Full SQL Server features
- Entity Framework Core support
- Free for commercial use
- Easy migration to paid

**Điểm khó:**
- 10GB limit
- No built-in replication
- Windows-focused (but works on Linux)

**Alternative: PostgreSQL**
- 100% free, no limits
- Good EF Core support
- Better for Linux hosting

### 2.8 Error Tracking: Sentry (FREE)

| Feature | Free Tier |
|---------|-----------|
| Events | 5,000/month |
| Retention | 30 days |
| Team members | 1 |

**Điểm mạnh:**
- Automatic error capture
- Performance monitoring
- Good .NET integration
- Source maps support

---

## 3. Hosting Options (Backend)

### Option A: Railway (Recommended for MVP)

| Feature | Free Tier |
|---------|-----------|
| Credit | $5/month |
| RAM | 512MB |
| CPU | Shared |
| Egress | 100GB |

**Pros:** Easy deploy, good DX
**Cons:** Limited resources

### Option B: Render

| Feature | Free Tier |
|---------|-----------|
| Web Service | 750 hours/month |
| RAM | 512MB |
| Sleep | After 15 min inactive |

**Pros:** Generous free tier
**Cons:** Cold starts

### Option C: Azure App Service (Free Tier)

| Feature | Free Tier |
|---------|-----------|
| Apps | 10 |
| RAM | 1GB |
| Storage | 1GB |
| CPU | Shared |

**Pros:** Native .NET, great integration
**Cons:** Limited, sleeps after inactivity

### Option D: Self-hosted VPS

| Provider | Price | Specs |
|----------|-------|-------|
| DigitalOcean | $4/month | 512MB RAM |
| Vultr | $2.50/month | 512MB RAM |
| Hetzner | €3.79/month | 2GB RAM |

**Pros:** Full control, no cold starts
**Cons:** More maintenance

---

## 4. Cost Projection

### 4.1 MVP Phase (0-1000 users)

| Service | Monthly Cost |
|---------|-------------|
| Vercel (Frontend) | $0 |
| Firebase Auth | $0 |
| Cloudflare R2 | $0 |
| Azure Speech (5hr) | $0 |
| Upstash Redis | $0 |
| Railway/Render | $5 |
| SEPay | 1% of revenue |
| **Total** | **~$5/month** |

### 4.2 Growth Phase (1000-10000 users)

| Service | Monthly Cost |
|---------|-------------|
| Vercel Pro | $20 |
| Firebase | $0 (under 50K) |
| Cloudflare R2 | $5 |
| Azure Speech | $50 (50hr) |
| Upstash Pro | $10 |
| Railway Pro | $20 |
| **Total** | **~$105/month** |

---

## 5. Migration Path

### When to Upgrade

| Trigger | Action |
|---------|--------|
| > 50K MAU | Firebase → Custom auth |
| > 10GB storage | R2 paid tier |
| > 500 concurrent | VPS or Azure App Service |
| > 50hr AI/month | Batch processing, Whisper fallback |

---

## 6. Summary Recommendations

| Category | Recommended | Alternative |
|----------|-------------|-------------|
| Frontend hosting | Vercel | Netlify |
| Auth | Firebase | Auth0, Supabase |
| Storage | Cloudflare R2 | Firebase Storage |
| AI/Speech | Azure Speech | OpenAI Whisper |
| Cache | Upstash | Redis Cloud |
| Database | SQL Server Express | PostgreSQL |
| Backend hosting | Railway | Render, Azure |
| Payment | SEPay | VNPay, MoMo |
| Error tracking | Sentry | LogRocket |
