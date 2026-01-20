# DEMIF - Step-by-Step Development Roadmap

## 📋 Overview

Phát triển theo thứ tự: **Dictation → Shadowing → YouTube Integration**

---

## Phase 1: Dictation Feature (Week 1-2)

### Step 1.1: Backend - Lesson Management
```
[ ] Tạo ASP.NET Core project với Clean Architecture
[ ] Setup Entity Framework + SQL Server connection
[ ] Tạo Lesson entity và repository
[ ] API: GET /lessons - List lessons by level/category
[ ] API: GET /lessons/{id} - Lesson detail với DictationTemplate
```

### Step 1.2: Backend - Dictation Template Generation
```
[ ] Service: DictationTemplateGenerator
    - Input: FullTranscript + Level
    - Output: JSON template với blanks
[ ] Algorithm: SelectBlanks by level
    - Beginner: 15% blanks (easy content words)
    - Intermediate: 35% blanks
    - Advanced: 60% blanks
    - Expert: 100% (full transcription)
```

### Step 1.3: Backend - Scoring
```
[ ] API: POST /exercises/dictation/submit
[ ] Service: DictationScorer
    - Compare user answers với correct answers
    - Levenshtein distance for fuzzy matching
    - Calculate score với penalties
[ ] Save to UserExercises table
[ ] Update UserProgress
```

### Step 1.4: Frontend Integration
```
[ ] Connect existing DictationExercise component với real API
[ ] Replace mock lessons data với API calls
[ ] Implement submit flow
[ ] Display real scoring results
```

---

## Phase 2: Shadowing Feature (Week 3-4)

### Option A: Browser Web Speech API (Recommended for MVP)

```
┌──────────────────────────────────────────────────────────────┐
│ FRONTEND (Browser)                                           │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ 1. User clicks "Record"                                │  │
│ │ 2. Web Speech API transcribes in real-time             │  │
│ │ 3. Get transcript text                                 │  │
│ │ 4. Send to backend for comparison                      │  │
│ └────────────────────────────────────────────────────────┘  │
│                          │                                   │
│                          ▼                                   │
│ ┌────────────────────────────────────────────────────────┐  │
│ │ BACKEND (.NET)                                         │  │
│ │ - Receive: originalText + userTranscript               │  │
│ │ - Compare texts (word by word)                         │  │
│ │ - Calculate accuracy score                             │  │
│ │ - Return: {score, differences, feedback}               │  │
│ └────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### Step 2.1: Frontend - Voice Recording
```javascript
// No Python needed! Browser-native API
const recognition = new webkitSpeechRecognition();
recognition.lang = 'en-US';
recognition.continuous = false;
recognition.interimResults = true;

recognition.onresult = (event) => {
  const transcript = event.results[0][0].transcript;
  // Send to backend for comparison
  submitForComparison(originalText, transcript);
};
```

### Step 2.2: Backend - Text Comparison Service
```csharp
public class TextComparisonService
{
    public ComparisonResult Compare(string original, string userSaid)
    {
        var originalWords = Tokenize(original);
        var userWords = Tokenize(userSaid);
        
        // Use diff algorithm
        var differences = FindDifferences(originalWords, userWords);
        var accuracy = CalculateAccuracy(originalWords, userWords);
        
        return new ComparisonResult
        {
            Accuracy = accuracy,
            Differences = differences,
            Feedback = GenerateFeedback(differences)
        };
    }
}
```

### Step 2.3: Shadowing Backend APIs
```
[ ] API: POST /exercises/shadowing/submit
    - Input: { lessonId, originalText, userTranscript }
    - Output: { score, differences, feedback }
[ ] Service: TextComparisonService
[ ] Save results to UserExercises
```

### Option B: Whisper (Add Later for Premium)
```
[ ] Setup Python FastAPI service
[ ] Docker container for Whisper
[ ] API: POST /whisper/transcribe
[ ] Integrate với .NET backend
```

---

## Phase 3: YouTube Integration (Week 5-6)

### 3.1 Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│ USER FLOW                                                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  1. User pastes YouTube URL                                     │
│     ▼                                                           │
│  2. Backend fetches transcript (youtube-transcript-api)         │
│     ▼                                                           │
│  3. Frontend displays:                                          │
│     ┌─────────────────┬─────────────────────────────────┐      │
│     │  YouTube Video  │  Transcript Panel               │      │
│     │  (iframe embed) │  - Current sentence highlighted │      │
│     │                 │  - Click to jump to time        │      │
│     │                 │  - "Practice this" button       │      │
│     └─────────────────┴─────────────────────────────────┘      │
│     ▼                                                           │
│  4. Practice Mode:                                              │
│     - Pause video at sentence end                               │
│     - User speaks the sentence                                  │
│     - Web Speech API transcribes                                │
│     - Compare & show score                                      │
│     - Continue to next sentence                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 YouTube Service (Python - Required)

```python
# Lý do cần Python: youtube-transcript-api chỉ có cho Python
# Đây là microservice nhỏ, chỉ fetch transcript

from youtube_transcript_api import YouTubeTranscriptApi
from fastapi import FastAPI

app = FastAPI()

@app.get("/transcript/{video_id}")
def get_transcript(video_id: str):
    transcript = YouTubeTranscriptApi.get_transcript(video_id)
    return {
        "segments": [
            {
                "text": entry["text"],
                "start": entry["start"],
                "end": entry["start"] + entry["duration"]
            }
            for entry in transcript
        ]
    }
```

### 3.3 .NET Integration

```csharp
public class YouTubeService
{
    private readonly HttpClient _http;
    
    public async Task<YouTubeLesson> CreateLessonFromUrl(string youtubeUrl)
    {
        var videoId = ExtractVideoId(youtubeUrl);
        
        // Fetch transcript from Python service
        var transcript = await _http.GetFromJsonAsync<TranscriptResponse>(
            $"http://youtube-service:8001/transcript/{videoId}"
        );
        
        // Create lesson
        var lesson = new Lesson
        {
            Title = await GetVideoTitle(videoId),
            LessonType = "shadowing",
            AudioUrl = null, // Use YouTube embed instead
            YouTubeVideoId = videoId,
            FullTranscript = string.Join(" ", transcript.Segments.Select(s => s.Text))
        };
        
        // Create shadowing exercises from segments
        var exercises = transcript.Segments.Select((s, i) => new ShadowingSegment
        {
            Index = i,
            Text = s.Text,
            StartTime = s.Start,
            EndTime = s.End
        });
        
        return new YouTubeLesson { Lesson = lesson, Segments = exercises };
    }
}
```

### 3.4 Frontend YouTube Player

```typescript
// Key: YouTube IFrame API để control playback
import YouTube from 'react-youtube';

function YouTubeShadowing({ videoId, segments }) {
  const playerRef = useRef(null);
  const [currentSegment, setCurrentSegment] = useState(null);
  
  // Sync transcript highlight với video time
  useEffect(() => {
    const interval = setInterval(() => {
      const time = playerRef.current?.getCurrentTime();
      const segment = segments.find(s => 
        time >= s.startTime && time <= s.endTime
      );
      setCurrentSegment(segment);
    }, 100);
    return () => clearInterval(interval);
  }, [segments]);
  
  // Practice mode: pause and let user speak
  const practiceSegment = (segment) => {
    playerRef.current.seekTo(segment.startTime);
    playerRef.current.playVideo();
    
    // Pause at end of segment
    setTimeout(() => {
      playerRef.current.pauseVideo();
      startRecording(segment);
    }, (segment.endTime - segment.startTime) * 1000);
  };
  
  return (
    <div className="grid grid-cols-2">
      <YouTube videoId={videoId} onReady={e => playerRef.current = e.target} />
      
      <div className="transcript-panel">
        {segments.map(seg => (
          <div 
            key={seg.index}
            className={currentSegment?.index === seg.index ? 'bg-yellow-200' : ''}
            onClick={() => playerRef.current.seekTo(seg.startTime)}
          >
            <span>{seg.text}</span>
            <button onClick={() => practiceSegment(seg)}>🎤 Practice</button>
          </div>
        ))}
      </div>
    </div>
  );
}
```

---

## 📊 Summary: What Needs Python?

| Feature | Python Required? | Reason |
|---------|-----------------|--------|
| Dictation | ❌ No | Pure .NET |
| Shadowing (Basic) | ❌ No | Web Speech API (browser) |
| Shadowing (Premium) | ✅ Yes | Whisper for better accuracy |
| YouTube Transcript | ✅ Yes | youtube-transcript-api |

### Minimal Python Services

```yaml
# docker-compose.yml
services:
  # ONLY YouTube transcript fetching
  youtube-service:
    image: python:3.10-slim
    command: uvicorn main:app --host 0.0.0.0 --port 8001
    volumes:
      - ./youtube-service:/app
    ports:
      - "8001:8001"
    # Very lightweight, no GPU needed
    # Memory: ~100MB
    
  # OPTIONAL: Whisper for premium users
  whisper-service:
    image: whisper-service:latest
    ports:
      - "8000:8000"
    # Only run if you want premium STT
```

---

## 🗓️ Implementation Timeline

| Week | Focus | Deliverable |
|------|-------|-------------|
| 1 | Dictation Backend | APIs + Scoring |
| 2 | Dictation Frontend | Connect UI |
| 3 | Shadowing Backend | Comparison Service |
| 4 | Shadowing Frontend | Voice Recording |
| 5 | YouTube Service | Transcript Fetch |
| 6 | YouTube UI | Player + Practice Mode |

---

## 🚀 Next Steps

1. **Start với Dictation** (không cần Python)
2. **Add Shadowing** với Web Speech API
3. **Add YouTube** khi cần (cần Python nhỏ)
4. **Optional**: Whisper cho premium users
