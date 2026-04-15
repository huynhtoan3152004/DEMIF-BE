namespace Demif.Api.Contracts;

/// <summary>
/// API route constants
/// </summary>
public static class ApiRoutes
{
    public const string Base = "api";

    public static class Auth
    {
        public const string Register = $"{Base}/auth/register";
        public const string VerifyEmail = $"{Base}/auth/verify-email";
        public const string Login = $"{Base}/auth/login";
        public const string GoogleLogin = $"{Base}/auth/google-login";
        public const string RefreshToken = $"{Base}/auth/refresh-token";
        public const string Logout = $"{Base}/auth/logout";
    }

    public static class Profile
    {
        public const string GetMe = $"{Base}/profile/me";
        public const string UpdateMe = $"{Base}/profile/me";
        public const string ChangePassword = $"{Base}/profile/change-password";
    }

    public static class Lessons
    {
        public const string GetAll = $"{Base}/lessons";
        public const string GetById = $"{Base}/lessons/{{id}}";
        public const string GetDictation = $"{Base}/lessons/{{id}}/dictation";
        public const string SubmitDictation = $"{Base}/lessons/{{id}}/dictation/submit";
    }

    public static class SubscriptionPlans
    {
        public const string GetAll = $"{Base}/subscription-plans";
        public const string Subscribe = $"{Base}/subscription-plans/subscribe";
        public const string GetMySubscription = $"{Base}/subscription-plans/my-subscription";
        public const string Cancel = $"{Base}/subscription-plans/cancel";
    }

    public static class Payments
    {
        public const string SePayWebhook = $"{Base}/payments/sepay/webhook";
    }

    public static class AdminLessons
    {
        public const string GetAll = $"{Base}/admin/lessons";
        public const string GetById = $"{Base}/admin/lessons/{{id}}";
        public const string UploadAudio = $"{Base}/admin/lessons/audio/upload";
        public const string Create = $"{Base}/admin/lessons";
        public const string QuickCreate = $"{Base}/admin/lessons/quick-create";
        public const string CreateFromYouTube = $"{Base}/admin/lessons/from-youtube";
        public const string Update = $"{Base}/admin/lessons/{{id}}";
        public const string UpdateTranscript = $"{Base}/admin/lessons/{{id}}/transcript";
        public const string DictationPreview = $"{Base}/admin/lessons/{{id}}/dictation-preview";
        public const string UpdateStatus = $"{Base}/admin/lessons/{{id}}/status";
        public const string Delete = $"{Base}/admin/lessons/{{id}}";
        public const string RegenerateTemplates = $"{Base}/admin/lessons/{{id}}/regenerate-templates";
        public const string YouTubePreview = $"{Base}/admin/lessons/youtube/preview";
        public const string YouTubeTranscripts = $"{Base}/admin/lessons/youtube/transcripts";
    }

    public static class AdminSubscriptionPlans
    {
        public const string GetAll = $"{Base}/admin/subscription-plans";
        public const string GetStats = $"{Base}/admin/subscription-plans/stats";
        public const string Create = $"{Base}/admin/subscription-plans";
        public const string Update = $"{Base}/admin/subscription-plans/{{id}}";
        public const string Delete = $"{Base}/admin/subscription-plans/{{id}}";
    }

    public static class AdminUsers
    {
        public const string GetAll = $"{Base}/admin/users";
        public const string GetById = $"{Base}/admin/users/{{id}}";
        public const string Create = $"{Base}/admin/users";
        public const string Update = $"{Base}/admin/users/{{id}}";
        public const string Delete = $"{Base}/admin/users/{{id}}";
        public const string UpdateStatus = $"{Base}/admin/users/{{id}}/status";
        public const string AssignRole = $"{Base}/admin/users/{{id}}/roles";
        public const string RemoveRole = $"{Base}/admin/users/{{id}}/roles/{{roleName}}";
    }

    public static class Me
    {
        public const string GetProgress = $"{Base}/me/progress";
        public const string GetStreak = $"{Base}/me/streak";
        public const string RecordActivity = $"{Base}/me/activity";
        public const string Vocabulary = $"{Base}/me/vocabulary";
        public const string VocabularyReview = $"{Base}/me/vocabulary/review";
        public const string VocabularyOverview = $"{Base}/me/vocabulary/overview";
        public const string VocabularySuggestions = $"{Base}/me/vocabulary/suggestions";
        public static class Notifications
        {
            public const string GetAll = $"{Base}/me/notifications";
            public const string GetUnreadCount = $"{Base}/me/notifications/unread-count";
            public const string MarkAsRead = $"{Base}/me/notifications/{{id}}/read";
            public const string ReadAll = $"{Base}/me/notifications/read-all";
        }
    }

    public static class AdminUserSubscriptions
    {
        public const string GetAll = $"{Base}/admin/user-subscriptions";
        public const string GetById = $"{Base}/admin/user-subscriptions/{{id}}";
        public const string Extend = $"{Base}/admin/user-subscriptions/{{id}}/extend";
        public const string Cancel = $"{Base}/admin/user-subscriptions/{{id}}/cancel";
    }

    public static class AdminNotifications
    {
        public const string Broadcast = $"{Base}/admin/notifications/broadcast";
    }

    public static class AdminBlogs
    {
        public const string Create = $"{Base}/admin/blogs";
        public const string Update = $"{Base}/admin/blogs/{{id}}";
        public const string Delete = $"{Base}/admin/blogs/{{id}}";
    }
}
