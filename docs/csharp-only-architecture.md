# DEMIF - C# Only Architecture (No Python Required)

## 🎯 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    FRONTEND (Next.js / Mobile)                  │
│  ┌─────────────────────────────────────────────────────────────┐│
│  │ Web Speech API / Mobile Speech Recognition                  ││
│  │ - User nói → Browser/App transcribe → Text                  ││
│  │ - Gửi text xuống Backend để compare                         ││
│  └─────────────────────────────────────────────────────────────┘│
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    BACKEND (ASP.NET Core - 100% C#)             │
│  ┌──────────────────┬──────────────────┬───────────────────┐   │
│  │ DictationService │ ShadowingService │ YouTubeService    │   │
│  │ - Generate blanks│ - Compare texts  │ - Fetch caption   │   │
│  │ - Score answers  │ - Calculate score│ - Parse VTT       │   │
│  └──────────────────┴──────────────────┴───────────────────┘   │
│                                │                                 │
│                                ▼                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              N8N / RAG System (Optional)                 │  │
│  │  - Learning roadmap generation                           │  │
│  │  - Personalized recommendations                          │  │
│  │  - AI feedback (connect to OpenAI/Claude)                │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 1. Speech-to-Text Flow (NO Python)

### Web Browser
```javascript
// Frontend: Web Speech API (built into Chrome, Edge, Safari)
const recognition = new webkitSpeechRecognition();
recognition.lang = 'en-US';

recognition.onresult = async (event) => {
  const userSaidText = event.results[0][0].transcript;
  
  // Send to C# backend for comparison
  const response = await fetch('/api/shadowing/compare', {
    method: 'POST',
    body: JSON.stringify({
      originalText: "Hello, my name is Sarah",
      userText: userSaidText
    })
  });
  
  const result = await response.json();
  // { score: 85, differences: [...], feedback: [...] }
};
```

### Mobile App (React Native / Flutter / .NET MAUI)

| Platform | Speech API | Notes |
|----------|------------|-------|
| **React Native** | @react-native-voice/voice | Works offline on iOS/Android |
| **Flutter** | speech_to_text package | Same as RN |
| **.NET MAUI** | Plugin.SpeechRecognition | Native C# |
| **PWA Mobile** | Web Speech API | Cần Chrome/Safari |

**Answer về Mobile**: Web Speech API hoạt động TỐT trên mobile browsers (Chrome Android, Safari iOS).
React Native/Flutter có native packages riêng cũng hoạt động tốt.

---

## 2. YouTube Caption - C# Solution (No Python)

### Option A: YouTube Data API v3 (Official)

```csharp
public class YouTubeCaptionService
{
    private readonly string _apiKey;
    
    public async Task<List<CaptionSegment>> GetCaptionsAsync(string videoId)
    {
        // 1. Get caption track ID
        var client = new YouTubeService(new BaseClientService.Initializer
        {
            ApiKey = _apiKey
        });
        
        var captionListRequest = client.Captions.List("snippet", videoId);
        var captionList = await captionListRequest.ExecuteAsync();
        
        // 2. Download caption file (VTT/SRT format)
        var captionId = captionList.Items.FirstOrDefault()?.Id;
        var downloadRequest = client.Captions.Download(captionId);
        
        using var stream = new MemoryStream();
        await downloadRequest.DownloadAsync(stream);
        
        // 3. Parse VTT/SRT to segments
        return ParseCaptions(stream);
    }
    
    private List<CaptionSegment> ParseCaptions(Stream vttStream)
    {
        // Parse WebVTT format
        // 00:00:01.000 --> 00:00:03.500
        // Hello, my name is Sarah
        
        var segments = new List<CaptionSegment>();
        // ... parsing logic
        return segments;
    }
}
```

### Option B: Scrape từ timedtext API (Không cần API key)

```csharp
public async Task<List<CaptionSegment>> GetCaptionsDirectAsync(string videoId)
{
    // YouTube timedtext endpoint (public)
    var url = $"https://www.youtube.com/api/timedtext?v={videoId}&lang=en&fmt=vtt";
    
    var response = await _httpClient.GetStringAsync(url);
    return ParseVTT(response);
}
```

### Option C: N8N Workflow (No Code)

```yaml
# N8N Workflow: Fetch YouTube Captions
Trigger: Webhook (POST /n8n/youtube-caption)
  ↓
HTTP Request: GET youtube transcript API
  ↓
Parse JSON/VTT
  ↓
Respond to Webhook: Return segments
```

---

## 3. RAG System & N8N Integration

### N8N Use Cases for DEMIF

| Use Case | N8N Workflow | Benefit |
|----------|--------------|---------|
| **Learning Roadmap** | Analyze user progress → Call LLM → Generate roadmap | No coding needed |
| **AI Feedback** | User answer → OpenAI → Personalized feedback | Easy to modify |
| **YouTube Processing** | Video URL → Fetch captions → Store in DB | Visual workflow |
| **Daily Recommendations** | User stats → LLM → Suggest lessons | Scheduled jobs |

### RAG for Learning Roadmap

```
┌──────────────────────────────────────────────────────────────┐
│                    N8N + RAG Workflow                        │
│                                                              │
│  1. Trigger: User completes placement test                   │
│         ↓                                                    │
│  2. Fetch: User progress, skill gaps                         │
│         ↓                                                    │
│  3. Vector Search: Find similar learner paths                │
│         ↓                                                    │
│  4. LLM (OpenAI/Claude): Generate personalized roadmap       │
│         ↓                                                    │
│  5. Store: Save roadmap to database                          │
│         ↓                                                    │
│  6. Return: Roadmap to user                                  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### N8N Setup

```yaml
# docker-compose.yml - Add N8N
services:
  n8n:
    image: docker.n8n.io/n8nio/n8n
    ports:
      - "5678:5678"
    environment:
      - N8N_BASIC_AUTH_ACTIVE=true
      - N8N_BASIC_AUTH_USER=admin
      - N8N_BASIC_AUTH_PASSWORD=your-password
    volumes:
      - n8n_data:/home/node/.n8n
```

### Call N8N from C#

```csharp
public class N8NService
{
    private readonly HttpClient _http;
    
    public async Task<LearningRoadmap> GenerateRoadmapAsync(UserProgress progress)
    {
        var response = await _http.PostAsJsonAsync(
            "http://n8n:5678/webhook/generate-roadmap",
            new { userId = progress.UserId, skills = progress.Skills }
        );
        
        return await response.Content.ReadFromJsonAsync<LearningRoadmap>();
    }
    
    public async Task<string> GetAIFeedbackAsync(string original, string userSaid)
    {
        var response = await _http.PostAsJsonAsync(
            "http://n8n:5678/webhook/ai-feedback",
            new { original, userSaid }
        );
        
        return await response.Content.ReadAsStringAsync();
    }
}
```

---

## 4. Mobile App Compatibility

### Web Speech API on Mobile

| Platform | Browser | Support |
|----------|---------|---------|
| Android | Chrome | ✅ Full support |
| Android | Firefox | ❌ Not supported |
| iOS | Safari | ✅ Full support |
| iOS | Chrome | ⚠️ Limited (uses Safari engine) |

### Recommendation for Mobile

| Approach | Pros | Cons |
|----------|------|------|
| **PWA first** | Same codebase, works now | Need Chrome/Safari |
| **React Native** | Native speech, offline | Separate codebase |
| **.NET MAUI** | C# everywhere | Learning curve |

**Best Strategy:**
1. **Phase 1**: PWA với Web Speech API (fits with Next.js)
2. **Phase 2**: React Native app nếu cần offline/native features

---

## 5. Complete Tech Stack (C# Only)

| Component | Technology | Python? |
|-----------|------------|---------|
| Frontend Web | Next.js | ❌ |
| Frontend Mobile | PWA / React Native | ❌ |
| Backend API | ASP.NET Core 8 | ❌ |
| Database | SQL Server | ❌ |
| Speech-to-Text | Web Speech API | ❌ |
| YouTube Captions | YouTube Data API v3 | ❌ |
| AI/RAG | N8N + OpenAI | ❌ |
| Workflow Automation | N8N | ❌ |
| Learning Roadmap | N8N + LLM | ❌ |

**Result: 100% No Python Required!**
