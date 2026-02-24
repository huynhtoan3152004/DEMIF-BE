# Script tạo sample English lesson bằng POST API
# Chạy khi API đang chạy: dotnet run (trong Demif.Api)
# Lưu ý: cần JWT token admin để gọi endpoint admin

# Đổi URL và Token cho phù hợp
$baseUrl = "http://localhost:5000"
$adminToken = "YOUR_ADMIN_JWT_TOKEN_HERE"

$headers = @{
    "Content-Type"  = "application/json"
    "Authorization" = "Bearer $adminToken"
}

# ========================
# LESSON 1: Free - Beginner
# ========================
$lesson1 = @{
    title            = "Daily Conversation: At the Coffee Shop"
    description      = "Practice everyday English used when ordering at a coffee shop. Learn common phrases and vocabulary."
    lessonType       = 1  # Dictation
    level            = 1  # Beginner
    category         = "daily"
    audioUrl         = "https://storage.demif.com/lessons/coffee-shop.mp3"
    durationSeconds  = 35
    fullTranscript   = "Good morning. Can I have a large coffee please? Sure, would you like cream and sugar? Yes, just a little sugar. That will be three dollars and fifty cents. Here you go. Thank you, have a nice day!"
    isPremiumOnly    = $false
    displayOrder     = 1
    tags             = '["daily", "conversation", "beginner", "ordering"]'
    status           = "published"
} | ConvertTo-Json -Depth 3

Write-Host "Creating Lesson 1: Coffee Shop (Free, Beginner)..." -ForegroundColor Cyan
$response1 = Invoke-RestMethod -Uri "$baseUrl/api/admin/lessons" -Method POST -Headers $headers -Body $lesson1 -ErrorAction SilentlyContinue
Write-Host "Result: $($response1 | ConvertTo-Json)" -ForegroundColor Green

# ========================
# LESSON 2: Free - Intermediate
# ========================
$lesson2 = @{
    title            = "Business English: Team Meeting"
    description      = "Improve your business English by listening to a typical team meeting discussion about project updates."
    lessonType       = 1
    level            = 2  # Intermediate
    category         = "business"
    audioUrl         = "https://storage.demif.com/lessons/team-meeting.mp3"
    durationSeconds  = 55
    fullTranscript   = "Good afternoon everyone. Let's get started with our weekly update. First, I'd like to discuss the progress on the marketing campaign. The team has completed the initial research phase. We found that our target audience responds better to video content. Sarah, could you share the analytics from last week? Absolutely. Our engagement rate increased by fifteen percent. That's excellent progress. Now, let's move on to the next agenda item."
    isPremiumOnly    = $false
    displayOrder     = 2
    tags             = '["business", "meeting", "intermediate"]'
    status           = "published"
} | ConvertTo-Json -Depth 3

Write-Host "`nCreating Lesson 2: Team Meeting (Free, Intermediate)..." -ForegroundColor Cyan
$response2 = Invoke-RestMethod -Uri "$baseUrl/api/admin/lessons" -Method POST -Headers $headers -Body $lesson2 -ErrorAction SilentlyContinue
Write-Host "Result: $($response2 | ConvertTo-Json)" -ForegroundColor Green

# ========================
# LESSON 3: Premium - Advanced
# ========================
$lesson3 = @{
    title            = "TED Talk: The Power of Vulnerability"
    description      = "Challenge yourself with an advanced dictation exercise from a famous TED Talk about human connection and vulnerability."
    lessonType       = 1
    level            = 3  # Advanced
    category         = "academic"
    audioUrl         = "https://storage.demif.com/lessons/ted-vulnerability.mp3"
    durationSeconds  = 75
    fullTranscript   = "So I'll start with this. A couple of years ago, an event planner called me because I was going to do a speaking event. And she called and said, I'm really struggling with how to write about you on the little flyer. And I thought, well what's the struggle? And she said, well I saw you speak and I'm going to call you a researcher, I think. But I'm afraid that if I call you a researcher, no one will come because they'll think you're boring and irrelevant. And I was like, okay. She said, but the thing I liked about your talk is you're a storyteller. So I think what I'll do is just call you a storyteller. And of course the academic, insecure part of me was like, you're going to call me a what?"
    isPremiumOnly    = $true
    displayOrder     = 3
    tags             = '["academic", "ted-talk", "advanced", "premium"]'
    status           = "published"
} | ConvertTo-Json -Depth 3

Write-Host "`nCreating Lesson 3: TED Talk (Premium, Advanced)..." -ForegroundColor Cyan
$response3 = Invoke-RestMethod -Uri "$baseUrl/api/admin/lessons" -Method POST -Headers $headers -Body $lesson3 -ErrorAction SilentlyContinue
Write-Host "Result: $($response3 | ConvertTo-Json)" -ForegroundColor Green

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "Done! 3 lessons created." -ForegroundColor Yellow
Write-Host "- Lesson 1: Free Beginner (coffee shop)" -ForegroundColor White
Write-Host "- Lesson 2: Free Intermediate (business meeting)" -ForegroundColor White
Write-Host "- Lesson 3: Premium Advanced (TED talk)" -ForegroundColor White
Write-Host "========================================" -ForegroundColor Yellow
Write-Host "`nTest dictation endpoint:" -ForegroundColor Cyan
Write-Host "GET $baseUrl/api/lessons/{id}/dictation?level=Beginner" -ForegroundColor White
