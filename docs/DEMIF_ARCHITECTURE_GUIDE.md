# 🎧 Demif — Hướng dẫn Kiến trúc & Vận hành Toàn diện

> **Stack:** C# ASP.NET · PostgreSQL · Redis · VPS Hostinger · yt-dlp · OpenAI Whisper API · whisper.cpp · FuzzySharp · Hangfire · Cloudinary

---

## Mục lục

1. [Kiến trúc tổng thể](#1-kiến-trúc-tổng-thể)
2. [Setup VPS từ đầu](#2-setup-vps-từ-đầu)
3. [Tự host Whisper trên VPS (Shadowing)](#3-tự-host-whisper-trên-vps)
4. [Pipeline YouTube → Bài học](#4-pipeline-youtube--bài-học)
5. [Hệ thống chấm điểm Dictation](#5-hệ-thống-chấm-điểm-dictation)
6. [Hệ thống SRS (Ôn tập từ vựng)](#6-hệ-thống-srs)
7. [Gamification (Streak, Stats, Badges)](#7-gamification)
8. [Toàn bộ API Endpoints](#8-toàn-bộ-api-endpoints)
9. [Database Schema](#9-database-schema)
10. [Vận hành tối ưu](#10-vận-hành-tối-ưu)
11. [Nguồn tìm content (Resource)](#11-nguồn-tìm-content)
12. [Chi phí & Lộ trình Scale](#12-chi-phí--lộ-trình-scale)

---

## 1. Kiến trúc tổng thể

```
┌──────────────────────────── VPS Hostinger ─────────────────────────────┐
│                                                                          │
│   [Nginx :80/:443]  ← SSL termination, rate limiting, reverse proxy     │
│          │                                                               │
│          ▼                                                               │
│   [ASP.NET API :5000]          [Hangfire Worker]                         │
│   - Controllers                - LessonPipelineJob                       │
│   - Services                   - TranscriptionJob                        │
│   - FuzzySharp Evaluator       - SRS Scheduler                           │
│          │                              │                                │
│          └──────────┬───────────────────┘                                │
│                     ▼                                                    │
│              [PostgreSQL :5432]     [Redis :6379]                        │
│              - Lessons              - Hangfire queue                     │
│              - Segments             - Response cache                     │
│              - SrsItems             - Session store                      │
│              - UserStats                                                 │
│                                                                          │
│   [whisper.cpp]  ← tự host, dùng cho Shadowing (không tốn tiền)         │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
          │                      │                      │
          ▼                      ▼                      ▼
   [Cloudinary]          [OpenAI Whisper API]     [yt-dlp binary]
   lưu audio dài hạn     transcript + timestamps   tải audio YouTube
```

### Nguyên tắc thiết kế

- **Không lưu audio trên VPS** — dùng Cloudinary cho audio dài hạn, `/tmp` chỉ cho xử lý tạm
- **Không block HTTP thread** — mọi tác vụ nặng (tải audio, transcribe) đều chạy qua Hangfire background job
- **Không tự chạy model AI nặng** — dùng OpenAI Whisper API cho transcription, chỉ tự host whisper.cpp nhỏ cho shadowing
- **Fallback thông minh** — thử lấy subtitle YouTube trước (miễn phí, 5 giây), chỉ dùng Whisper API khi không có subtitle

---

## 2. Setup VPS từ đầu

### 2.1 Cài đặt cơ bản

```bash
# SSH vào VPS
ssh root@your-vps-ip

# Cập nhật hệ thống
sudo apt update && sudo apt upgrade -y

# Cài các dependency cơ bản
sudo apt install -y build-essential git curl wget nano \
    ffmpeg redis-server nginx certbot python3-certbot-nginx

# Cài .NET 8
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# Verify
dotnet --version   # → 8.x.x
redis-cli ping     # → PONG
ffmpeg -version    # → ffmpeg version...
```

### 2.2 Cài yt-dlp

```bash
sudo curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp \
    -o /usr/local/bin/yt-dlp
sudo chmod a+rx /usr/local/bin/yt-dlp

# Cập nhật yt-dlp (nên chạy hàng tuần — YouTube thay đổi liên tục)
yt-dlp -U

# Test
yt-dlp --version
```

### 2.3 Cấu hình Redis

```bash
sudo nano /etc/redis/redis.conf
```

Tìm và chỉnh các dòng sau:

```
bind 127.0.0.1          # Chỉ lắng nghe nội bộ — bảo mật
maxmemory 512mb          # Giới hạn RAM cho Redis
maxmemory-policy allkeys-lru  # Tự xóa key cũ khi đầy
```

```bash
sudo systemctl enable redis-server
sudo systemctl restart redis-server
```

### 2.4 Cấu hình Nginx

```bash
sudo nano /etc/nginx/sites-available/demif
```

```nginx
# Rate limiting zones
limit_req_zone $binary_remote_addr zone=api:10m rate=30r/m;
limit_req_zone $binary_remote_addr zone=upload:10m rate=5r/m;
limit_req_zone $binary_remote_addr zone=shadowing:10m rate=20r/m;

server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name yourdomain.com;

    ssl_certificate     /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;

    # Upload YouTube (tốn thời gian nhất)
    location /api/admin/lessons/from-youtube {
        limit_req zone=upload burst=3 nodelay;
        proxy_pass         http://localhost:5000;
        proxy_read_timeout 30s;   # Chỉ khởi tạo job, trả về ngay
    }

    # Shadowing upload audio
    location /api/lessons/ {
        limit_req zone=shadowing burst=10;
        client_max_body_size 10M;  # File audio shadowing tối đa 10MB
        proxy_pass         http://localhost:5000;
        proxy_read_timeout 60s;
    }

    # API thông thường
    location /api/ {
        limit_req zone=api burst=20 nodelay;
        proxy_pass         http://localhost:5000;
        proxy_read_timeout 30s;
        proxy_set_header   X-Real-IP $remote_addr;
        proxy_set_header   Host $host;
    }

    # Hangfire dashboard (bảo vệ bằng password)
    location /hangfire {
        auth_basic "Admin Area";
        auth_basic_user_file /etc/nginx/.htpasswd;
        proxy_pass http://localhost:5000;
    }
}
```

```bash
# Tạo password cho Hangfire dashboard
sudo apt install apache2-utils -y
sudo htpasswd -c /etc/nginx/.htpasswd admin

sudo ln -s /etc/nginx/sites-available/demif /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx

# Cài SSL miễn phí
sudo certbot --nginx -d yourdomain.com
```

### 2.5 Chạy ASP.NET như Systemd Service

```bash
sudo nano /etc/systemd/system/demif-api.service
```

```ini
[Unit]
Description=Demif API Server
After=network.target postgresql.service redis.service
Wants=postgresql.service redis.service

[Service]
Type=notify
WorkingDirectory=/var/www/demif
ExecStart=/usr/bin/dotnet /var/www/demif/DemifApi.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=demif-api
User=www-data
Group=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Giới hạn resource để tránh OOM
MemoryLimit=1G
CPUQuota=80%

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable demif-api
sudo systemctl start demif-api

# Xem log real-time
sudo journalctl -u demif-api -f
```

### 2.6 Deploy script (dùng mỗi khi update code)

```bash
#!/bin/bash
# deploy.sh — chạy từ máy local
set -e

echo "🔨 Building..."
dotnet publish -c Release -o ./publish

echo "📦 Copying to VPS..."
rsync -avz ./publish/ root@your-vps-ip:/var/www/demif/

echo "🔄 Restarting service..."
ssh root@your-vps-ip "sudo systemctl restart demif-api"

echo "✅ Deployed!"
```

---

## 3. Tự host Whisper trên VPS

> Dùng cho tính năng **Shadowing** — hoàn toàn miễn phí, không gọi API bên ngoài.

### 3.1 Build whisper.cpp

```bash
# Cài dependency
sudo apt install -y cmake build-essential libopenblas-dev

# Clone và build
cd /opt
sudo git clone https://github.com/ggerganov/whisper.cpp.git
cd whisper.cpp
sudo make -j$(nproc)   # dùng hết CPU core để build nhanh hơn

# Tải model (chọn 1 trong các options dưới)
# → tiny.en  (75MB,  ~1s/câu) — RAM ít, VPS yếu
# → small.en (244MB, ~3s/câu) — Khuyến nghị cho KVM2
# → medium.en (769MB, ~8s/câu) — Cần KVM4+
sudo bash ./models/download-ggml-model.sh small.en

# Test
./main -m models/ggml-small.en.bin \
       -f samples/jfk.wav \
       --no-timestamps \
       -l en
# Output: "And so my fellow Americans ask not what your country can do for you..."
```

### 3.2 Chọn model phù hợp VPS

| Model | Size | RAM cần | Tốc độ/câu 10s | Phù hợp |
|-------|------|---------|----------------|---------|
| `tiny.en` | 75MB | ~300MB | ~1 giây | KVM1 (4GB RAM) |
| `small.en` | 244MB | ~500MB | ~3 giây | KVM2 (8GB RAM) ✅ |
| `medium.en` | 769MB | ~1.5GB | ~8 giây | KVM4 (16GB RAM) |
| `large-v3` | 1.5GB | ~3GB | ~20 giây | Dedicated server |

### 3.3 Gọi whisper.cpp từ C#

```csharp
// Services/ShadowingService.cs
public class ShadowingService
{
    private readonly string _whisperBin = "/opt/whisper.cpp/main";
    private readonly string _modelPath  = "/opt/whisper.cpp/models/ggml-small.en.bin";
    private readonly IEvaluatorService  _evaluator;

    public async Task<ShadowingResult> AnalyzeAsync(
        IFormFile audioFile, string segmentId)
    {
        var segment  = await _db.DictationSegments.FindAsync(segmentId);
        var tempPath = $"/tmp/shadow_{Guid.NewGuid():N}.wav";

        // Đảm bảo audio là WAV 16kHz (whisper.cpp yêu cầu)
        var rawPath = $"/tmp/raw_{Guid.NewGuid():N}";
        await SaveFormFileAsync(audioFile, rawPath);
        await ConvertToWavAsync(rawPath, tempPath);

        try
        {
            var userText = await RunWhisperAsync(tempPath);
            var score    = Fuzz.TokenSortRatio(
                Normalize(userText),
                Normalize(segment.TargetText));

            return new ShadowingResult
            {
                Score       = score,
                UserSpoke   = userText.Trim(),
                Target      = segment.TargetText,
                Feedback    = GetFeedback(score),
                Passed      = score >= 70
            };
        }
        finally
        {
            File.Delete(tempPath);
            File.Delete(rawPath);
        }
    }

    private async Task<string> RunWhisperAsync(string wavPath)
    {
        var tcs = new TaskCompletionSource<string>();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName               = _whisperBin,
                Arguments              = $"-m {_modelPath} -f \"{wavPath}\" " +
                                         $"--no-timestamps -l en --output-txt",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            },
            EnableRaisingEvents = true
        };

        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) output.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();

        var completed = await Task.Run(() => process.WaitForExit(30_000));
        if (!completed)
        {
            process.Kill();
            throw new TimeoutException("Whisper timed out after 30s");
        }

        return output.ToString().Trim();
    }

    // Chuyển audio sang WAV 16kHz mono (ffmpeg)
    private async Task ConvertToWavAsync(string inputPath, string outputPath)
    {
        await RunProcessAsync("ffmpeg",
            $"-i \"{inputPath}\" -ar 16000 -ac 1 -c:a pcm_s16le \"{outputPath}\" -y",
            timeoutMs: 30_000);
    }

    private string GetFeedback(int score) => score switch
    {
        >= 95 => "🌟 Xuất sắc! Phát âm rất chuẩn.",
        >= 80 => "✅ Tốt! Một vài từ cần luyện thêm.",
        >= 65 => "📖 Khá. Nghe lại và chú ý ngữ điệu.",
        >= 50 => "💪 Cần luyện tập thêm. Tập từng từ một.",
        _     => "🎧 Hãy nghe thật kỹ rồi thử lại nhé."
    };
}
```

---

## 4. Pipeline YouTube → Bài học

### 4.1 Flow tổng quan

```
POST /api/admin/lessons/from-youtube  (URL)
         │
         ▼ trả về ngay: { lessonId, jobId }
         │
    [Hangfire Job chạy ngầm]
         │
         ├─① GetMetadata (yt-dlp, ~2 giây)
         │
         ├─② TryGetSubtitle (yt-dlp --write-subs, ~5 giây)
         │       ├── Có subtitle → Parse VTT → segments  [Con đường A]
         │       └── Không có   → tiếp tục Con đường B
         │
         ├─③ DownloadAudio (yt-dlp -x, 30s~3 phút)        [Con đường B]
         │
         ├─④ Whisper API (word timestamps, ~1 phút)
         │
         ├─⑤ BuildSegments (NLP split, ~1 giây)
         │
         └─⑥ Lưu DB, xóa file /tmp
```

### 4.2 NuGet packages cần thiết

```xml
<!-- DemifApi.csproj -->
<PackageReference Include="FuzzySharp"           Version="2.0.2" />
<PackageReference Include="Hangfire.AspNetCore"  Version="1.8.9" />
<PackageReference Include="Hangfire.PostgreSql"  Version="1.20.9" />
<PackageReference Include="CloudinaryDotNet"     Version="1.26.2" />
<PackageReference Include="Serilog.AspNetCore"   Version="8.0.1" />
<PackageReference Include="StackExchange.Redis"  Version="2.7.27" />
```

### 4.3 Lấy metadata YouTube

```csharp
// Services/YoutubeService.cs
public async Task<YoutubeMetadata> GetMetadataAsync(string url)
{
    var json = await RunProcessOutputAsync("yt-dlp",
        $"--print-json --skip-download --no-playlist \"{url}\"",
        timeoutMs: 15_000);

    var doc = JsonDocument.Parse(json).RootElement;
    return new YoutubeMetadata
    {
        VideoId      = doc.GetProperty("id").GetString(),
        Title        = doc.GetProperty("title").GetString(),
        Duration     = doc.GetProperty("duration").GetInt32(),
        Channel      = doc.GetProperty("channel").GetString(),
        ThumbnailUrl = doc.GetProperty("thumbnail").GetString(),
        UploadDate   = doc.GetProperty("upload_date").GetString()
    };
}
```

### 4.4 Thử lấy subtitle VTT trước (nhanh & miễn phí)

```csharp
public async Task<string> TryGetSubtitlePathAsync(string url, string videoId)
{
    var outTemplate = $"/tmp/demif_{videoId}";
    var vttPath     = $"{outTemplate}.en.vtt";

    try
    {
        await RunProcessAsync("yt-dlp",
            $"--write-subs --write-auto-subs " +
            $"--sub-lang en --sub-format vtt " +
            $"--skip-download -o \"{outTemplate}\" \"{url}\"",
            timeoutMs: 15_000);

        return File.Exists(vttPath) ? vttPath : null;
    }
    catch { return null; }
}
```

### 4.5 Parse file VTT thành segments

```csharp
// Services/VttParser.cs
public List<DictationSegmentDto> ParseVtt(string vttContent)
{
    var rawBlocks = vttContent
        .Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
        .Where(b => b.Contains("-->"));

    var raw = rawBlocks.Select(block =>
    {
        var lines    = block.Trim().Split('\n');
        var timeLine = lines.First(l => l.Contains("-->"));
        var parts    = timeLine.Split("-->");
        var text     = string.Join(" ",
            lines.Where(l => !l.Contains("-->"))
                 .Select(CleanVttTags)
                 .Where(l => !string.IsNullOrWhiteSpace(l)));

        return new
        {
            Text      = text,
            StartTime = ParseTime(parts[0]),
            EndTime   = ParseTime(parts[1])
        };
    }).Where(r => !string.IsNullOrWhiteSpace(r.Text)).ToList();

    // Merge thành câu hoàn chỉnh 6–14 từ
    return MergeIntoSentences(raw
        .Select(r => new SubtitleBlock(r.Text, r.StartTime, r.EndTime))
        .ToList());
}

private List<DictationSegmentDto> MergeIntoSentences(
    List<SubtitleBlock> blocks)
{
    var result = new List<DictationSegmentDto>();
    var buffer = new List<SubtitleBlock>();

    foreach (var block in blocks)
    {
        buffer.Add(block);
        var words     = buffer.Sum(b => b.Text.Split(' ').Length);
        var endsClean = Regex.IsMatch(buffer.Last().Text.TrimEnd(), @"[.!?]$");

        if ((words >= 6 && endsClean) || words >= 14)
        {
            result.Add(new DictationSegmentDto
            {
                Text      = string.Join(" ", buffer.Select(b => b.Text)),
                StartTime = buffer.First().StartTime,
                EndTime   = buffer.Last().EndTime + 0.3
            });
            buffer.Clear();
        }
    }
    if (buffer.Any())
        result.Add(new DictationSegmentDto
        {
            Text      = string.Join(" ", buffer.Select(b => b.Text)),
            StartTime = buffer.First().StartTime,
            EndTime   = buffer.Last().EndTime + 0.3
        });

    return result;
}

private string CleanVttTags(string line) =>
    Regex.Replace(line,
        @"<[^>]+>|align:\S+|position:\S+|line:\S+|size:\S+", "").Trim();

private double ParseTime(string s)
{
    s = s.Trim().Split(' ')[0];  // bỏ tag sau space
    var p = s.Split(':');
    return p.Length == 3
        ? double.Parse(p[0]) * 3600 + double.Parse(p[1]) * 60 + double.Parse(p[2])
        : double.Parse(p[0]) * 60 + double.Parse(p[1]);
}
```

### 4.6 Gọi Whisper API với word timestamps

```csharp
// Services/WhisperService.cs
public async Task<WhisperResult> TranscribeAsync(string audioPath)
{
    using var form = new MultipartFormDataContent();
    using var file = File.OpenRead(audioPath);

    form.Add(new StreamContent(file), "file", Path.GetFileName(audioPath));
    form.Add(new StringContent("whisper-1"),      "model");
    form.Add(new StringContent("verbose_json"),   "response_format");
    form.Add(new StringContent("en"),             "language");
    form.Add(new StringContent("[\"word\"]"),     "timestamp_granularities[]");

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _apiKey);

    var response = await _client.PostAsync(
        "https://api.openai.com/v1/audio/transcriptions", form);
    response.EnsureSuccessStatusCode();

    var json = await response.Content.ReadAsStringAsync();
    var doc  = JsonDocument.Parse(json).RootElement;

    var words = doc.GetProperty("words").EnumerateArray()
        .Select(w => new WordTimestamp(
            Word:  w.GetProperty("word").GetString().Trim(),
            Start: w.GetProperty("start").GetDouble(),
            End:   w.GetProperty("end").GetDouble()))
        .ToList();

    return new WhisperResult(
        FullText: doc.GetProperty("text").GetString(),
        Words:    words);
}
```

### 4.7 Build segments từ word timestamps

```csharp
// Services/SegmentBuilder.cs
public List<DictationSegmentDto> BuildFromWords(List<WordTimestamp> words)
{
    var segments = new List<DictationSegmentDto>();
    var buffer   = new List<WordTimestamp>();

    for (int i = 0; i < words.Count; i++)
    {
        var word = words[i];
        buffer.Add(word);

        bool endsWithPunct  = Regex.IsMatch(word.Word, @"[.!?]$");
        bool longPause      = i < words.Count - 1 &&
                              words[i + 1].Start - word.End > 1.2;
        bool tooLong        = buffer.Count >= 15;
        bool goodLength     = buffer.Count >= 5;

        if (tooLong || (goodLength && (endsWithPunct || longPause)))
        {
            segments.Add(new DictationSegmentDto
            {
                Text      = string.Join(" ", buffer.Select(w => w.Word)).Trim(),
                StartTime = buffer.First().Start,
                EndTime   = buffer.Last().End + 0.5,  // buffer 500ms
                WordCount = buffer.Count
            });
            buffer.Clear();
        }
    }

    if (buffer.Any())
        segments.Add(new DictationSegmentDto
        {
            Text      = string.Join(" ", buffer.Select(w => w.Word)).Trim(),
            StartTime = buffer.First().Start,
            EndTime   = buffer.Last().End + 0.5,
            WordCount = buffer.Count
        });

    return segments;
}
```

### 4.8 Hangfire Job hoàn chỉnh

```csharp
// Jobs/LessonPipelineJob.cs
public class LessonPipelineJob
{
    public async Task ExecuteAsync(Guid lessonId, string youtubeUrl)
    {
        var lesson    = await _db.Lessons.FindAsync(lessonId);
        string audio  = null;
        string vtt    = null;

        try
        {
            await SetProgress(lesson, 5,  "Fetching metadata...");
            var meta = await _youtube.GetMetadataAsync(youtubeUrl);
            lesson.Title        = meta.Title;
            lesson.ThumbnailUrl = meta.ThumbnailUrl;
            lesson.Duration     = meta.Duration;
            await _db.SaveChangesAsync();

            // Con đường A: thử subtitle trước
            await SetProgress(lesson, 15, "Checking subtitles...");
            vtt = await _youtube.TryGetSubtitlePathAsync(youtubeUrl, meta.VideoId);

            List<DictationSegmentDto> segments;

            if (vtt != null)
            {
                await SetProgress(lesson, 80, "Parsing subtitles...");
                var content  = await File.ReadAllTextAsync(vtt);
                segments     = _vttParser.ParseVtt(content);
                lesson.TranscriptSource = "youtube_subtitle";
            }
            else
            {
                // Con đường B: tải audio + Whisper
                await SetProgress(lesson, 25, "Downloading audio...");
                audio = await _youtube.DownloadAudioAsync(youtubeUrl);

                await SetProgress(lesson, 55, "Transcribing with Whisper AI...");
                var transcript = await _whisper.TranscribeAsync(audio);

                await SetProgress(lesson, 80, "Building segments...");
                segments             = _segmentBuilder.BuildFromWords(transcript.Words);
                lesson.FullTranscript = transcript.FullText;
                lesson.TranscriptSource = "whisper_api";
            }

            // Lưu segments vào DB
            await SetProgress(lesson, 90, "Saving...");
            var dbSegments = segments.Select((s, idx) => new DictationSegment
            {
                Id         = Guid.NewGuid(),
                LessonId   = lessonId,
                Order      = idx,
                TargetText = s.Text,
                StartTime  = s.StartTime,
                EndTime    = s.EndTime,
                WordCount  = s.WordCount
            });
            await _db.DictationSegments.AddRangeAsync(dbSegments);

            lesson.Status             = LessonStatus.Published;
            lesson.ProcessingProgress = 100;
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            lesson.Status       = LessonStatus.Failed;
            lesson.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync();
            throw;  // Hangfire auto-retry 3 lần
        }
        finally
        {
            if (audio != null && File.Exists(audio)) File.Delete(audio);
            if (vtt   != null && File.Exists(vtt))   File.Delete(vtt);
        }
    }

    private async Task SetProgress(Lesson l, int pct, string msg)
    {
        l.ProcessingProgress = pct;
        l.ProcessingMessage  = msg;
        await _db.SaveChangesAsync();
    }
}
```

---

## 5. Hệ thống chấm điểm Dictation

### 5.1 Cài FuzzySharp

```bash
dotnet add package FuzzySharp
```

### 5.2 Logic chấm điểm hoàn chỉnh

```csharp
// Services/DictationEvaluator.cs
public class DictationEvaluator
{
    public EvaluationResult Evaluate(string userInput, string targetText)
    {
        var normUser   = Normalize(userInput);
        var normTarget = Normalize(targetText);

        // Điểm tổng thể — Token Sort Ratio (chuẩn vàng cho câu nhiều từ)
        int overallScore = Fuzz.TokenSortRatio(normUser, normTarget);

        // Phân tích từng từ
        var targetWords = normTarget.Split(' ');
        var userWords   = normUser.Split(' ');
        var wordResults = AlignWords(userWords, targetWords);

        // Trích xuất từ sai để inject vào SRS
        var errorWords = wordResults
            .Where(w => w.Status != WordStatus.Correct)
            .Select(w => w.Target)
            .Where(w => !string.IsNullOrEmpty(w))
            .Distinct()
            .ToList();

        return new EvaluationResult
        {
            OverallScore      = overallScore,
            Words             = wordResults,
            ExtractedErrors   = errorWords,
            IsPassed          = overallScore >= 80
        };
    }

    private List<WordResult> AlignWords(string[] userWords, string[] targetWords)
    {
        var results = new List<WordResult>();

        // Dynamic programming để căn chỉnh từ
        for (int i = 0; i < targetWords.Length; i++)
        {
            var target  = targetWords[i];
            // Tìm từ của user gần nhất với target
            var match   = userWords.FirstOrDefault(u =>
                              Fuzz.Ratio(u, target) >= 70);

            if (match == null)
            {
                results.Add(new WordResult(target, null,
                    WordStatus.Missing, 0));
            }
            else
            {
                int distance = Levenshtein(match, target);
                var status   = distance == 0   ? WordStatus.Correct
                             : distance <= 2   ? WordStatus.Typo     // lỗi đánh máy nhỏ
                                               : WordStatus.Wrong;   // sai hoàn toàn
                results.Add(new WordResult(target, match, status, distance));
            }
        }

        // Thêm các từ thừa của user
        var usedWords = results.Where(r => r.UserWord != null)
                               .Select(r => r.UserWord).ToHashSet();
        foreach (var extra in userWords.Where(w => !usedWords.Contains(w)))
            results.Add(new WordResult(null, extra, WordStatus.Extra, 0));

        return results;
    }

    private string Normalize(string input) =>
        Regex.Replace(input.ToLower(), @"[^\w\s]", "")
             .Replace("  ", " ").Trim();

    private int Levenshtein(string a, string b)
    {
        int[,] dp = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
        for (int j = 1; j <= b.Length; j++)
            dp[i, j] = Math.Min(
                Math.Min(dp[i-1, j] + 1, dp[i, j-1] + 1),
                dp[i-1, j-1] + (a[i-1] == b[j-1] ? 0 : 1));
        return dp[a.Length, b.Length];
    }
}

// Response mẫu gửi về cho Frontend
// {
//   "segmentId": "uuid",
//   "overallScore": 92,
//   "isPassed": true,
//   "words": [
//     { "target": "the",   "user": "the",  "status": "correct", "distance": 0 },
//     { "target": "quick", "user": "quikc","status": "typo",    "distance": 2 },
//     { "target": "brown", "user": null,   "status": "missing", "distance": 0 }
//   ],
//   "extractedErrors": ["brown", "fox"]
// }
```

---

## 6. Hệ thống SRS

### 6.1 Database Schema

```sql
CREATE TABLE "SrsItems" (
    "Id"             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"         UUID NOT NULL REFERENCES "Users"("Id"),
    "VocabWord"      VARCHAR(200) NOT NULL,
    "SourceSegmentId" UUID REFERENCES "DictationSegments"("Id"),
    "EasinessFactor" FLOAT   NOT NULL DEFAULT 2.5,
    "Interval"       INT     NOT NULL DEFAULT 1,
    "Repetitions"    INT     NOT NULL DEFAULT 0,
    "NextReviewDate" TIMESTAMP NOT NULL DEFAULT NOW(),
    "CreatedAt"      TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE("UserId", "VocabWord")  -- tránh duplicate
);

-- Index quan trọng nhất — query mỗi lần user mở app
CREATE INDEX idx_srs_due ON "SrsItems"("UserId", "NextReviewDate");
```

### 6.2 Thuật toán SM-2

```csharp
// Services/SrsScheduler.cs
public SrsItem ProcessReview(SrsItem item, int quality)  // quality: 0-5
{
    if (quality >= 3)  // Nhớ được
    {
        item.Interval = item.Repetitions switch
        {
            0 => 1,
            1 => 6,
            _ => (int)Math.Round(item.Interval * item.EasinessFactor)
        };
        item.Repetitions++;
    }
    else  // Quên
    {
        item.Repetitions = 0;
        item.Interval    = 1;
    }

    // Cập nhật EasinessFactor (không bao giờ < 1.3)
    item.EasinessFactor = Math.Max(1.3,
        item.EasinessFactor + 0.1 - (5 - quality) * 0.08);

    item.NextReviewDate = DateTime.UtcNow.AddDays(item.Interval);
    return item;
}

// Inject từ sai vào SRS sau khi chấm bài
public async Task InjectErrorWordsAsync(Guid userId, List<string> words)
{
    foreach (var word in words.Distinct())
    {
        // Dùng UPSERT — tránh duplicate
        var existing = await _db.SrsItems
            .FirstOrDefaultAsync(s => s.UserId == userId &&
                                      s.VocabWord == word);
        if (existing == null)
        {
            await _db.SrsItems.AddAsync(new SrsItem
            {
                UserId         = userId,
                VocabWord      = word,
                EasinessFactor = 2.5,
                Interval       = 1,
                Repetitions    = 0,
                NextReviewDate = DateTime.UtcNow.AddHours(4)
                // Ôn lại sau 4 giờ — không phải ngày mai
            });
        }
        else
        {
            // Đặt lại nếu sai lại
            existing.Repetitions    = 0;
            existing.Interval       = 1;
            existing.NextReviewDate = DateTime.UtcNow.AddHours(4);
        }
    }
    await _db.SaveChangesAsync();
}
```

---

## 7. Gamification

### 7.1 Database

```sql
ALTER TABLE "UserProfiles" ADD COLUMN "CurrentStreak"  INT DEFAULT 0;
ALTER TABLE "UserProfiles" ADD COLUMN "LongestStreak"  INT DEFAULT 0;
ALTER TABLE "UserProfiles" ADD COLUMN "LastStudyDate"  DATE;
ALTER TABLE "UserProfiles" ADD COLUMN "TotalXp"        INT DEFAULT 0;
ALTER TABLE "UserProfiles" ADD COLUMN "TotalCompleted" INT DEFAULT 0;

CREATE TABLE "Achievements" (
    "UserId"    UUID NOT NULL,
    "BadgeType" VARCHAR(50) NOT NULL,   -- 'streak_7', 'perfect_score', ...
    "EarnedAt"  TIMESTAMP DEFAULT NOW(),
    PRIMARY KEY ("UserId", "BadgeType")
);
```

### 7.2 Streak Service

```csharp
public async Task<StreakResult> UpdateStreakAsync(Guid userId)
{
    var profile = await _db.UserProfiles.FindAsync(userId);
    var today   = DateTime.UtcNow.Date;
    var last    = profile.LastStudyDate;

    if (last == today) return StreakResult.AlreadyUpdated(profile.CurrentStreak);

    profile.CurrentStreak = (last == today.AddDays(-1))
        ? profile.CurrentStreak + 1
        : 1;

    profile.LongestStreak = Math.Max(profile.LongestStreak, profile.CurrentStreak);
    profile.LastStudyDate  = today;
    profile.TotalXp       += 10;  // +10 XP mỗi ngày học

    await _db.SaveChangesAsync();
    await CheckAndAwardBadgesAsync(userId, profile);

    return StreakResult.Updated(profile.CurrentStreak);
}

private async Task CheckAndAwardBadgesAsync(Guid userId, UserProfile profile)
{
    var milestones = new Dictionary<int, string>
    {
        { 3,   "streak_3"   },
        { 7,   "streak_7"   },
        { 30,  "streak_30"  },
        { 100, "streak_100" }
    };

    foreach (var (days, badge) in milestones)
    {
        if (profile.CurrentStreak == days)
            await AwardBadgeAsync(userId, badge);
    }
}
```

---

## 8. Toàn bộ API Endpoints

### Admin Endpoints

| Method | Route | Mô tả |
|--------|-------|-------|
| `POST` | `/api/admin/lessons/from-youtube` | Submit YouTube URL, nhận `lessonId` ngay |
| `GET`  | `/api/admin/lessons/{id}/status` | Poll tiến độ pipeline (0–100%) |
| `PUT`  | `/api/admin/lessons/{id}` | Chỉnh sửa title, level, tags |
| `DELETE` | `/api/admin/lessons/{id}` | Xóa bài học |
| `GET`  | `/api/admin/lessons` | Danh sách tất cả bài học + trạng thái |

### User — Dictation

| Method | Route | Mô tả |
|--------|-------|-------|
| `GET`  | `/api/lessons` | Danh sách bài học (phân trang, filter) |
| `GET`  | `/api/lessons/{id}` | Chi tiết bài: metadata, số segments |
| `GET`  | `/api/lessons/{id}/segments` | Lấy segments (KHÔNG có `targetText`) |
| `GET`  | `/api/lessons/{id}/segments/{sid}/hint?level=1` | Gợi ý theo mức độ |
| `POST` | `/api/lessons/{id}/evaluate` | Nộp bài, nhận điểm chi tiết + lỗi |

### User — Shadowing

| Method | Route | Mô tả |
|--------|-------|-------|
| `POST` | `/api/lessons/{id}/shadowing/analyze` | Upload audio, nhận điểm phát âm |
| `GET`  | `/api/profile/shadowing-stats` | Lịch sử điểm shadowing |

### User — SRS

| Method | Route | Mô tả |
|--------|-------|-------|
| `GET`  | `/api/srs/due` | Từ cần ôn hôm nay (tối đa 50) |
| `POST` | `/api/srs/review` | Nộp kết quả ôn tập (quality 0–5) |
| `GET`  | `/api/srs/stats` | Thống kê từ vựng đã học |

### User — Gamification

| Method | Route | Mô tả |
|--------|-------|-------|
| `GET`  | `/api/profile/stats` | Streak, XP, badges, tổng bài làm |
| `GET`  | `/api/profile/history` | Lịch sử 30 ngày gần nhất |
| `GET`  | `/api/leaderboard` | Bảng xếp hạng tuần (top 20) |

### Response mẫu — `/api/lessons/{id}/evaluate`

```json
{
  "lessonId":      "uuid",
  "segmentId":     "uuid",
  "overallScore":  87.5,
  "isPassed":      true,
  "words": [
    { "target": "the",   "user": "the",   "status": "correct", "distance": 0 },
    { "target": "quick", "user": "quikc", "status": "typo",    "distance": 2 },
    { "target": "brown", "user": null,    "status": "missing", "distance": 0 },
    { "target": null,    "user": "thee",  "status": "extra",   "distance": 0 }
  ],
  "extractedErrors": ["brown", "fox"],
  "srsInjected":   true,
  "streakUpdated": true,
  "xpEarned":      15
}
```

---

## 9. Database Schema

```sql
-- ── Lessons ──────────────────────────────────────────────────────
CREATE TABLE "Lessons" (
    "Id"               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "YoutubeUrl"       TEXT NOT NULL,
    "YoutubeId"        VARCHAR(20) UNIQUE,
    "Title"            TEXT,
    "ThumbnailUrl"     TEXT,
    "Duration"         INT,
    "FullTranscript"   TEXT,
    "TranscriptSource" VARCHAR(30),   -- 'youtube_subtitle' | 'whisper_api'
    "Difficulty"       INT DEFAULT 1, -- 1=Easy, 2=Medium, 3=Hard
    "Tags"             TEXT[],        -- PostgreSQL array
    "Status"           VARCHAR(20) DEFAULT 'processing',
    "ProcessingProgress" INT DEFAULT 0,
    "ProcessingMessage"  TEXT,
    "ErrorMessage"     TEXT,
    "CreatedAt"        TIMESTAMP DEFAULT NOW()
);

-- ── Dictation Segments ───────────────────────────────────────────
CREATE TABLE "DictationSegments" (
    "Id"         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "LessonId"   UUID NOT NULL REFERENCES "Lessons"("Id") ON DELETE CASCADE,
    "Order"      INT NOT NULL,
    "TargetText" TEXT NOT NULL,
    "StartTime"  DOUBLE PRECISION NOT NULL,
    "EndTime"    DOUBLE PRECISION NOT NULL,
    "WordCount"  INT
);

CREATE INDEX idx_segments_lesson ON "DictationSegments"("LessonId", "Order");

-- ── Dictation Attempts (lịch sử làm bài) ─────────────────────────
CREATE TABLE "DictationAttempts" (
    "Id"         UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"     UUID NOT NULL,
    "SegmentId"  UUID NOT NULL REFERENCES "DictationSegments"("Id"),
    "UserInput"  TEXT,
    "Score"      FLOAT,
    "IsPassed"   BOOLEAN,
    "CreatedAt"  TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_attempts_user ON "DictationAttempts"("UserId", "CreatedAt" DESC);

-- ── SRS Items ────────────────────────────────────────────────────
CREATE TABLE "SrsItems" (
    "Id"              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId"          UUID NOT NULL,
    "VocabWord"       VARCHAR(200) NOT NULL,
    "SourceSegmentId" UUID,
    "EasinessFactor"  FLOAT   DEFAULT 2.5,
    "Interval"        INT     DEFAULT 1,
    "Repetitions"     INT     DEFAULT 0,
    "NextReviewDate"  TIMESTAMP DEFAULT NOW(),
    "CreatedAt"       TIMESTAMP DEFAULT NOW(),
    UNIQUE("UserId", "VocabWord")
);

CREATE INDEX idx_srs_due ON "SrsItems"("UserId", "NextReviewDate");

-- ── User Profiles ────────────────────────────────────────────────
CREATE TABLE "UserProfiles" (
    "UserId"         UUID PRIMARY KEY,
    "CurrentStreak"  INT DEFAULT 0,
    "LongestStreak"  INT DEFAULT 0,
    "LastStudyDate"  DATE,
    "TotalXp"        INT DEFAULT 0,
    "TotalCompleted" INT DEFAULT 0
);
```

---

## 10. Vận hành tối ưu

### 10.1 Cập nhật yt-dlp định kỳ (Quan trọng!)

YouTube thường xuyên thay đổi cấu trúc → yt-dlp cũ sẽ bị lỗi.

```bash
# Thêm vào crontab — tự cập nhật mỗi tuần vào Chủ nhật 3 giờ sáng
sudo crontab -e
0 3 * * 0 /usr/local/bin/yt-dlp -U >> /var/log/yt-dlp-update.log 2>&1
```

### 10.2 Dọn dẹp file /tmp

```bash
# Xóa file audio tạm cũ hơn 1 giờ (tránh đầy ổ cứng)
0 * * * * find /tmp/demif_* -mmin +60 -delete 2>/dev/null
```

### 10.3 Monitoring đơn giản

```bash
# Xem API đang chạy không
sudo systemctl status demif-api

# Xem log lỗi
sudo journalctl -u demif-api -n 100 --no-pager

# Xem RAM/CPU
htop

# Xem ổ cứng còn bao nhiêu
df -h

# Xem Hangfire queue
# → Truy cập yourdomain.com/hangfire (cần đăng nhập)
```

### 10.4 Giới hạn chi phí Whisper API

```csharp
// Chỉ gọi Whisper API nếu video dưới 20 phút
// Video dài hơn → yêu cầu admin review thủ công
if (metadata.Duration > 1200)
    throw new Exception("Video quá dài (>20 phút). Vui lòng chọn video ngắn hơn.");
```

### 10.5 Cache kết quả phổ biến

```csharp
// Cache danh sách bài học 5 phút (query nhiều nhất)
[ResponseCache(Duration = 300)]
[HttpGet("/api/lessons")]
public async Task<IActionResult> GetLessons() { ... }
```

### 10.6 Giới hạn SRS mỗi ngày

```csharp
// GET /api/srs/due — tối đa 30 từ/ngày để tránh user nản
var dueItems = await _db.SrsItems
    .Where(s => s.UserId == userId &&
                s.NextReviewDate <= DateTime.UtcNow)
    .OrderBy(s => s.NextReviewDate)
    .Take(30)
    .ToListAsync();
```

---

## 11. Nguồn tìm Content

### YouTube Channels chất lượng cao cho Dictation

| Kênh | Level | Đặc điểm |
|------|-------|----------|
| **VOA Learning English** | Beginner–Intermediate | Phát âm rõ, có subtitle chính xác |
| **TED-Ed** | Intermediate | Đa dạng chủ đề, subtitle chuẩn |
| **EnglishClass101** | All levels | Subtitle sẵn, phân chia rõ ràng |
| **BBC Learning English** | Intermediate | Giọng British chuẩn |
| **CNN 10** | Intermediate–Advanced | News thực tế, ngắn 10 phút |
| **TED Talks** | Advanced | Speakers đa dạng giọng điệu |
| **NowThis News** | Intermediate | Tin tức nhanh, câu ngắn |
| **Speak English With Vanessa** | Beginner | Giải thích chi tiết, rõ ràng |

### Tiêu chí chọn video tốt

```
✅ Video 3–10 phút (tối ưu)
✅ Có subtitle/CC chính xác
✅ Phát âm rõ, không nói quá nhanh
✅ Ít nhạc nền
✅ Chủ đề thực tế, gần gũi
✅ 1 người nói (không phải hội thoại nhiều người)

❌ Tránh: Video có quá nhiều thuật ngữ chuyên ngành
❌ Tránh: Nhạc nền to
❌ Tránh: Giọng nói không rõ ràng
```

### Playlist gợi ý để bắt đầu

```
Beginner (Level 1-2):
  → VOA Learning English: "Words and Their Stories"
  → EnglishClass101: "Absolute Beginner" series

Intermediate (Level 3-4):
  → CNN 10 (daily 10-minute news)
  → TED-Ed: "Can you solve this riddle?" series
  → BBC News 60 Seconds

Advanced (Level 5):
  → TED Talks (18 phút, speakers đa giọng)
  → NPR Short Wave podcast clips
  → The Economist: "The World in Brief"
```

### Công cụ tìm video phù hợp

```bash
# Dùng yt-dlp để xem trước thông tin video trước khi tạo bài
yt-dlp --print-json --skip-download "URL" | python3 -c "
import json,sys
d = json.load(sys.stdin)
print(f'Title:    {d[\"title\"]}')
print(f'Duration: {d[\"duration\"]}s ({d[\"duration\"]//60}m{d[\"duration\"]%60}s)')
print(f'Channel:  {d[\"channel\"]}')
print(f'Has subs: {bool(d.get(\"subtitles\"))}')
print(f'Has auto: {bool(d.get(\"automatic_captions\"))}')
"
```

---

## 12. Chi phí & Lộ trình Scale

### Chi phí hiện tại (0–100 users)

| Dịch vụ | Plan | Chi phí/tháng |
|---------|------|---------------|
| VPS Hostinger KVM2 | 2 vCPU, 8GB RAM | ~$12 |
| Cloudinary | Free (25GB) | $0 |
| OpenAI Whisper API | ~50 videos × 5 phút | ~$1.5 |
| whisper.cpp (shadowing) | Self-hosted | $0 mãi mãi |
| Redis | VPS built-in | $0 |
| SSL (Let's Encrypt) | Free | $0 |
| **Tổng** | | **~$14/tháng** |

### Chi phí khi scale lên 1,000 users

| Dịch vụ | Plan | Chi phí/tháng |
|---------|------|---------------|
| VPS Hostinger KVM4 | 4 vCPU, 16GB RAM | ~$28 |
| Cloudinary | Plus (225GB) | ~$89 → dùng S3 thay thế (~$5) |
| OpenAI Whisper API | ~500 videos | ~$15 |
| **Tổng** | | **~$48/tháng** |

### Khi nào cần chuyển lên Cloud (AWS/GCP)?

```
→ Khi đạt 3,000+ Daily Active Users
→ Khi cần multi-region (users ở nhiều nước)
→ Khi cần auto-scaling (traffic đột biến)
→ Khi cần compliance (GDPR, SOC2...)

Trước ngưỡng đó: VPS Hostinger là đủ và tiết kiệm nhất.
```

---

## Quick Reference — Checklist 5 tuần

```
Tuần 1: ✅ FuzzySharp chấm điểm + DB indexes
Tuần 2: ✅ yt-dlp + VTT parser + Whisper API pipeline
Tuần 3: ✅ Hangfire background jobs + Cloudinary
Tuần 4: ✅ SRS (SM-2) + Streak/Gamification
Tuần 5: ✅ whisper.cpp Shadowing + Nginx + Deploy đúng cách

Sau launch:
  □ Crontab cập nhật yt-dlp hàng tuần
  □ Monitor Hangfire dashboard
  □ Theo dõi chi phí Whisper API hàng tuần
  □ Backup PostgreSQL hàng ngày
```

---

*Tài liệu này tổng hợp toàn bộ kiến trúc kỹ thuật của Demif — ứng dụng nghe chép chính tả tiếng Anh. Cập nhật lần cuối: 2025.*
