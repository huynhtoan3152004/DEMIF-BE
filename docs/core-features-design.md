                                                                        # DEMIF - Core Features Technical Design

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                       FRONTEND (Next.js)                        │
├──────────────────┬──────────────────┬───────────────────────────┤
│  Dictation UI    │  Shadowing UI    │  YouTube Player UI        │
│  - Audio Player  │  - Video Player  │  - YouTube Embed          │
│  - Fill Blanks   │  - Voice Record  │  - Sync Transcript        │
│  - Submit Check  │  - Real-time STT │  - Speak & Compare        │
└────────┬─────────┴────────┬─────────┴─────────────┬─────────────┘
         │                  │                       │
         ▼                  ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                    BACKEND (ASP.NET Core)                       │
├─────────────────────────────────────────────────────────────────┤
│  DictationService  │  ShadowingService  │  YouTubeService       │
│  - Generate Blanks │  - Whisper STT     │  - Fetch Transcript   │
│  - Score Answers   │  - Compare Text    │  - Sync Timestamps    │
│  - Adaptive Level  │  - Pronunciation   │  - Cache Results      │
└────────────────────┴───────────┬────────┴───────────────────────┘
                                 │
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                   WHISPER SERVICE (Self-hosted)                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │  faster-whisper (Python)                                │   │
│  │  - Speech-to-Text                                       │   │
│  │  - Word-level timestamps                                │   │
│  │  - Language detection                                   │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

---

## 2. Dictation Feature

### 2.1 Concept
- User nghe MP3, điền từ vào chỗ trống
- Level khác nhau = số lượng blank khác nhau

### 2.2 Blank Generation Algorithm

```csharp
public class DictationGenerator
{
    public DictationTemplate GenerateTemplate(string fullTranscript, string level)
    {
        var words = Tokenize(fullTranscript);
        var blankRatio = GetBlankRatio(level);
        var blanks = SelectBlanksIntelligently(words, blankRatio);
        
        return BuildTemplate(words, blanks);
    }
    
    private double GetBlankRatio(string level) => level switch
    {
        "beginner" => 0.15,      // 15% words hidden (easy words)
        "intermediate" => 0.35,  // 35% words hidden
        "advanced" => 0.60,      // 60% words hidden
        "expert" => 1.0,         // 100% - full transcription
        _ => 0.25
    };
    
    private List<int> SelectBlanksIntelligently(List<Word> words, double ratio)
    {
        var blanks = new List<int>();
        var totalBlanks = (int)(words.Count * ratio);
        
        // Priority: Content words > Function words
        // Content: nouns, verbs, adjectives, adverbs
        // Function: the, a, is, are, to, etc.
        
        var contentWords = words
            .Select((w, i) => (w, i))
            .Where(x => IsContentWord(x.w))
            .OrderBy(_ => Random.Next())
            .Take(totalBlanks)
            .Select(x => x.i)
            .ToList();
            
        return contentWords;
    }
    
    private bool IsContentWord(Word word)
    {
        var functionWords = new HashSet<string> 
        { 
            "the", "a", "an", "is", "are", "was", "were", 
            "to", "of", "in", "on", "at", "for", "with",
            "and", "or", "but", "so", "yet"
        };
        return !functionWords.Contains(word.Text.ToLower());
    }
}
```

### 2.3 Template Structure

```json
{
  "lessonId": "lesson-001",
  "level": "intermediate",
  "fullTranscript": "Hello, my name is Sarah. Nice to meet you.",
  "segments": [
    {"type": "text", "value": "Hello, my "},
    {"type": "blank", "index": 0, "answer": "name", "hint": "n___", "audioStart": 0.5, "audioEnd": 0.8},
    {"type": "text", "value": " is "},
    {"type": "blank", "index": 1, "answer": "Sarah", "hint": "S____", "audioStart": 1.0, "audioEnd": 1.3},
    {"type": "text", "value": ". "},
    {"type": "blank", "index": 2, "answer": "Nice", "hint": "____", "audioStart": 1.5, "audioEnd": 1.7},
    {"type": "text", "value": " to meet you."}
  ],
  "totalBlanks": 3,
  "audioUrl": "/audio/lesson-001.mp3",
  "audioDuration": 3.5
}
```

### 2.4 Scoring Algorithm

```csharp
public class DictationScorer
{
    public DictationResult Score(DictationSubmission submission, DictationTemplate template)
    {
        var results = new List<BlankResult>();
        int correct = 0;
        
        for (int i = 0; i < template.Blanks.Count; i++)
        {
            var expected = template.Blanks[i].Answer;
            var userAnswer = submission.Answers.ElementAtOrDefault(i) ?? "";
            
            var similarity = CalculateSimilarity(userAnswer, expected);
            var isCorrect = similarity >= 0.85; // 85% similarity threshold
            
            if (isCorrect) correct++;
            
            results.Add(new BlankResult
            {
                Index = i,
                Expected = expected,
                UserAnswer = userAnswer,
                IsCorrect = isCorrect,
                Similarity = similarity
            });
        }
        
        // Calculate score
        int baseScore = (int)((double)correct / template.Blanks.Count * 100);
        int playPenalty = Math.Max(0, (submission.PlaysUsed - 1) * 5);
        int timePenalty = CalculateTimePenalty(submission.TimeSpent, template.ExpectedTime);
        
        return new DictationResult
        {
            Score = Math.Max(0, baseScore - playPenalty - timePenalty),
            CorrectCount = correct,
            TotalBlanks = template.Blanks.Count,
            Details = results
        };
    }
    
    private double CalculateSimilarity(string a, string b)
    {
        // Levenshtein distance based similarity
        a = a.Trim().ToLower();
        b = b.Trim().ToLower();
        
        if (a == b) return 1.0;
        
        int distance = LevenshteinDistance(a, b);
        int maxLen = Math.Max(a.Length, b.Length);
        
        return 1.0 - (double)distance / maxLen;
    }
}
```

---

## 3. Shadowing Feature (Self-hosted Whisper)

### 3.1 Concept
- User nghe native speaker
- User ghi âm giọng mình
- System so sánh với transcript gốc
- Đưa ra feedback

### 3.2 Whisper Service (Python FastAPI)

```python
# whisper_service.py
from fastapi import FastAPI, UploadFile, File
from faster_whisper import WhisperModel
import tempfile
import os

app = FastAPI()

# Load model once at startup
model = WhisperModel("base.en", device="cpu", compute_type="int8")
# Options: tiny.en, base.en, small.en, medium.en, large-v3

@app.post("/transcribe")
async def transcribe(audio: UploadFile = File(...)):
    # Save uploaded file
    with tempfile.NamedTemporaryFile(delete=False, suffix=".webm") as tmp:
        content = await audio.read()
        tmp.write(content)
        tmp_path = tmp.name
    
    try:
        # Transcribe with word timestamps
        segments, info = model.transcribe(
            tmp_path,
            word_timestamps=True,
            language="en"
        )
        
        words = []
        full_text = ""
        
        for segment in segments:
            full_text += segment.text
            for word in segment.words:
                words.append({
                    "word": word.word.strip(),
                    "start": word.start,
                    "end": word.end,
                    "confidence": word.probability
                })
        
        return {
            "success": True,
            "text": full_text.strip(),
            "words": words,
            "language": info.language,
            "duration": info.duration
        }
    finally:
        os.unlink(tmp_path)

@app.post("/compare")
async def compare_transcripts(
    original: str,
    user_audio: UploadFile = File(...)
):
    # First transcribe user audio
    transcription = await transcribe(user_audio)
    user_text = transcription["text"]
    
    # Compare texts
    result = compare_texts(original, user_text)
    
    return {
        "original": original,
        "userSaid": user_text,
        "wordAccuracy": result["accuracy"],
        "differences": result["differences"],
        "feedback": generate_feedback(result)
    }

def compare_texts(original: str, user_said: str):
    original_words = original.lower().split()
    user_words = user_said.lower().split()
    
    # Use diff algorithm
    from difflib import SequenceMatcher
    
    matcher = SequenceMatcher(None, original_words, user_words)
    differences = []
    correct = 0
    
    for tag, i1, i2, j1, j2 in matcher.get_opcodes():
        if tag == 'equal':
            correct += (i2 - i1)
        elif tag == 'replace':
            for k in range(i1, i2):
                user_word = user_words[j1 + k - i1] if j1 + k - i1 < len(user_words) else ""
                differences.append({
                    "type": "wrong",
                    "expected": original_words[k],
                    "got": user_word,
                    "position": k
                })
        elif tag == 'delete':
            for k in range(i1, i2):
                differences.append({
                    "type": "missed",
                    "expected": original_words[k],
                    "position": k
                })
        elif tag == 'insert':
            for k in range(j1, j2):
                differences.append({
                    "type": "extra",
                    "got": user_words[k],
                    "position": i1
                })
    
    accuracy = correct / len(original_words) * 100 if original_words else 0
    
    return {
        "accuracy": round(accuracy, 1),
        "correct": correct,
        "total": len(original_words),
        "differences": differences
    }
```

### 3.3 ASP.NET Integration

```csharp
public class WhisperService : IWhisperService
{
    private readonly HttpClient _httpClient;
    private readonly string _whisperUrl;
    
    public WhisperService(IConfiguration config, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _whisperUrl = config["Whisper:ServiceUrl"]; // http://localhost:8000
    }
    
    public async Task<TranscriptionResult> TranscribeAsync(Stream audioStream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(audioStream), "audio", fileName);
        
        var response = await _httpClient.PostAsync($"{_whisperUrl}/transcribe", content);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<TranscriptionResult>();
    }
    
    public async Task<ComparisonResult> CompareAsync(string originalText, Stream userAudio)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(originalText), "original");
        content.Add(new StreamContent(userAudio), "user_audio", "recording.webm");
        
        var response = await _httpClient.PostAsync($"{_whisperUrl}/compare", content);
        return await response.Content.ReadFromJsonAsync<ComparisonResult>();
    }
}
```

### 3.4 Shadowing Result

```json
{
  "lessonId": "lesson-001",
  "exerciseType": "shadowing",
  "original": "Hello, my name is Sarah",
  "userSaid": "Hello, my name is Sara",
  "scores": {
    "wordAccuracy": 80,
    "overall": 80
  },
  "differences": [
    {
      "type": "wrong",
      "expected": "Sarah",
      "got": "Sara",
      "position": 4,
      "suggestion": "Chú ý phát âm 'Sarah' với âm 'h' cuối"
    }
  ],
  "feedback": {
    "positive": ["Phát âm tốt: Hello, my, name, is"],
    "improve": ["Cần cải thiện: Sarah → Sara (thiếu âm 'h')"]
  }
}
```

---

## 4. YouTube Integration

### 4.1 Concept
- User paste YouTube URL
- System fetch transcript với timestamps
- Sync transcript với video playback
- User nói theo từng câu
- System check và scoring

### 4.2 YouTube Transcript Fetcher

```python
# youtube_service.py
from youtube_transcript_api import YouTubeTranscriptApi
from fastapi import FastAPI
import re

app = FastAPI()

@app.get("/transcript/{video_id}")
async def get_transcript(video_id: str, language: str = "en"):
    try:
        # Fetch transcript
        transcript_list = YouTubeTranscriptApi.list_transcripts(video_id)
        
        # Try to get English transcript
        try:
            transcript = transcript_list.find_transcript([language])
        except:
            transcript = transcript_list.find_generated_transcript([language])
        
        entries = transcript.fetch()
        
        # Process into segments
        segments = []
        for entry in entries:
            segments.append({
                "text": entry["text"],
                "start": entry["start"],
                "duration": entry["duration"],
                "end": entry["start"] + entry["duration"]
            })
        
        return {
            "success": True,
            "videoId": video_id,
            "language": language,
            "segments": segments,
            "totalDuration": segments[-1]["end"] if segments else 0
        }
    except Exception as e:
        return {
            "success": False,
            "error": str(e)
        }

@app.post("/youtube/create-lesson")
async def create_youtube_lesson(video_url: str, level: str = "intermediate"):
    # Extract video ID
    video_id = extract_video_id(video_url)
    
    # Fetch transcript
    transcript_data = await get_transcript(video_id)
    
    if not transcript_data["success"]:
        return {"error": "Could not fetch transcript"}
    
    # Generate shadowing exercises from segments
    exercises = []
    for i, segment in enumerate(transcript_data["segments"]):
        exercises.append({
            "index": i,
            "text": segment["text"],
            "startTime": segment["start"],
            "endTime": segment["end"],
            "duration": segment["duration"]
        })
    
    return {
        "videoId": video_id,
        "embedUrl": f"https://www.youtube.com/embed/{video_id}",
        "exercises": exercises,
        "totalExercises": len(exercises)
    }

def extract_video_id(url: str) -> str:
    patterns = [
        r'(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/embed\/)([^&\n?#]+)',
    ]
    for pattern in patterns:
        match = re.search(pattern, url)
        if match:
            return match.group(1)
    return url
```

### 4.3 Frontend YouTube Player Component

```typescript
// components/youtube-shadowing.tsx
"use client"

import { useState, useRef, useEffect } from 'react'
import YouTube from 'react-youtube'

interface Segment {
  index: number
  text: string
  startTime: number
  endTime: number
}

export function YouTubeShadowing({ videoId, segments }: Props) {
  const [currentSegment, setCurrentSegment] = useState<Segment | null>(null)
  const [isRecording, setIsRecording] = useState(false)
  const [results, setResults] = useState<Result[]>([])
  const playerRef = useRef<any>(null)
  const mediaRecorderRef = useRef<MediaRecorder | null>(null)
  
  // Sync current segment with video time
  const onStateChange = (event: any) => {
    if (event.data === 1) { // Playing
      startTimeTracking()
    }
  }
  
  const startTimeTracking = () => {
    const interval = setInterval(() => {
      if (playerRef.current) {
        const currentTime = playerRef.current.getCurrentTime()
        const segment = segments.find(
          s => currentTime >= s.startTime && currentTime <= s.endTime
        )
        if (segment) {
          setCurrentSegment(segment)
        }
      }
    }, 100)
    
    return () => clearInterval(interval)
  }
  
  // Pause at segment end for user to repeat
  const handleSegmentEnd = async (segment: Segment) => {
    playerRef.current?.pauseVideo()
    
    // Prompt user to repeat
    setCurrentSegment(segment)
  }
  
  // Record user's voice
  const startRecording = async () => {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
    const recorder = new MediaRecorder(stream, { mimeType: 'audio/webm' })
    const chunks: Blob[] = []
    
    recorder.ondataavailable = (e) => chunks.push(e.data)
    recorder.onstop = async () => {
      const blob = new Blob(chunks, { type: 'audio/webm' })
      await submitRecording(blob, currentSegment!)
    }
    
    mediaRecorderRef.current = recorder
    recorder.start()
    setIsRecording(true)
  }
  
  const stopRecording = () => {
    mediaRecorderRef.current?.stop()
    setIsRecording(false)
  }
  
  // Submit to backend for comparison
  const submitRecording = async (audioBlob: Blob, segment: Segment) => {
    const formData = new FormData()
    formData.append('audio', audioBlob, 'recording.webm')
    formData.append('originalText', segment.text)
    formData.append('segmentIndex', segment.index.toString())
    
    const response = await fetch('/api/shadowing/compare', {
      method: 'POST',
      body: formData
    })
    
    const result = await response.json()
    setResults(prev => [...prev, { segment, result }])
    
    // Auto continue to next segment
    playerRef.current?.playVideo()
  }
  
  return (
    <div className="grid grid-cols-2 gap-6">
      {/* Video Player */}
      <div>
        <YouTube
          videoId={videoId}
          onReady={(e) => playerRef.current = e.target}
          onStateChange={onStateChange}
          opts={{
            playerVars: { controls: 1 }
          }}
        />
      </div>
      
      {/* Transcript & Recording */}
      <div className="space-y-4">
        {/* Current Segment */}
        <div className="p-4 bg-orange-50 rounded-xl">
          <h3 className="font-bold mb-2">Câu hiện tại:</h3>
          <p className="text-lg">{currentSegment?.text}</p>
        </div>
        
        {/* Recording Button */}
        <button
          onClick={isRecording ? stopRecording : startRecording}
          className={`w-full py-3 rounded-xl ${
            isRecording ? 'bg-red-500' : 'bg-orange-500'
          } text-white`}
        >
          {isRecording ? 'Dừng ghi âm' : 'Bắt đầu nói'}
        </button>
        
        {/* Results */}
        <div className="space-y-2">
          {results.map((r, i) => (
            <div key={i} className="p-3 bg-white border rounded-lg">
              <p className="text-sm text-gray-600">{r.segment.text}</p>
              <p className="font-bold text-green-600">
                Accuracy: {r.result.wordAccuracy}%
              </p>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}
```

### 4.4 Data Flow

```
┌─────────────────────────────────────────────────────────────────┐
│  1. User pastes YouTube URL                                     │
└───────────────────────────────┬─────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  2. Backend fetches transcript via youtube-transcript-api       │
│     - Get segments with timestamps                              │
│     - Cache for future use                                      │
└───────────────────────────────┬─────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  3. Frontend displays video + synced transcript                 │
│     - Highlight current segment                                 │
│     - Pause at segment end                                      │
└───────────────────────────────┬─────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  4. User records their voice saying the segment                 │
│     - MediaRecorder API (browser)                               │
│     - WebM/Opus format                                          │
└───────────────────────────────┬─────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  5. Backend processes via Whisper                               │
│     - Transcribe user audio                                     │
│     - Compare with original                                     │
│     - Calculate accuracy                                        │
└───────────────────────────────┬─────────────────────────────────┘
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│  6. Show results + feedback                                     │
│     - Word-by-word comparison                                   │
│     - Highlight errors                                          │
│     - Suggestions for improvement                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## 5. Self-hosted Setup

### 5.1 Docker Compose

```yaml
# docker-compose.yml
version: '3.8'

services:
  whisper:
    build: ./whisper-service
    ports:
      - "8000:8000"
    volumes:
      - whisper-cache:/root/.cache/huggingface
    environment:
      - WHISPER_MODEL=base.en
    deploy:
      resources:
        limits:
          memory: 2G

  youtube-service:
    build: ./youtube-service
    ports:
      - "8001:8001"
    
  api:
    build: ./backend
    ports:
      - "5000:5000"
    depends_on:
      - whisper
      - youtube-service
    environment:
      - Whisper__ServiceUrl=http://whisper:8000
      - YouTube__ServiceUrl=http://youtube-service:8001

volumes:
  whisper-cache:
```

### 5.2 Whisper Dockerfile

```dockerfile
# whisper-service/Dockerfile
FROM python:3.10-slim

WORKDIR /app

RUN apt-get update && apt-get install -y ffmpeg && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
```

### 5.3 Requirements

```txt
# whisper-service/requirements.txt
fastapi==0.104.1
uvicorn==0.24.0
python-multipart==0.0.6
faster-whisper==0.9.0
```

---

## 6. VPS Requirements

| Spec | Minimum | Recommended |
|------|---------|-------------|
| CPU | 2 cores | 4 cores |
| RAM | 2GB | 4GB |
| Storage | 10GB | 20GB |
| Model | tiny.en | base.en |

### Model Size vs Accuracy

| Model | Size | Speed | Accuracy |
|-------|------|-------|----------|
| tiny.en | 75MB | 10x | Good |
| base.en | 150MB | 7x | Better |
| small.en | 500MB | 4x | Great |
| medium.en | 1.5GB | 2x | Excellent |

**Recommendation:** Start with `base.en` - best balance for VPS

---

## 7. Pronunciation Improvement (Future)

### Using Phoneme Analysis

```python
# Future: Add pronunciation scoring
from phonemizer import phonemize

def analyze_pronunciation(expected: str, user_said: str):
    expected_phonemes = phonemize(expected, language='en-us')
    user_phonemes = phonemize(user_said, language='en-us')
    
    # Compare phonemes
    similarity = phoneme_similarity(expected_phonemes, user_phonemes)
    
    return {
        "expectedPhonemes": expected_phonemes,
        "userPhonemes": user_phonemes,
        "similarity": similarity,
        "suggestions": generate_pronunciation_tips(expected, user_said)
    }
```
