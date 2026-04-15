# UserVocabulary Overview

## Mục tiêu

`UserVocabulary` là feature cho phép người học lưu các từ vựng chưa biết theo từng bài học và theo chủ đề, sau đó ôn lại theo trạng thái review của riêng mình.

Mục tiêu chính:

- Lưu từ theo `LessonId`.
- Gắn từ với `Topic` để FE dễ lọc theo chủ đề.
- Theo dõi tiến trình ôn tập cơ bản.
- Cung cấp API riêng cho trang cá nhân của user.

## Domain Model

Entity mới: `UserVocabulary`.

Các field chính:

- `UserId`: chủ sở hữu từ vựng.
- `LessonId`: bài học gốc.
- `Topic`: chủ đề hiển thị và filter.
- `Word`: từ được lưu.
- `NormalizedWord`: bản chuẩn hóa để chống trùng.
- `Meaning`: nghĩa/từ dịch.
- `ContextSentence`: câu ví dụ hoặc ngữ cảnh.
- `Note`: ghi chú của người học.
- `ReviewCount`: số lần đã ôn.
- `CorrectReviews`: số lần ôn đúng.
- `IsMastered`: đánh dấu đã thuộc.
- `LastReviewedAt`: thời điểm ôn gần nhất.
- `NextReviewAt`: thời điểm nên ôn tiếp.
- `MasteredAt`: thời điểm đạt trạng thái mastered.

Navigation được gắn vào:

- `User.Vocabularies`
- `Lesson.Vocabularies`

## API

Route được mount dưới `/api/me/vocabulary`.

Các endpoint hiện có:

- `GET /api/me/vocabulary`: lấy danh sách từ vựng của user.
- `GET /api/me/vocabulary/overview`: lấy dashboard tổng quan của bộ từ vựng.
- `GET /api/me/vocabulary/review`: lấy queue ôn tập theo trạng thái review.
- `GET /api/me/vocabulary/due`: lấy các từ đến hạn ôn.
- `GET /api/me/vocabulary/suggestions?lessonId=...`: lấy gợi ý từ vựng từ transcript của một lesson.
- `POST /api/me/vocabulary`: lưu hoặc cập nhật một từ vựng.
- `POST /api/me/vocabulary/{id}/review`: ghi nhận một lần ôn.
- `DELETE /api/me/vocabulary/{id}`: xoá một từ.

## Logic Lưu Từ

Khi user lưu từ mới:

- Nếu không truyền `Topic`, hệ thống sẽ lấy từ `Lesson.Category`, nếu không có thì fallback về `Lesson.Title`.
- Từ được normalize trước khi so trùng.
- Nếu đã có bản ghi cùng `UserId + LessonId + Topic + NormalizedWord`, service sẽ update thay vì tạo mới.
- Mỗi bản ghi mới sẽ có `NextReviewAt = UtcNow + 1 ngày`, nên không xuất hiện ngay trong queue ôn tập.

## Logic Ôn Tập

Mỗi lần review:

- Tăng `ReviewCount`.
- Nếu đúng thì tăng `CorrectReviews`.
- Cập nhật `LastReviewedAt`.
- Tính `NextReviewAt` theo lịch đơn giản: 1, 3, 7, 14, 30 ngày.
- Chỉ đánh dấu `IsMastered` khi user trả lời đúng ở phiên review đúng hạn.
- Review sớm đúng sẽ giữ nguyên lịch hiện tại, nhưng không đẩy word vào trạng thái mastered.

## Trạng thái FE nên dùng

Response của `GET /api/me/vocabulary` và `POST /api/me/vocabulary/{id}/review` có thêm các field:

- `reviewStatus`: `new`, `learning`, `due`, `overdue`, `mastered`.
- `isDue`: dùng để hiển thị badge cần ôn.
- `isOverdue`: dùng để ưu tiên item trễ hạn.
- `daysUntilNextReview`: số ngày còn lại đến lịch ôn tiếp theo, có thể âm nếu quá hạn.
- `lastReviewWasEarly`: chỉ có ý nghĩa sau khi gọi review, để FE hiển thị thông báo “review sớm nên lịch không đổi”.

## FE Validate Gợi Ý

- Dùng `GET /api/me/vocabulary/review` cho màn flashcard ôn tập.
- Dùng `reviewStatus` để gắn badge thay vì tự suy đoán từ `nextReviewAt`.
- Khi `reviewStatus = new`, hiển thị là từ mới đã lưu, chưa cần đưa vào queue ngay.
- Khi `reviewStatus = overdue`, ưu tiên đưa lên đầu danh sách.
- Sau `POST /api/me/vocabulary/{id}/review`, nếu `lastReviewWasEarly = true` thì không nên thông báo đã tiến lịch.

## Database

Migration mới: `AddUserVocabulary`.

Bảng `UserVocabularies` có:

- FK tới `Users`.
- FK tới `Lessons`.
- Unique index trên `UserId + LessonId + Topic + NormalizedWord`.
- Index cho `UserId`, `LessonId`, `Topic`, `NextReviewAt`.

## Application Layer

Service chính:

- `VocabularyService`

Nó xử lý:

- save / upsert
- list
- due review
- review update
- delete

## Testing

Unit tests đã thêm trong:

- `tests/Demif.Tests/Me/VocabularyServiceTests.cs`

Các case được cover:

- Tự lấy `Topic` từ lesson nếu request không gửi topic.
- Upsert khi lưu trùng từ.
- Lấy danh sách due review.
- Review đúng thì cập nhật lịch ôn và trạng thái mastered.

## FE Gợi Ý Sử Dụng

FE có thể dùng feature này để:

- hiển thị dashboard giống Quizlet với tổng số từ, số từ đến hạn, số từ đã mastered và tỷ lệ mastered,
- bấm lưu từ ngay trên màn lesson/dictation/shadowing,
- xem danh sách từ theo bài học,
- lọc theo chủ đề,
- mở màn ôn tập những từ sắp đến hạn.

### Vocabulary Overview

`GET /api/me/vocabulary/overview`

Response:

```json
{
	"totalCount": 24,
	"dueCount": 5,
	"overdueCount": 2,
	"newCount": 6,
	"masteredCount": 9,
	"learningCount": 9,
	"topicCount": 4,
	"lessonCount": 7,
	"recentCount": 3,
	"masteryRate": 37.5,
	"recentItems": []
}
```

### Vocabulary Suggestions

`GET /api/me/vocabulary/suggestions?lessonId=0f8fad5b-d9cb-469f-a165-70867728950e&limit=20`

Response:

```json
{
	"lessonId": "0f8fad5b-d9cb-469f-a165-70867728950e",
	"lessonTitle": "Travel Basics",
	"lessonCategory": "travel",
	"topic": "travel",
	"sourceTextLength": 1240,
	"totalCandidates": 18,
	"items": [
		{
			"word": "journey",
			"normalizedWord": "journey",
			"topic": "travel",
			"lessonId": "0f8fad5b-d9cb-469f-a165-70867728950e",
			"lessonTitle": "Travel Basics",
			"lessonCategory": "travel",
			"contextSentence": "The journey builds courage.",
			"frequency": 2,
			"isAlreadySaved": true
		}
	]
}
```

FE flow gợi ý:

1. Render 4 card ở đầu trang: total, due, mastered, mastery rate.
2. Render danh sách recent items để user quay lại từ đã lưu gần đây.
3. Từ trang lesson, gọi suggestions để show panel "Add to vocabulary".
4. Mỗi suggestion có thể bắn thẳng `POST /api/me/vocabulary` khi user chọn lưu.

## Files Chính

- `src/Demif.Domain/Entities/UserVocabulary.cs`
- `src/Demif.Infrastructure/Persistence/Configurations/UserVocabularyConfiguration.cs`
- `src/Demif.Application/Features/Me/Vocabulary/VocabularyService.cs`
- `src/Demif.Api/Controllers/VocabularyController.cs`
- `src/Demif.Infrastructure/Persistence/Migrations/20260412152832_AddUserVocabulary.cs`
- `tests/Demif.Tests/Me/VocabularyServiceTests.cs`

## FE JSON Contract

### Save Vocabulary

`POST /api/me/vocabulary`

Request:

```json
{
	"lessonId": "0f8fad5b-d9cb-469f-a165-70867728950e",
	"word": "journey",
	"topic": "travel",
	"meaning": "hành trình",
	"contextSentence": "The journey was long but rewarding.",
	"note": "Từ này hay xuất hiện trong bài nghe"
}
```

Rules:

- `lessonId` và `word` là bắt buộc.
- Nếu không gửi `topic`, backend sẽ lấy `Lesson.Category`, nếu không có thì fallback về `Lesson.Title`.
- Nếu lưu trùng `UserId + LessonId + Topic + NormalizedWord`, backend sẽ update bản ghi cũ.

Response:

```json
{
	"id": "6b7f2e2c-0b44-4cb8-b3a2-7f5be5e7c001",
	"userId": "4f1c6a0d-9f65-4c0f-8f2f-3b3d3c80f0b0",
	"lessonId": "0f8fad5b-d9cb-469f-a165-70867728950e",
	"lessonTitle": "Travel Basics",
	"lessonCategory": "travel",
	"topic": "travel",
	"word": "journey",
	"meaning": "hành trình",
	"contextSentence": "The journey was long but rewarding.",
	"note": "Từ này hay xuất hiện trong bài nghe",
	"reviewCount": 0,
	"correctReviews": 0,
	"isDue": false,
	"isOverdue": false,
	"reviewStatus": "new",
	"daysUntilNextReview": 1,
	"isMastered": false,
	"lastReviewedAt": null,
	"nextReviewAt": "2026-04-13T08:00:00Z",
	"masteredAt": null,
	"createdAt": "2026-04-12T08:00:00Z",
	"updatedAt": null
}
```

### Get Vocabulary List

`GET /api/me/vocabulary?page=1&pageSize=20&lessonId=...&topic=travel&search=jour`

Response:

```json
{
	"items": [
		{
			"id": "6b7f2e2c-0b44-4cb8-b3a2-7f5be5e7c001",
			"userId": "4f1c6a0d-9f65-4c0f-8f2f-3b3d3c80f0b0",
			"lessonId": "0f8fad5b-d9cb-469f-a165-70867728950e",
			"lessonTitle": "Travel Basics",
			"lessonCategory": "travel",
			"topic": "travel",
			"word": "journey",
			"meaning": "hành trình",
			"contextSentence": "The journey was long but rewarding.",
			"note": "Từ này hay xuất hiện trong bài nghe",
			"reviewCount": 1,
			"correctReviews": 1,
			"isDue": true,
			"isOverdue": true,
			"reviewStatus": "overdue",
			"daysUntilNextReview": -1,
			"isMastered": false,
			"lastReviewedAt": "2026-04-12T08:15:00Z",
			"nextReviewAt": "2026-04-13T08:15:00Z",
			"masteredAt": null,
			"createdAt": "2026-04-12T08:00:00Z",
			"updatedAt": "2026-04-12T08:15:00Z"
		}
	],
	"totalCount": 1,
	"page": 1,
	"pageSize": 20
}
```

### Get Due Vocabulary

`GET /api/me/vocabulary/due?page=1&pageSize=20`

Response shape giống `GET /api/me/vocabulary`, nhưng chỉ trả các từ đến hạn ôn.

### Get Review Queue

`GET /api/me/vocabulary/review?page=1&pageSize=20`

Response shape giống `GET /api/me/vocabulary`, nhưng chỉ trả các từ cần ôn ngay và có thêm thống kê queue:

```json
{
	"items": [],
	"totalCount": 0,
	"page": 1,
	"pageSize": 20,
	"dueCount": 0,
	"overdueCount": 0,
	"newCount": 0,
	"learningCount": 0,
	"masteredCount": 0
}
```

### Review Vocabulary

`POST /api/me/vocabulary/{id}/review`

Request:

```json
{
	"isCorrect": true
}
```

Response:

```json
{
	"id": "6b7f2e2c-0b44-4cb8-b3a2-7f5be5e7c001",
	"userId": "4f1c6a0d-9f65-4c0f-8f2f-3b3d3c80f0b0",
	"lessonId": "0f8fad5b-d9cb-469f-a165-70867728950e",
	"lessonTitle": "Travel Basics",
	"lessonCategory": "travel",
	"topic": "travel",
	"word": "journey",
	"meaning": "hành trình",
	"contextSentence": "The journey was long but rewarding.",
	"note": "Từ này hay xuất hiện trong bài nghe",
	"reviewCount": 2,
	"correctReviews": 2,
	"isDue": false,
	"isOverdue": false,
	"reviewStatus": "learning",
	"daysUntilNextReview": 3,
	"isMastered": false,
	"lastReviewedAt": "2026-04-12T08:20:00Z",
	"nextReviewAt": "2026-04-15T08:20:00Z",
	"lastReviewWasEarly": false,
	"masteredAt": null,
	"createdAt": "2026-04-12T08:00:00Z",
	"updatedAt": "2026-04-12T08:20:00Z"
}
```

### Delete Vocabulary

`DELETE /api/me/vocabulary/{id}`

Response: `204 No Content`

## JSON Field Map

| Field | Type | Mô tả |
| --- | --- | --- |
| `id` | string | UUID của từ vựng |
| `userId` | string | UUID của user |
| `lessonId` | string | UUID của bài học gốc |
| `lessonTitle` | string | Tên bài học để FE hiển thị |
| `lessonCategory` | string | Category gốc của lesson |
| `topic` | string | Chủ đề để group/filter |
| `word` | string | Từ được lưu |
| `meaning` | string | Nghĩa/từ dịch |
| `contextSentence` | string | Câu ví dụ hoặc ngữ cảnh |
| `note` | string | Ghi chú của user |
| `reviewCount` | number | Số lần ôn |
| `correctReviews` | number | Số lần ôn đúng |
| `isDue` | boolean | Có nằm trong queue ôn ngay không |
| `isOverdue` | boolean | Có bị quá hạn hay không |
| `reviewStatus` | string | Trạng thái FE nên dùng (`new`, `learning`, `due`, `overdue`, `mastered`) |
| `daysUntilNextReview` | number or null | Số ngày còn lại đến lịch ôn tiếp theo |
| `isMastered` | boolean | Đã thuộc hay chưa |
| `lastReviewedAt` | string or null | Thời điểm ôn gần nhất |
| `nextReviewAt` | string or null | Thời điểm nên ôn tiếp |
| `lastReviewWasEarly` | boolean or null | Có phải review sớm hay không, chỉ có ý nghĩa sau khi gọi review |
| `masteredAt` | string or null | Thời điểm đạt mastered |
| `createdAt` | string | Thời điểm tạo |
| `updatedAt` | string or null | Thời điểm cập nhật |
