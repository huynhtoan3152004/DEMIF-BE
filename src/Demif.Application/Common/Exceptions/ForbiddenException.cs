namespace Demif.Application.Common.Exceptions;

/// <summary>
/// Forbidden Exception - throw khi user không có quyền
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException() : base("Access to this resource is forbidden.")
    {
    }

    public ForbiddenException(string message) : base(message)
    {
    }
}
