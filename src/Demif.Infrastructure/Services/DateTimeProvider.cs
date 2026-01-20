using Demif.Application.Abstractions.Services;

namespace Demif.Infrastructure.Services;

/// <summary>
/// DateTime Provider - cho production dùng thời gian thực
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.UtcNow.Date;
}
