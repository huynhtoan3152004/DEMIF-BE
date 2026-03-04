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
        public const string Create = $"{Base}/admin/lessons";
        public const string Update = $"{Base}/admin/lessons/{{id}}";
        public const string Delete = $"{Base}/admin/lessons/{{id}}";
        public const string RegenerateTemplates = $"{Base}/admin/lessons/{{id}}/regenerate-templates";
        public const string YouTubePreview = $"{Base}/admin/lessons/youtube/preview";
        public const string CreateFromYouTube = $"{Base}/admin/lessons/from-youtube";
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
        public const string GetAll = $"{Base}/users";
        public const string GetById = $"{Base}/users/{{id}}";
        public const string Create = $"{Base}/users";
        public const string Update = $"{Base}/users/{{id}}";
        public const string Delete = $"{Base}/users/{{id}}";
        public const string UpdateStatus = $"{Base}/users/{{id}}/status";
        public const string AssignRole = $"{Base}/users/{{id}}/roles";
        public const string RemoveRole = $"{Base}/users/{{id}}/roles/{{roleName}}";
    }
}
