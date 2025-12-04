using EventForge.DTOs.Common;
using System.Net;
using System.Text.Json;

namespace EventForge.Client.Services.Store;

/// <summary>
/// Shared helper utilities for Store service implementations.
/// </summary>
public static class StoreServiceHelper
{
    /// <summary>
    /// Extracts a user-friendly error message from the HTTP response, with special handling for tenant-related errors.
    /// </summary>
    /// <param name="response">The HTTP response message.</param>
    /// <param name="entityType">The type of entity (e.g., "operatore", "gruppo", "punto cassa") for error messages.</param>
    /// <param name="logger">Logger for error tracking.</param>
    /// <returns>A user-friendly error message in Italian.</returns>
    public static async Task<string> GetErrorMessageAsync(HttpResponseMessage response, string entityType, ILogger logger)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            
            // Check for tenant-related errors
            if (content.Contains("Tenant context is required", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("TenantId", StringComparison.OrdinalIgnoreCase))
            {
                return "Impossibile completare l'operazione: contesto tenant mancante. Effettua nuovamente l'accesso.";
            }

            // Try to parse ProblemDetails
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var problemDetails = JsonSerializer.Deserialize<ProblemDetailsDto>(content, options);
                if (!string.IsNullOrEmpty(problemDetails?.Detail))
                {
                    return problemDetails.Detail;
                }
                if (!string.IsNullOrEmpty(problemDetails?.Title))
                {
                    return problemDetails.Title;
                }
            }
            catch
            {
                // Not a ProblemDetails response
            }

            // Return generic message based on status code
            return response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "Dati non validi. Verifica i campi inseriti.",
                HttpStatusCode.Unauthorized => "Non autorizzato. Effettua nuovamente l'accesso.",
                HttpStatusCode.Forbidden => "Non hai i permessi necessari per questa operazione.",
                HttpStatusCode.NotFound => $"{char.ToUpper(entityType[0]) + entityType.Substring(1)} non trovato.",
                HttpStatusCode.Conflict => $"{char.ToUpper(entityType[0]) + entityType.Substring(1)} esiste già o c'è un conflitto con i dati esistenti.",
                _ => $"Errore: {response.ReasonPhrase}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting error message from response");
            return "Si è verificato un errore durante l'operazione.";
        }
    }
}
