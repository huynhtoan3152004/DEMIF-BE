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
    
    public static Error NotFound(string message) => 
        new("NotFound", message);
    
    public static Error Validation(string message) => 
        new("Validation", message);
    
    public static Error Conflict(string message) => 
        new("Conflict", message);
    
    public static Error Unauthorized(string message = "Unauthorized access.") => 
        new("Unauthorized", message);
    
    public static Error Forbidden(string message = "Access forbidden.") => 
        new("Forbidden", message);
    
    public static Error Internal(string message) => 
        new("Internal", message);
}

