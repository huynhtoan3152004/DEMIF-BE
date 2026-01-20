namespace Demif.Application.Common.Models;

/// <summary>
/// Error object cho Result pattern
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    
    // Common errors
    public static Error NotFound(string entityName, object id) => 
        new($"{entityName}.NotFound", $"{entityName} with id '{id}' was not found.");
    
    public static Error Validation(string message) => 
        new("Validation.Error", message);
    
    public static Error Conflict(string message) => 
        new("Conflict.Error", message);
    
    public static Error Unauthorized(string message = "Unauthorized access.") => 
        new("Auth.Unauthorized", message);
    
    public static Error Forbidden(string message = "Access forbidden.") => 
        new("Auth.Forbidden", message);
}
