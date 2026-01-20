namespace Demif.Api.Contracts;

/// <summary>
/// Định nghĩa các route API
/// </summary>
public static class ApiRoutes
{
    public const string Base = "api";

    public static class Auth
    {
        public const string Login = $"{Base}/auth/login";
        public const string Register = $"{Base}/auth/register";
        public const string Refresh = $"{Base}/auth/refresh";
        public const string Logout = $"{Base}/auth/logout";
    }

    public static class Lessons
    {
        public const string GetAll = $"{Base}/lessons";
        public const string GetById = $"{Base}/lessons/{{id}}";
    }

    public static class Exercises
    {
        public const string SubmitDictation = $"{Base}/exercises/dictation/submit";
        public const string SubmitShadowing = $"{Base}/exercises/shadowing/submit";
    }
}
