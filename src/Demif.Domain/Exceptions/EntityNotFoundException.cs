namespace Demif.Domain.Exceptions;

/// <summary>
/// Exception khi không tìm thấy entity
/// </summary>
public class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object id)
        : base($"{entityName} with id '{id}' was not found.")
    {
    }
}
