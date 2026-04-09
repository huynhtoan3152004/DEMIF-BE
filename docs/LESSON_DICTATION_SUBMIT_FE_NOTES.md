# Dictation Submit - FE Notes

Endpoint: `POST /api/lessons/{id}/dictation/submit`

## Request

Request body không đổi:

```json
{
  "level": "Intermediate",
  "timeSpentSeconds": 120,
  "answers": [
    {
      "segmentIndex": 0,
      "position": 1,
      "userInput": "sleep"
    }
  ]
}
```

## Response changes

Các field cũ vẫn giữ nguyên. Backend đã thêm 3 field mới:

- `answeredAccuracy`: % đúng trên số blank user đã submit.
- `isSubmissionComplete`: `true` khi user đã điền đủ toàn bộ blank.
- `isFullyCorrect`: `true` khi toàn bộ blank của bài đều đúng.

```json
{
  "score": 92,
  "answeredAccuracy": 100,
  "totalBlanks": 12,
  "answeredBlanks": 10,
  "correctCount": 9,
  "incorrectCount": 1,
  "skippedCount": 2,
  "level": "Intermediate",
  "isSubmissionComplete": false,
  "isFullyCorrect": false
}
```

## FE usage

- Dùng `score` để hiển thị điểm tổng của bài.
- Dùng `answeredAccuracy` nếu cần feedback cho phần user đã điền.
- Dùng `isSubmissionComplete` để biết user đã làm đủ bài hay chưa.
- Dùng `isFullyCorrect` để show trạng thái hoàn hảo.
