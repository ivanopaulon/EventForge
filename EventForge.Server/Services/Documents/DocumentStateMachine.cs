using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;

namespace EventForge.Server.Services.Documents;

/// <summary>
/// State machine per gestire transizioni stato documenti con business rules.
/// Implementa pattern Finite State Machine per garantire integrità.
/// </summary>
public static class DocumentStateMachine
{
    #region Transition Matrix
    
    /// <summary>
    /// Matrice transizioni consentite.
    /// Key = Stato attuale, Value = Lista stati raggiungibili
    /// </summary>
    private static readonly Dictionary<DocumentStatus, List<DocumentStatus>> AllowedTransitions = new()
    {
        // DRAFT → può diventare OPEN o CANCELLED
        { 
            DocumentStatus.Draft, 
            new List<DocumentStatus> 
            { 
                DocumentStatus.Open, 
                DocumentStatus.Cancelled 
            } 
        },
        
        // OPEN → può diventare CLOSED, tornare DRAFT, o essere CANCELLED
        { 
            DocumentStatus.Open, 
            new List<DocumentStatus> 
            { 
                DocumentStatus.Closed, 
                DocumentStatus.Draft,
                DocumentStatus.Cancelled 
            } 
        },
        
        // CLOSED → IMMUTABILE (nessuna transizione)
        { 
            DocumentStatus.Closed, 
            new List<DocumentStatus>() 
        },
        
        // CANCELLED → IMMUTABILE (nessuna transizione)
        { 
            DocumentStatus.Cancelled, 
            new List<DocumentStatus>() 
        }
    };
    
    #endregion
    
    #region Validation Methods
    
    public static bool CanTransition(DocumentStatus from, DocumentStatus to)
    {
        if (!AllowedTransitions.TryGetValue(from, out var allowedStates))
        {
            return false;
        }
        
        return allowedStates.Contains(to);
    }
    
    public static bool IsImmutable(DocumentStatus status)
    {
        return status == DocumentStatus.Closed || status == DocumentStatus.Cancelled;
    }
    
    public static List<DocumentStatus> GetAvailableTransitions(DocumentStatus currentStatus)
    {
        return AllowedTransitions.TryGetValue(currentStatus, out var allowed) 
            ? allowed 
            : new List<DocumentStatus>();
    }
    
    public static StateTransitionValidationResult ValidateTransition(
        DocumentHeaderDto document, 
        DocumentStatus newStatus)
    {
        if (!CanTransition(document.Status, newStatus))
        {
            return StateTransitionValidationResult.Fail(
                $"Transizione non consentita da {document.Status} a {newStatus}",
                StateTransitionErrorCode.InvalidTransition);
        }
        
        return newStatus switch
        {
            DocumentStatus.Open => ValidateTransitionToOpen(document),
            DocumentStatus.Closed => ValidateTransitionToClosed(document),
            DocumentStatus.Cancelled => ValidateTransitionToCancelled(document),
            DocumentStatus.Draft => ValidateTransitionToDraft(document),
            _ => StateTransitionValidationResult.Success()
        };
    }
    
    #endregion
    
    #region Business Rules
    
    private static StateTransitionValidationResult ValidateTransitionToOpen(DocumentHeaderDto document)
    {
        if (document.BusinessPartyId == Guid.Empty)
        {
            return StateTransitionValidationResult.Fail(
                "Impossibile aprire il documento: seleziona prima un cliente o fornitore",
                StateTransitionErrorCode.MissingBusinessParty);
        }
        
        if (document.DocumentTypeId == Guid.Empty)
        {
            return StateTransitionValidationResult.Fail(
                "Impossibile aprire il documento: seleziona un tipo di documento",
                StateTransitionErrorCode.MissingDocumentType);
        }
        
        return StateTransitionValidationResult.Success();
    }
    
    private static StateTransitionValidationResult ValidateTransitionToClosed(DocumentHeaderDto document)
    {
        if (document.Rows == null || !document.Rows.Any())
        {
            return StateTransitionValidationResult.Fail(
                "Impossibile chiudere il documento: deve contenere almeno una riga",
                StateTransitionErrorCode.NoRows);
        }
        
        if (document.TotalGrossAmount <= 0)
        {
            return StateTransitionValidationResult.Fail(
                "Impossibile chiudere il documento: il totale deve essere maggiore di zero",
                StateTransitionErrorCode.ZeroTotal);
        }
        
        if (document.BusinessPartyId == Guid.Empty)
        {
            return StateTransitionValidationResult.Fail(
                "Impossibile chiudere il documento: seleziona un cliente o fornitore",
                StateTransitionErrorCode.MissingBusinessParty);
        }
        
        if (string.IsNullOrWhiteSpace(document.Number))
        {
            return StateTransitionValidationResult.Fail(
                "Impossibile chiudere il documento: assegna un numero al documento",
                StateTransitionErrorCode.MissingNumber);
        }
        
        return StateTransitionValidationResult.Success();
    }
    
    private static StateTransitionValidationResult ValidateTransitionToCancelled(DocumentHeaderDto document)
    {
        if (document.Status == DocumentStatus.Closed)
        {
            return StateTransitionValidationResult.Fail(
                "Impossibile annullare un documento chiuso. Crea una nota di credito.",
                StateTransitionErrorCode.CannotCancelClosed);
        }
        
        return StateTransitionValidationResult.Success();
    }
    
    private static StateTransitionValidationResult ValidateTransitionToDraft(DocumentHeaderDto document)
    {
        if (document.Status != DocumentStatus.Open)
        {
            return StateTransitionValidationResult.Fail(
                "Si può riportare in bozza solo un documento aperto",
                StateTransitionErrorCode.InvalidTransition);
        }
        
        return StateTransitionValidationResult.Success();
    }
    
    #endregion
    
    #region Helper Methods
    
    public static string GetTransitionConfirmationMessage(DocumentStatus from, DocumentStatus to)
    {
        return (from, to) switch
        {
            (DocumentStatus.Draft, DocumentStatus.Open) => 
                "Aprire il documento? Sarà pronto per la lavorazione.",
            
            (DocumentStatus.Open, DocumentStatus.Closed) => 
                "Chiudere il documento? Questa azione è IRREVERSIBILE e il documento diventerà immutabile.",
            
            (DocumentStatus.Open, DocumentStatus.Draft) => 
                "Riportare il documento in bozza? Potrai continuare a modificarlo.",
            
            (_, DocumentStatus.Cancelled) => 
                "Annullare il documento? Questa azione è IRREVERSIBILE.",
            
            _ => $"Confermare la transizione da {from} a {to}?"
        };
    }
    
    #endregion
}

#region DTOs

public class StateTransitionValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public StateTransitionErrorCode? ErrorCode { get; set; }
    
    public static StateTransitionValidationResult Success() => 
        new() { IsValid = true };
    
    public static StateTransitionValidationResult Fail(string message, StateTransitionErrorCode code) => 
        new() 
        { 
            IsValid = false, 
            ErrorMessage = message, 
            ErrorCode = code 
        };
}

public enum StateTransitionErrorCode
{
    InvalidTransition,
    MissingBusinessParty,
    MissingDocumentType,
    NoRows,
    ZeroTotal,
    MissingNumber,
    CannotCancelClosed
}

#endregion
