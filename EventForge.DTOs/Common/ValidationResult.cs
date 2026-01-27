using System;
using System.Collections.Generic;

namespace EventForge.DTOs.Common;

/// <summary>
/// Risultato di una validazione business
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indica se la validazione è passata
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Messaggio di errore (se IsValid = false)
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Codice errore standardizzato
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// Errori multipli per campo
    /// </summary>
    public Dictionary<string, List<string>> Errors { get; init; } = new();
    
    /// <summary>
    /// Crea un risultato di validazione positivo
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };
    
    /// <summary>
    /// Crea un risultato di validazione negativo
    /// </summary>
    public static ValidationResult Invalid(string message, string code) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ErrorCode = code
    };
    
    /// <summary>
    /// Crea un risultato per entità non trovata
    /// </summary>
    public static ValidationResult NotFound(string message) => new()
    {
        IsValid = false,
        ErrorMessage = message,
        ErrorCode = "NOT_FOUND"
    };
    
    /// <summary>
    /// Crea un risultato con errori multipli
    /// </summary>
    public static ValidationResult WithErrors(Dictionary<string, List<string>> errors) => new()
    {
        IsValid = false,
        Errors = errors
    };
}
