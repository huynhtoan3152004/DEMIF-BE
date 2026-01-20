namespace Demif.Domain.Common;

/// <summary>
/// Marker interface cho Domain Events
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
