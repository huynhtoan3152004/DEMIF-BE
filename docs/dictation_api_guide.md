# Hướng dẫn tích hợp API Dictation (Điền từ khuyết)

Tài liệu này cung cấp đầy đủ thông tin về các Endpoint liên quan đến chức năng Dictation (Luyện nghe chép chính tả), cấu trúc Response, và cách xử lý các trường dữ liệu.

## 1. Lấy Bài Tập Dictation Theo Level

**Ví dụ:** Người dùng chọn độ khó là `Intermediate` để bắt đầu luyện tập một bài học.

- **Endpoint**: `GET /api/lessons/{id}/dictation`
- **Method**: GET
- **Auth Required**: Không bắt buộc (Nhưng nếu bài Premium thì yêu cầu Token có Subcription).
- **Query Params**:
  - `level` (string, default: "Beginner"): Mức độ khó của bài tập. Cho phép truyền: `Beginner`, `Intermediate`, `Advanced`, `Expert`. *(Lưu ý: FE gọi `?level=...` nhé, Backend đã fix lỗi không nhận param).*

**Response (200 OK):**
```json
{
  "lessonId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Tên bài học",
  "description": "Mô tả bài học",
  "mediaUrl": "https://example.com/audio.mp3",
  "audioUrl": "https://example.com/audio.mp3", // (Legacy - Nên dùng mediaUrl)
  "mediaType": "audio", // "audio" | "youtube" | "video"
  "videoId": null, // Nếu youtube thì đây là ID của YouTube
  "embedUrl": null, // URL dành cho iframe Youtube
  "durationSeconds": 120,
  "level": "Intermediate",
  "thumbnailUrl": "https://example.com/thumb.jpg",
  
  "template": {
    "level": "Intermediate",
    "blankPercentage": 35, // 35% số từ bị làm khuyết
    "totalBlanks": 15,
    "totalWords": 45,
    "segments": [
      {
        "startTime": 0.0,
        "endTime": 5.5,
        "originalText": "Hello, how are you today?",
        "words": [
          {
            "text": "Hello,",
            "isBlank": false,
            "position": 0,
            "punctuation": ","
          },
          {
            "text": "",
            "isBlank": true,
            "position": 1,
            "punctuation": "",
            "hint": "h__",     // Gợi ý cho người dùng chữ cái đầu
            "length": 3,       // Độ dài ô input cần render
            "answer": null     // BACKEND ĐÃ XÓA ANSWER ĐỂ NGĂN CHEAT
          }
          // ... Các từ tiếp theo
        ]
      }
    ]
  }
}
```

### Hướng dẫn Render Giao diện (Thẻ `template`)
- Chạy lặp qua `segments`, rồi lặp tiếp qua `words`.
- Nếu `isBlank == false`: In ra HTML tag text thuờng là `word.text + word.punctuation + " "`.
- Nếu `isBlank == true`: In ra HTML thẻ `<input>` có độ dài maxlength là `word.length`. Có thể hiển thị placeholder mờ mờ bằng giá trị của `word.hint`.

---

## 2. Kiểm Tra Đáp Án 1 Segment (Check Segment)

Khi User đang nghe một câu (1 segment) và gõ các từ khuyết, sau đó bấm "Kiểm tra / Nộp câu này".

- **Endpoint**: `POST /api/lessons/{id}/segments/{segmentIndex}/check`
- **Method**: POST
- **Auth Required**: Có (Bearer Token)
- **Body**:
```json
{
  "userAnswers": {
    "0": "Hello",  // key là vị trí 'position' của khuyết
    "1": "how"
  }
}
```

**Response (200 OK):**
```json
{
  "segmentIndex": 0,
  "isCorrect": true,  // Cả câu đúng hoàn toàn
  "score": 100.0,
  "wordResults": [
    {
      "position": 1,
      "userAnswer": "hou",
      "correctAnswer": "how", // Backend trả về đáp án đúng nếu sai!
      "isCorrect": false,
      "isBlank": true,
      "hint": "h__"
    }
  ]
}
```

---

## 3. Submit Cả Bài Dictation (Lưu Lịch Sử / Điểm)

Gửi khi người dùng bấm "Nộp Bài Toàn Bộ" ở cuối buổi học.

- **Endpoint**: `POST /api/lessons/{id}/dictation/submit`
- **Method**: POST
- **Auth Required**: Có (Bearer Token)
- **Body**:
```json
{
  "level": "Intermediate",
  "totalTimeSpentSeconds": 150,
  "segments": [
    {
      "segmentIndex": 0,
      "isCorrect": true,
      "score": 100.0,
      "userAnswers": {
        "1": "how"
      }
    }
  ]
}
```

**Response (200 OK):**
```json
{
  "progressId": "...",
  "lessonId": "...",
  "level": "Intermediate",
  "overallScore": 95.5,
  "totalBlanks": 15,
  "correctBlanks": 14,
  "completedAt": "2026-04-14T07:00:00Z"
}
```
