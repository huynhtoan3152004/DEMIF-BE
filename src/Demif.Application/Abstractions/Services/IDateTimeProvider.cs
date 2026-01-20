namespace Demif.Application.Abstractions.Services;

/// <summary>
/// DateTime Provider - cho phép mock time trong tests
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Today { get; }
}
