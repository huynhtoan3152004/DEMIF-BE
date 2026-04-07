# Lesson Workflow

Tài liệu này mô tả luồng Lesson hiện tại trong backend và API nên dùng ở từng bước.

## Mục tiêu

Luồng chuẩn nên hiểu như sau:

1. Admin/Moderator tạo lesson từ YouTube hoặc import transcript thủ công.
2. Backend tạo `FullTranscript`, `TimedTranscript` và sinh `DictationTemplates`.
3. Moderator rà lại, chỉnh chỗ khuyết nếu cần.
4. Publish lesson.
5. User xem lesson, lấy segments, làm dictation hoặc shadowing.

## Kết Luận Ngắn

- Backend không gọi trực tiếp Tactiq.io.
- Nếu bạn lấy transcript từ Tactiq.io, cách đúng là copy transcript đó vào API cập nhật transcript thủ công.
- FE không cần tự tách segment nữa: chỉ cần paste nguyên block transcript, backend sẽ auto-detect `auto|srt|vtt|plain`, parse xong và trả về `transcript.segments` + `transcript.fullTranscript`.
- Segment của lesson được tạo từ `TimedTranscript`.
- Dictation template được sinh từ transcript đã có timing.
- Moderator/Admin chỉ can thiệp ở bước review, chỉnh blank, và publish.

## API Nên Dùng Theo Từng Bước

### 1. Xem trước YouTube trước khi tạo lesson

Dùng khi admin chỉ có YouTube URL và muốn kiểm tra video có caption hay không.

- `GET /api/admin/lessons/youtube/preview?url=...`
- `GET /api/admin/lessons/youtube/transcripts?url=...&preferredLanguage=en&includeText=true`

Mục đích:

- Kiểm tra metadata video.
- Xem video có captions không.
- Lấy transcript từ YouTube nếu có.

### 2. Tạo lesson từ YouTube

Dùng khi video đã có caption và muốn import tự động.

- `POST /api/admin/lessons/from-youtube`

Hành vi backend:

- Lấy title, thumbnail, duration, embed URL.
- Lấy captions/transcript từ YouTube.
- Tạo `FullTranscript` và `TimedTranscript`.
- Tự sinh `DictationTemplates` cho 4 levels.
- Tạo lesson ở trạng thái `draft`.

### 3. Tạo lesson thủ công từ transcript

Dùng khi bạn có transcript từ Tactiq.io, SRT, VTT, hoặc text thuần.

Có 2 API phù hợp:

- `POST /api/admin/lessons/quick-create`
- `PATCH /api/admin/lessons/{id}/transcript`

Chọn API nào:

- Nếu tạo lesson mới từ đầu, dùng `quick-create`.
- Nếu lesson đã có sẵn và chỉ cần thay transcript, dùng `PATCH /transcript`.

Ghi chú:

- `quick-create` phù hợp khi bạn paste transcript ngay từ đầu.
- `PATCH /transcript` phù hợp khi bạn copy transcript từ Tactiq.io hoặc sửa transcript thủ công sau khi xem preview.
- Nếu không muốn FE tự đoán format, hãy gửi `format: "auto"` hoặc bỏ `format`; backend sẽ detect block có timestamp hay plain text.
- Response của cả hai API đều trả `transcript.fullTranscript` và `transcript.segments`, nên FE chỉ render text, còn `startTime/endTime` dùng cho sync player.

### Transcript contract cho FE

FE nên dùng chung contract này cho `quick-create` và `PATCH /transcript`:

```json
{
	"requestedFormat": "auto",
	"detectedFormat": "timed",
	"fullTranscript": "9:00 in the morning and Cassie is still in bed ...",
	"segmentCount": 2,
	"wordCount": 41,
	"segments": [
		{
			"index": 0,
			"startTime": 0.03,
			"endTime": 2.79,
			"text": "9:00 in the morning and Cassie is still",
			"wordCount": 8
		}
	]
}
```

Ý nghĩa field:

- `requestedFormat`: giá trị FE gửi lên, thường là `auto`.
- `detectedFormat`: `timed` hoặc `plain`.
- `fullTranscript`: chuỗi text liền mạch để preview/regenerate.
- `segments`: list segment đã chuẩn hóa, FE dùng `text` để hiển thị và `startTime/endTime` để sync player.

### Dictation template contract cho FE

Khi moderator/custom editor chỉnh blanks, backend giữ nguyên theo từng level và từng word:

```json
{
	"beginner": {
		"level": "Beginner",
		"blankPercentage": 15,
		"segments": [
			{
				"startTime": 0,
				"endTime": 2.5,
				"originalText": "Hello world, welcome to class.",
				"words": [
					{ "text": "Hello", "isBlank": false, "position": 0 },
					{ "text": "", "isBlank": true, "position": 1, "answer": "world" },
					{ "text": "welcome", "isBlank": false, "position": 2 },
					{ "text": "to", "isBlank": false, "position": 3 },
					{ "text": "class.", "isBlank": false, "position": 4 }
				]
			}
		]
	},
	"intermediate": {
		"level": "Intermediate",
		"blankPercentage": 30,
		"segments": [
			{
				"startTime": 0,
				"endTime": 2.5,
				"originalText": "Hello world, welcome to class.",
				"words": [
					{ "text": "", "isBlank": true, "position": 0, "answer": "Hello" },
					{ "text": "", "isBlank": true, "position": 1, "answer": "world" },
					{ "text": "welcome", "isBlank": false, "position": 2 },
					{ "text": "to", "isBlank": false, "position": 3 },
					{ "text": "class.", "isBlank": false, "position": 4 }
				]
			}
		]
	}
}
```

Điểm cần nhớ:

- `level` là bắt buộc cho mỗi template.
- Moderator/admin có thể giữ nguyên cùng một `segment` nhưng tạo nhiều template khác nhau theo level để độ khó tăng dần.
- `segments[].words` là bắt buộc, vì đây là nơi giữ `isBlank`, `answer`, `hint`, `position`.
- `GET /api/admin/lessons/{id}/dictation-preview` giờ trả thêm `dictationTemplates` theo level để FE/moderator xem đúng blanks đã gán.

### 4. Xem preview dictation cho moderator

Dùng để moderator nhìn thấy toàn bộ segment và đáp án trước khi publish.

- `GET /api/admin/lessons/{id}/dictation-preview`

Mục đích:

- Kiểm tra segments có đúng không.
- Soát lại chỗ khuyết.
- Xác nhận lesson đã sẵn sàng để publish.

### 5. Moderator chỉnh blanks thủ công

Dùng khi muốn chọn từ nào khuyết, từ nào không khuyết.

- `PUT /api/admin/lessons/{id}/dictation-templates`

Luồng này là phần bạn hỏi chính:

- Admin/import transcript xong, backend sinh template tự động.
- Moderator mở preview.
- Moderator chỉnh lại JSON templates nếu cần.
- Backend lưu đè template mới.

### 6. Publish lesson

Dùng sau khi transcript và dictation template đã ổn.

- `PATCH /api/admin/lessons/{id}/status`

Thường set:

- `draft` → `published`
- hoặc `draft` → `archived`

### 7. User xem lesson

Dùng cho frontend user.

- `GET /api/lessons`
- `GET /api/lessons/{id}`
- `GET /api/lessons/{id}/dictation?level=Beginner`
- `GET /api/lessons/{id}/segments?level=Intermediate`

### 8. User làm bài

Dùng để submit và chấm điểm.

- `POST /api/lessons/{id}/dictation/submit`
- `POST /api/lessons/{id}/segments/{segmentIndex}/check`
- `POST /api/lessons/{id}/segments/{segmentIndex}/check-voice`
- `POST /api/lessons/{id}/segments/{segmentIndex}/shadowing`

## Luồng Chi Tiết Đề Xuất

### Luồng A: YouTube tự động

1. Admin gọi `GET /api/admin/lessons/youtube/preview`.
2. Nếu video có captions tốt, admin gọi `POST /api/admin/lessons/from-youtube`.
3. Backend tự tạo `TimedTranscript`, `FullTranscript`, `DictationTemplates`.
4. Moderator xem `GET /api/admin/lessons/{id}/dictation-preview`.
5. Nếu cần, moderator sửa bằng `PUT /api/admin/lessons/{id}/dictation-templates`.
6. Publish bằng `PATCH /api/admin/lessons/{id}/status`.

### Luồng B: Transcript từ Tactiq.io

1. Admin lấy transcript từ Tactiq.io.
2. Admin tạo lesson bằng `POST /api/admin/lessons/quick-create`, hoặc mở lesson có sẵn rồi dùng `PATCH /api/admin/lessons/{id}/transcript`.
3. Backend parse transcript, tạo `TimedTranscript` và `FullTranscript`.
4. Backend sinh `DictationTemplates` tự động.
5. Moderator xem preview, chỉnh blanks nếu cần.
6. Publish lesson.

### Luồng C: Chỉnh thủ công cho lesson đã có

1. Admin mở lesson chi tiết bằng `GET /api/admin/lessons/{id}`.
2. Nếu transcript chưa ổn, update bằng `PATCH /api/admin/lessons/{id}/transcript`.
3. Xem lại preview bằng `GET /api/admin/lessons/{id}/dictation-preview`.
4. Chỉnh blanks bằng `PUT /api/admin/lessons/{id}/dictation-templates`.
5. Publish bằng `PATCH /api/admin/lessons/{id}/status`.

## Mối Liên Hệ Giữa Transcript, Segments Và Blanks

- `TimedTranscript` là nguồn chuẩn để tách segment.
- `FullTranscript` là chuỗi text liền mạch dùng cho preview và regenerate.
- `DictationTemplates` là JSON đã được đục lỗ cho từng level.
- User không được thấy đáp án trong dictation exercise.
- Admin preview thì thấy toàn bộ đáp án.

## Khi Nào Dùng API Nào

- Muốn import tự động từ YouTube: dùng `POST /api/admin/lessons/from-youtube`.
- Muốn xem YouTube có caption không: dùng `GET /api/admin/lessons/youtube/preview`.
- Muốn lấy transcript từ YouTube trước khi import: dùng `GET /api/admin/lessons/youtube/transcripts`.
- Muốn dán transcript từ Tactiq.io: dùng `PATCH /api/admin/lessons/{id}/transcript`.
- Muốn moderator chỉnh blanks: dùng `PUT /api/admin/lessons/{id}/dictation-templates`.
- Muốn xuất bản lesson: dùng `PATCH /api/admin/lessons/{id}/status`.
- Muốn user học bài: dùng các API dưới `/api/lessons/...`.

## Ghi Chú Thực Tế

- `GET /api/lessons/{id}/dictation` trả dictation exercise đã ẩn đáp án.
- `GET /api/admin/lessons/{id}/dictation-preview` là endpoint dành cho admin/moderator để rà lại đáp án.
- `GET /api/lessons/{id}/segments?level=...` trả các segment và cấu hình UI theo level.
- `POST /api/lessons/{id}/segments/{segmentIndex}/check` và `POST /api/lessons/{id}/segments/{segmentIndex}/shadowing` là luồng luyện từng đoạn.

## Dictation Vs Shadowing

Hai luồng này dùng chung lesson và transcript, nhưng mục tiêu khác nhau:

### Dictation

- User nghe hoặc đọc đoạn text đã được đục lỗ.
- User nhập từ bị khuyết vào blank.
- Backend chấm theo từng blank, đối chiếu với đáp án gốc trong `DictationTemplates`.
- API chính:
	- `GET /api/lessons/{id}/dictation?level=...`
	- `POST /api/lessons/{id}/dictation/submit`

### Shadowing

- User nghe và nói lại toàn bộ đoạn.
- Frontend gửi text nhận dạng giọng nói từ browser.
- Backend so sánh word-by-word với transcript gốc của segment.
- API chính:
	- `GET /api/lessons/{id}/segments?level=...`
	- `POST /api/lessons/{id}/segments/{segmentIndex}/shadowing`
	- `POST /api/lessons/{id}/segments/{segmentIndex}/check-voice`

### Kết luận kiến trúc

- Dictation là chấm theo đáp án của blank, nên độ chính xác phụ thuộc vào template moderator đã chọn.
- Shadowing là chấm theo transcript thật của segment, nên nó kiểm tra mức độ khớp với nội dung gốc tốt hơn.
- Cả hai đều dựa trên cùng một nguồn sự thật là `TimedTranscript` và `FullTranscript`.

## Flow Chi Tiết Theo API

### 1. Admin tạo lesson

Tùy nguồn vào:

- YouTube có caption: `GET /api/admin/lessons/youtube/preview` → `POST /api/admin/lessons/from-youtube`
- Transcript từ Tactiq hoặc SRT/VTT: `POST /api/admin/lessons/quick-create` hoặc `PATCH /api/admin/lessons/{id}/transcript`

### 2. Backend sinh dữ liệu học liệu

Sau khi có transcript:

- `TimedTranscript` được tạo ra từ caption/timestamp.
- `FullTranscript` là chuỗi text liền mạch.
- `DictationTemplates` được auto-generate.

### 3. Moderator review

- Xem toàn bộ nội dung bằng `GET /api/admin/lessons/{id}/dictation-preview`.
- Nếu cần, sửa blanks bằng `PUT /api/admin/lessons/{id}/dictation-templates`.
- Nếu transcript sai hoặc thiếu, cập nhật bằng `PATCH /api/admin/lessons/{id}/transcript`.

### 4. Publish

- `PATCH /api/admin/lessons/{id}/status` với `published`.

### 5. User học theo level

- Xem bài: `GET /api/lessons/{id}`.
- Lấy segments: `GET /api/lessons/{id}/segments?level=Beginner|Intermediate|Advanced|Expert`.
- Lấy dictation: `GET /api/lessons/{id}/dictation?level=...`.
- Làm bài: `POST /api/lessons/{id}/dictation/submit`, `POST /api/lessons/{id}/segments/{segmentIndex}/check`, `POST /api/lessons/{id}/segments/{segmentIndex}/shadowing`.

## Theo Vai Trò Người Dùng

### Admin

- Tạo lesson từ YouTube hoặc transcript thủ công.
- Không nên tự tay chỉnh từng từ bằng logic UI cứng; nên để backend sinh mặc định rồi review lại.

### Moderator

- Là người quyết định blanks nào nên ẩn, blanks nào nên giữ.
- Nên tập trung vào chất lượng học liệu, không nên sửa dữ liệu kỹ thuật thừa.

### User

- Chỉ nhìn thấy lesson theo level và quyền truy cập.
- Không thấy đáp án dictation, nhưng vẫn chấm đúng theo template gốc.

## Senior Backend Architecture Notes

Nếu nhìn dưới góc backend senior architect, tôi sẽ cải thiện như sau:

1. Tách rõ 3 lớp dữ liệu: raw transcript, timed transcript, và exercise template.
2. Không để moderator chỉnh trực tiếp quá nhiều JSON nếu có thể; nên có API chuyên biệt để mark blank trên từng segment/word.
3. Nên version hóa `DictationTemplates` để biết mỗi lần chỉnh là ai sửa, sửa gì, lúc nào.
4. Nên lưu trạng thái review riêng, ví dụ `Draft`, `Reviewed`, `Published`, để admin UI dễ đi theo luồng.
5. Nên có validation để đảm bảo transcript update xong thì template phải regenerate hoặc bị đánh dấu stale.
6. Nên có audit log cho các thao tác `transcript`, `dictation-templates`, `status`.

### Nếu là user, tôi muốn đổi gì

- Muốn thấy rõ bài nào là Dictation, bài nào là Shadowing.
- Muốn biết level nào đang mở transcript, level nào chỉ nghe và gõ.
- Muốn moderator không phải nghĩ quá nhiều về JSON, mà chỉ chọn word/phrase rồi backend tự build template.

## Có Check Lại Transcript Đúng Không?

Có, nhưng theo 2 kiểu khác nhau:

### 1. Dictation

Backend không so toàn bộ transcript của user với toàn bộ bài.
Backend chỉ so các blank đã được tạo trong `DictationTemplates`.

Ví dụ:

- Segment gốc: `waking up later and getting more sleep`
- Moderator blank `more sleep`
- User nhập `more sleep` → đúng
- User nhập `many sleep` → sai

Điều này có nghĩa là dictation kiểm tra đúng phần moderator đã quyết định khuyết, không kiểm tra toàn bộ câu.

### 2. Shadowing / Check Segment

Backend so word-by-word với transcript gốc của segment.

Ví dụ:

- Transcript gốc: `I can just do it on my own`
- User nói ra: `I can do it on my own`
- Backend sẽ trả word results để chỉ ra từ nào đúng, từ nào thiếu.

Đây là kiểu kiểm tra transcript đầy đủ hơn, hợp với shadowing.

## Ví Dụ Theo Level

### Beginner

- `GET /api/lessons/{id}/segments?level=Beginner`
- FE có thể hiện transcript trước khi user làm.
- Hợp với user mới, cần nhìn trước nội dung.

### Intermediate

- `GET /api/lessons/{id}/segments?level=Intermediate`
- Transcript ẩn trước, hiện sau khi submit.
- Hợp với dictation luyện nghe chính xác hơn.

### Advanced / Expert

- `GET /api/lessons/{id}/segments?level=Advanced|Expert`
- Transcript ẩn nhiều hơn, replay bị giới hạn hơn.
- Hợp với shadowing hoặc luyện mức khó cao.

## Ví Dụ Quyết Định Cho Admin

Nếu lesson có câu:

- `waking up later and getting more sleep has had a dramatic impact on life at home`

Moderator có thể làm như sau:

- Dictation: khuyết `more sleep`, `dramatic impact`.
- Shadowing: giữ nguyên toàn câu để user nói lại.

Khi đó:

- Dictation dùng `GET /dictation` và `POST /dictation/submit`.
- Shadowing dùng `GET /segments` và `POST /segments/{segmentIndex}/shadowing`.

## Khuyến Nghị Thực Tế

Nếu mục tiêu của bạn là học nghe nói tốt hơn, tôi sẽ ưu tiên:

1. Shadowing cho level trung bình trở lên.
2. Dictation cho beginner để làm quen từ vựng và chính tả.
3. Admin chỉ cần chỉnh blank ở một số từ quan trọng, không nên khuyết quá nhiều.
4. Publish lesson sau khi dictation preview và shadowing preview đều ổn.

## Tóm Tắt Quyết Định

Nếu bạn đang làm lesson workflow cho admin UI, thứ tự nên là:

`YouTube preview` → `import YouTube` hoặc `paste transcript` → `preview dictation` → `custom blanks` → `publish`.

Nếu dùng transcript từ Tactiq.io, backend hiện phù hợp nhất với nhánh `quick-create` hoặc `PATCH /transcript`, không có API riêng gọi Tactiq.io trực tiếp.

## Ví Dụ Thực Tế

Giả sử bạn có video:

- `https://www.youtube.com/watch?v=pJY0mBWHPw4`

Và có transcript đã lấy từ Tactiq.io như đoạn này:

```text
00:00:00.030
9:00 in the morning and Cassie is still
00:00:02.790
in bed most schools have already started
00:00:04.799
for the day but Cassie school now starts
...
00:02:37.990
Graham Satchel BBC News
```

### Cách làm đúng

#### Bước 1. Xem trước video

Gọi:

- `GET /api/admin/lessons/youtube/preview?url=https://www.youtube.com/watch?v=pJY0mBWHPw4`

Mục đích:

- kiểm tra video hợp lệ không,
- xem có captions không,
- lấy duration, thumbnail, title.

#### Bước 2. Lấy transcript nếu YouTube có caption

Nếu video có caption và bạn muốn lấy luôn từ YouTube:

- `GET /api/admin/lessons/youtube/transcripts?url=https://www.youtube.com/watch?v=pJY0mBWHPw4&preferredLanguage=en&includeText=true`

Nếu transcript từ Tactiq.io đã tốt hơn thì bỏ qua bước này và dùng transcript của Tactiq.

#### Bước 3. Tạo lesson từ transcript

Vì transcript của bạn đã có timestamp, cách hợp lý nhất là tạo lesson mới bằng:

- `POST /api/admin/lessons/quick-create`

Ví dụ body tối thiểu:

```json
{
	"title": "Cassie School Starts Later",
	"description": "Lesson import từ YouTube / Tactiq transcript",
	"transcript": "00:00:00.030\n9:00 in the morning and Cassie is still\n00:00:02.790\nin bed most schools have already started\n00:00:04.799\nfor the day but Cassie school now starts\n00:00:06.870\nlater much later\n00:00:08.189\nit runs from half-past 1:00 in the\n00:00:09.870\nafternoon till 7:00 in the evening",
	"format": "vtt",
	"mediaUrl": "https://www.youtube.com/watch?v=pJY0mBWHPw4",
	"mediaType": "youtube",
	"level": "Intermediate",
	"lessonType": "Dictation",
	"category": "academic",
	"isPremiumOnly": false,
	"displayOrder": 0,
	"tags": "youtube,transcript,teenagers"
}
```

Lưu ý:

- nếu transcript của bạn là kiểu timestamp như trên thì format thực tế có thể cần `srt` hoặc `vtt` đúng chuẩn hơn tùy parser,
- nếu lesson đã tồn tại rồi thì dùng `PATCH /api/admin/lessons/{id}/transcript` thay vì tạo mới.

#### Bước 4. Xem preview cho moderator

Gọi:

- `GET /api/admin/lessons/{id}/dictation-preview`

Tại đây moderator sẽ thấy toàn bộ segment và text gốc để quyết định:

- chỗ nào nên khuyết,
- chỗ nào giữ nguyên,
- segment nào cần sửa.

#### Bước 5. Chỉnh blanks thủ công nếu cần

Gọi:

- `PUT /api/admin/lessons/{id}/dictation-templates`

Use case:

- backend đã auto-generate template,
- nhưng moderator muốn đổi lại blank cho tự nhiên hơn,
- ví dụ giữ lại từ riêng, tên riêng, hoặc cụm từ quan trọng.

#### Bước 6. Publish lesson

Gọi:

- `PATCH /api/admin/lessons/{id}/status`

Body ví dụ:

```json
{
	"status": "published"
}
```

#### Bước 7. User học lesson

Sau khi publish, user dùng:

- `GET /api/lessons/{id}` để xem chi tiết,
- `GET /api/lessons/{id}/segments?level=Intermediate` để lấy segment,
- `GET /api/lessons/{id}/dictation?level=Intermediate` để lấy bài dictation,
- `POST /api/lessons/{id}/dictation/submit` để submit bài.

### Luồng ngắn gọn nhất cho case này

1. `GET /api/admin/lessons/youtube/preview`
2. `POST /api/admin/lessons/quick-create` hoặc `PATCH /api/admin/lessons/{id}/transcript`
3. `GET /api/admin/lessons/{id}/dictation-preview`
4. `PUT /api/admin/lessons/{id}/dictation-templates`
5. `PATCH /api/admin/lessons/{id}/status`
6. User học bằng các API dưới `/api/lessons`