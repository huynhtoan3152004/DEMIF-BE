# Lesson User API Spec

Tài liệu này mô tả phần lesson ở phía người dùng, tập trung vào 2 luồng chính:

- Dictation: điền từ vào chỗ trống.
- Shadowing: nghe và nói lại theo transcript.

Mục tiêu là để FE biết:

- gọi API nào,
- JSON trả về gồm trường gì,
- validate gì trước khi gọi,
- và lúc nào transcript được dùng để kiểm tra đúng/sai.

## 1. Khác nhau giữa Dictation và Shadowing

### Dictation

- User nhận một template đã được đục lỗ.
- User điền từ còn thiếu vào từng blank.
- Backend chỉ chấm trên các blank đã có trong template.
- Không chấm toàn bộ câu.

### Shadowing

- User nghe 1 segment rồi nói lại.
- FE gửi text nhận diện giọng nói lên backend.
- Backend so word-by-word với transcript gốc của segment.
- Đây là cách check transcript sát nhất ở phía user.

## 2. API user nên dùng theo flow

### A. Xem danh sách lesson

`GET /api/lessons`

Mục đích:

- render danh sách bài học,
- lọc theo level, type, category,
- hiển thị bài free hoặc premium.

### B. Xem chi tiết lesson

`GET /api/lessons/{id}`

Mục đích:

- xem metadata lesson,
- kiểm tra lesson có premium hay không,
- lấy media URL để play YouTube/audio.

### C. Xem dictation exercise

`GET /api/lessons/{id}/dictation?level=Beginner|Intermediate|Advanced|Expert`

Mục đích:

- lấy bài dictation cho level đã chọn,
- backend trả template đã ẩn đáp án,
- FE render input blanks.

### D. Submit dictation

`POST /api/lessons/{id}/dictation/submit`

Mục đích:

- user submit toàn bộ câu trả lời của các blank,
- backend chấm điểm và trả kết quả chi tiết từng blank.

### E. Xem segments của lesson

`GET /api/lessons/{id}/segments?level=Beginner|Intermediate|Advanced|Expert`

Mục đích:

- lấy segment theo level,
- lấy config UI cho FE như show transcript trước hay sau,
- lấy tiến độ đã làm của user.

### F. Check 1 segment kiểu free text

`POST /api/lessons/{id}/segments/{segmentIndex}/check`

Mục đích:

- user gõ tự do đoạn mình nghe được,
- backend so từng từ với transcript gốc,
- trả transcript gốc và diff word-by-word.

### G. Check 1 segment từ voice-to-text

`POST /api/lessons/{id}/segments/{segmentIndex}/check-voice`

Mục đích:

- FE dùng Web Speech API hoặc STT khác,
- gửi text đã nhận diện lên backend,
- backend chấm giống `check` nhưng thêm pass threshold.

### H. Shadowing 1 segment

`POST /api/lessons/{id}/segments/{segmentIndex}/shadowing`

Mục đích:

- user nói lại cả segment,
- backend so transcript gốc với text nhận diện,
- trả feedback về độ đúng phát âm và độ khớp nội dung.

## 3. JSON fields trả về cho FE

## 3.1 Get lessons

Response chính:

```json
{
  "items": [
    {
      "id": "guid",
      "title": "string",
      "description": "string",
      "lessonType": "Dictation",
      "level": "Beginner",
      "category": "academic",
      "mediaUrl": "string",
      "audioUrl": "string",
      "mediaType": "youtube",
      "videoId": "string",
      "embedUrl": "string",
      "durationSeconds": 123,
      "thumbnailUrl": "string",
      "isPremiumOnly": false,
      "completionsCount": 10,
      "avgScore": 88.5,
      "tags": "youtube,dictation"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 20,
  "totalPages": 2
}
```

Fields chính cần FE dùng:

- `id`
- `title`
- `level`
- `mediaUrl`
- `mediaType`
- `embedUrl`
- `thumbnailUrl`
- `isPremiumOnly`

### Validate phía FE

- `page >= 1`
- `pageSize` nên clamp 1-100
- `level` nếu có thì chỉ nhận `Beginner`, `Intermediate`, `Advanced`, `Expert`
- `type` nếu có thì phải match enum lesson type của backend

## 3.2 Get lesson by id

Response chính:

```json
{
  "id": "guid",
  "title": "string",
  "description": "string",
  "lessonType": "Dictation",
  "level": "Intermediate",
  "category": "academic",
  "mediaUrl": "string",
  "audioUrl": "string",
  "mediaType": "youtube",
  "videoId": "string",
  "embedUrl": "string",
  "durationSeconds": 123,
  "thumbnailUrl": "string",
  "fullTranscript": "string",
  "hasDictationExercise": true,
  "availableDictationLevels": ["Beginner", "Intermediate"],
  "isPremiumOnly": false,
  "completionsCount": 10,
  "avgScore": 88.5,
  "tags": "youtube,dictation",
  "createdAt": "2026-04-07T00:00:00Z"
}
```

Ý nghĩa:

- `fullTranscript` chỉ là text để hiển thị/preview, không phải đáp án dictation.
- `hasDictationExercise` cho biết lesson có template hay chưa.
- `availableDictationLevels` cho FE biết level nào có thể mở.

### Validate phía FE

- Nếu `mediaType = youtube` thì render iframe theo `embedUrl` hoặc `videoId`.
- Nếu `isPremiumOnly = true` mà user chưa có quyền thì chặn UI.
- Nếu `hasDictationExercise = false` thì không show tab dictation.

## 3.3 Get dictation exercise

Response chính:

```json
{
  "lessonId": "guid",
  "title": "string",
  "description": "string",
  "mediaUrl": "string",
  "audioUrl": "string",
  "mediaType": "youtube",
  "videoId": "string",
  "embedUrl": "string",
  "durationSeconds": 123,
  "level": "Intermediate",
  "template": {
    "segments": [
      {
        "startTime": 0,
        "endTime": 2.5,
        "words": [
          {
            "text": "waking",
            "isBlank": false,
            "answer": null,
            "position": 0
          },
          {
            "text": "sleep",
            "isBlank": true,
            "answer": null,
            "position": 1
          }
        ]
      }
    ],
    "totalBlanks": 12
  },
  "thumbnailUrl": "string"
}
```

Điểm quan trọng:

- `answer` đã bị strip ra khỏi response cho user.
- FE chỉ được render ô nhập cho `isBlank = true`.
- `position` phải được giữ đúng để submit.

### Validate phía FE

- `level` phải là 1 trong 4 level hợp lệ.
- `template.segments` không được rỗng.
- Mỗi blank phải có `segmentIndex` và `position` rõ ràng.
- Không tự suy đoán đáp án từ `text`, vì backend cố tình đã xóa `answer`.

## 3.4 Submit dictation

Request body:

```json
{
  "level": "Intermediate",
  "answers": [
    {
      "segmentIndex": 0,
      "position": 1,
      "userInput": "sleep"
    }
  ],
  "timeSpentSeconds": 120
}
```

Response chính:

```json
{
  "exerciseId": "guid",
  "score": 92,
  "totalBlanks": 12,
  "answeredBlanks": 10,
  "correctCount": 9,
  "incorrectCount": 1,
  "skippedCount": 2,
  "level": "Intermediate",
  "timeSpentSeconds": 120,
  "results": [
    {
      "segmentIndex": 0,
      "position": 1,
      "isCorrect": true,
      "userInput": "sleep",
      "correctAnswer": "sleep",
      "message": "Chính xác!"
    }
  ]
}
```

### Validate phía FE

- `level` bắt buộc.
- `answers` không được rỗng nếu user muốn submit.
- `segmentIndex` phải đúng với segment trong template.
- `position` phải đúng với blank position.
- `userInput` không nên rỗng.
- `timeSpentSeconds` nên là số nguyên dương.

### Ý nghĩa check transcript

Dictation không kiểm tra toàn bộ transcript của user.
Nó chỉ đối chiếu các blank trong template với `correctAnswer` gốc.

## 3.5 Get segments

Response chính:

```json
{
  "lessonId": "guid",
  "title": "string",
  "description": "string",
  "audioUrl": "string",
  "mediaType": "youtube",
  "durationSeconds": 123,
  "thumbnailUrl": "string",
  "isPremiumOnly": false,
  "levelConfig": {
    "level": "Intermediate",
    "showTranscriptBefore": false,
    "showTranscriptAfter": true,
    "maxReplays": 3,
    "showWordCount": true
  },
  "segments": [
    {
      "index": 0,
      "startTime": 0,
      "endTime": 2.5,
      "wordCount": 8,
      "text": null,
      "isCompleted": false,
      "bestScore": null,
      "attempts": null
    }
  ],
  "totalSegments": 18,
  "completedCount": 3,
  "progressPercent": 16.7
}
```

### Validate phía FE

- `level` phải hợp lệ.
- `segments` dùng để render player theo từng đoạn.
- Nếu `showTranscriptBefore = false`, FE không nên tự hiện text trước.
- `maxReplays = -1` nghĩa là không giới hạn replay.

## 3.6 Check segment

Request body:

```json
{
  "level": "Intermediate",
  "userText": "I can just do it on my own",
  "timeSpentSeconds": 30
}
```

Response chính:

```json
{
  "segmentIndex": 0,
  "accuracy": 85.7,
  "correctCount": 6,
  "totalWords": 7,
  "wrongCount": 1,
  "skippedCount": 0,
  "transcript": "I can just do it on my own",
  "wordResults": [
    {
      "word": "I",
      "status": "correct",
      "userTyped": null
    }
  ]
}
```

### Validate phía FE

- `level` bắt buộc.
- `userText` không được rỗng.
- `timeSpentSeconds` nên là số dương nếu có.

### Ý nghĩa check transcript

Đây là check transcript theo segment đầy đủ.
Nó so từng từ user gõ với transcript gốc, nên phù hợp hơn với shadowing hoặc luyện free typing.

## 3.7 Check voice segment

Request body:

```json
{
  "level": "Intermediate",
  "spokenText": "I can just do it on my own",
  "speechConfidence": 0.92,
  "timeSpentSeconds": 28
}
```

Response chính:

```json
{
  "segmentIndex": 0,
  "accuracy": 85.7,
  "correctCount": 6,
  "totalWords": 7,
  "wrongCount": 1,
  "skippedCount": 0,
  "transcript": "I can just do it on my own",
  "wordResults": [],
  "spokenText": "I can just do it on my own",
  "speechConfidence": 0.92,
  "passThreshold": 80,
  "isPassed": true
}
```

### Validate phía FE

- `spokenText` không được rỗng.
- `level` bắt buộc.
- `speechConfidence` là optional, khoảng 0-1.

## 3.8 Shadowing

Request body:

```json
{
  "level": "Advanced",
  "userText": "I can just do it on my own",
  "timeSpentSeconds": 40
}
```

Response chính:

```json
{
  "segmentIndex": 0,
  "accuracy": 85.7,
  "correctCount": 6,
  "wrongCount": 1,
  "skippedCount": 0,
  "totalWords": 7,
  "feedback": "✅ Good job! A few words need more practice.",
  "passed": true,
  "targetText": "I can just do it on my own",
  "userSpoke": "I can just do it on my own",
  "wordResults": [
    {
      "word": "I",
      "status": "correct",
      "userSpoken": null
    }
  ]
}
```

### Validate phía FE

- `level` bắt buộc.
- `userText` không được rỗng.
- Nên hiển thị feedback sau khi submit.

## 4. FE nên validate gì trước khi call

### 4.1 Validations chung

- Không gọi API với `lessonId` rỗng.
- Không gọi nếu chưa login ở các endpoint cần auth.
- Kiểm tra role premium nếu lesson là premium only.
- Validate `level` theo enum hợp lệ.

### 4.2 Validations cho Dictation

- `answers` phải map đúng `segmentIndex` và `position`.
- Không cho submit nếu chưa điền tất cả blank bắt buộc.
- Không cho phép blank trống nếu rule bài học yêu cầu.
- Giữ đúng thứ tự blank theo response của backend.

### 4.3 Validations cho Shadowing

- Cần có `userText` hoặc `spokenText` từ STT.
- Không nên call nếu text quá ngắn hoặc không có audio input.
- Nếu browser không hỗ trợ STT thì dùng text fallback.

### 4.4 Validations cho Segments

- `level` phải là một trong 4 level.
- Nếu `showTranscriptBefore = false` thì FE không tự reveal trước.
- Nếu `maxReplays` là số hữu hạn thì phải khóa nút replay sau giới hạn.

## 5. Có check lại transcript đúng không?

Có, nhưng tùy luồng:

- Dictation: kiểm tra các blank đã chọn, không kiểm tra toàn bộ transcript.
- Check segment / shadowing: kiểm tra word-by-word với transcript gốc của segment.

Ví dụ:

- Transcript gốc: `waking up later and getting more sleep has had a dramatic impact`
- Dictation blank: `more sleep`, `dramatic impact`
- User nhập đúng blank → pass dictation.
- User shadowing cả câu nhưng thiếu vài từ → vẫn bị trừ điểm ở shadowing.

## 6. Gợi ý cho FE

- Dùng `GET /api/lessons/{id}` để dựng trang chi tiết.
- Dùng `GET /api/lessons/{id}/segments?level=...` cho Shadowing.
- Dùng `GET /api/lessons/{id}/dictation?level=...` cho Dictation.
- Dùng `POST /api/lessons/{id}/dictation/submit` khi user xong bài.
- Dùng `POST /api/lessons/{id}/segments/{segmentIndex}/check` hoặc `check-voice` nếu muốn chấm từng segment.

## 7. Quy tắc UX nên giữ

- Beginner: hiển thị transcript trước, dễ học hơn.
- Intermediate: ẩn transcript trước, hiện sau submit.
- Advanced/Expert: ẩn mạnh hơn và giảm replay.
- Moderator chỉ nên sửa blank ở từ thật sự quan trọng, tránh đục lỗ quá dày.
