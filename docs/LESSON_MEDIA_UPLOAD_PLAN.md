# Lesson Media Upload Plan

Tài liệu này mô tả cách triển khai upload audio MP3 riêng, giữ tách biệt với flow YouTube, và mở rộng bộ lọc `GET` cho user/admin.

## Mục tiêu

1. Upload MP3 riêng qua endpoint độc lập.
2. Không làm ảnh hưởng flow YouTube hiện tại.
3. Quick-create lesson vẫn dùng chung contract `MediaUrl`, `MediaType`, `Tags`.
4. `GET /api/lessons` và `GET /api/admin/lessons` có filter tốt hơn cho FE.
5. FE có JSON contract rõ ràng để call dễ hơn.

## Hiện trạng cần giữ

- YouTube import vẫn dùng `POST /api/admin/lessons/from-youtube`.
- Transcript vẫn đi qua `PATCH /api/admin/lessons/{id}/transcript`.
- Quick-create hiện đã nhận `MediaUrl` và `MediaType`.
- Legacy row có thể chỉ có `AudioUrl`, nên backend phải coi `AudioUrl` null `MediaType` như audio.

## Endpoint mới cho MP3

### Upload audio

`POST /api/admin/lessons/audio/upload`

Body: `multipart/form-data`

```json
{
  "AudioFile": "<mp3 file>",
  "FolderName": "demif-lessons/audio"
}
```

Response:

```json
{
  "mediaUrl": "https://res.cloudinary.com/.../sample.mp3",
  "audioUrl": "https://res.cloudinary.com/.../sample.mp3",
  "mediaType": "audio",
  "folderName": "demif-lessons/audio",
  "fileName": "sample.mp3",
  "fileSize": 1234567
}
```

## Flow đề xuất cho FE

### Flow audio

1. FE chọn file MP3.
2. FE gọi `POST /api/admin/lessons/audio/upload`.
3. FE nhận `mediaUrl`.
4. FE gọi `POST /api/admin/lessons/quick-create` với `mediaUrl`, `mediaType = audio`, `tags`, `category`.
5. Nếu transcript chưa có, FE gọi `PATCH /api/admin/lessons/{id}/transcript` sau.

### Flow YouTube

1. FE gọi `GET /api/admin/lessons/youtube/preview`.
2. FE gọi `POST /api/admin/lessons/from-youtube`.
3. Nếu cần sửa transcript, gọi `PATCH /transcript`.

## Quick-create contract cho audio

### Request

```json
{
  "title": "BBC Short Audio: Daily Routine",
  "description": "Short audio lesson for beginner listening",
  "transcript": "00:00:00.030\nI wake up early every day\n00:00:02.000\nand drink a cup of coffee",
  "format": "auto",
  "mediaUrl": "https://res.cloudinary.com/.../sample.mp3",
  "mediaType": "audio",
  "durationSeconds": 12,
  "level": "Beginner",
  "lessonType": "Dictation",
  "category": "daily",
  "isPremiumOnly": false,
  "displayOrder": 0,
  "tags": "bbc,daily,short,audio",
  "thumbnailUrl": "https://res.cloudinary.com/.../thumb.jpg"
}
```

### Response

```json
{
  "lessonId": "00000000-0000-0000-0000-000000000000",
  "title": "BBC Short Audio: Daily Routine",
  "status": "draft",
  "segmentCount": 2,
  "wordCount": 13,
  "durationSeconds": 12,
  "hasDictationTemplates": true,
  "transcript": {
    "requestedFormat": "auto",
    "detectedFormat": "timed",
    "fullTranscript": "I wake up early every day and drink a cup of coffee",
    "segmentCount": 2,
    "wordCount": 13,
    "segments": []
  },
  "message": "..."
}
```

## Filter contract cho FE

### User list

`GET /api/lessons?page=1&pageSize=10&level=Beginner&type=Dictation&category=business&mediaType=audio&tag=bbc&search=english`

### Admin list

`GET /api/admin/lessons?page=1&pageSize=10&status=draft&level=Beginner&type=Dictation&category=business&mediaType=audio&tag=bbc&search=english&isPremiumOnly=false`

## JSON contract nên dùng ở FE

### Filter object

```json
{
  "page": 1,
  "pageSize": 10,
  "status": "draft",
  "level": "Beginner",
  "type": "Dictation",
  "category": "business",
  "mediaType": "audio",
  "tag": "bbc",
  "search": "english",
  "isPremiumOnly": false
}
```

### Lesson card payload

```json
{
  "id": "...",
  "title": "The future of food",
  "description": "...",
  "lessonType": "Dictation",
  "level": "Beginner",
  "category": "bbc",
  "mediaUrl": "https://res.cloudinary.com/.../sample.mp3",
  "mediaType": "audio",
  "durationSeconds": 45,
  "thumbnailUrl": "https://res.cloudinary.com/.../thumb.jpg",
  "isPremiumOnly": false,
  "tags": "bbc,daily,short"
}
```

## Lỗ hổng cần tránh

1. Không dùng chung endpoint upload audio với YouTube.
2. Không ép quick-create phải upload file; quick-create chỉ nhận URL media.
3. Không để `AudioUrl` và `MediaUrl` lệch nhau nếu lesson là audio.
4. Không filter tag bằng logic quá phức tạp ở MVP; dùng normalize lowercase và chứa chuỗi trước.
5. Không làm lại toàn bộ `GET` bằng một endpoint mới nếu chưa cần grouping theo section.
6. Không quên legacy lessons: `MediaType` null vẫn phải hiển thị như audio.

## Plan triển khai chi tiết

### Phase 1 - Hạ tầng upload audio

- Thêm `IAudioUploadService`.
- Thêm `CloudinaryAudioUploadService`.
- Register DI.
- Tạo endpoint `POST /api/admin/lessons/audio/upload`.

### Phase 2 - Quick-create audio

- Giữ `POST /api/admin/lessons/quick-create`.
- FE upload MP3 trước, lấy `mediaUrl`.
- FE submit `quick-create` với `mediaType = audio`.
- Bổ sung guideline FE để set `tags` và `category` chuẩn.

### Phase 3 - Mở rộng filter

- Public list: thêm `mediaType`, `tag`, `search`.
- Admin list: thêm `status`, `mediaType`, `tag`, `search`, `isPremiumOnly`.
- Repository: filter legacy audio đúng cách khi `MediaType` null.

### Phase 4 - Test

- Test public filter truyền đúng param xuống repository.
- Test admin filter truyền đúng param xuống repository.
- Test upload endpoint chặn file không phải MP3.
- Test upload endpoint trả đúng JSON contract.

### Phase 5 - FE integration

- FE thêm form upload audio riêng.
- FE map lesson card badge theo `mediaType`.
- FE thêm tag chips và filter panel.

## Ghi chú kiến trúc

- `Category` là nhóm lớn.
- `Tags` là nhãn nhỏ để lọc UI.
- `MediaType` quyết định player.
- `MediaUrl` là URL phát chính.
- `AudioUrl` chỉ là legacy alias để không vỡ dữ liệu cũ.
