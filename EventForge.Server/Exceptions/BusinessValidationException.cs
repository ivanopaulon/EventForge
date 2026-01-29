namespace EventForge.Server.Exceptions;

/// <summary>
/// Eccezione sollevata quando una validazione business fallisce
/// </summary>
public class BusinessValidationException : EventForgeException
{
    /// <summary>
    /// Codice errore standardizzato
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// Errori di validazione per campo
    /// </summary>
    public Dictionary<string, List<string>>? ValidationErrors { get; }
    
    public BusinessValidationException(string message, string errorCode = "VALIDATION_ERROR")
        : base(message)
    {
        ErrorCode = errorCode;
    }
    
    public BusinessValidationException(
        string message, 
        Dictionary<string, List<string>> errors)
        : base(message)
    {
        ErrorCode = "VALIDATION_ERROR";
        ValidationErrors = errors;
    }
    
    public BusinessValidationException(string field, string error) 
        : base($"Validation failed: {error}")
    {
        ErrorCode = "VALIDATION_ERROR";
        ValidationErrors = new Dictionary<string, List<string>>
        {
            { field, new List<string> { error } }
        };
    }
}
