using EventForge.Server.Services.Documents;
using Prym.DTOs.Common;
using Prym.DTOs.Documents;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentStateMachine focusing on state transition logic and business rules.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentStateMachineTests
{
    #region CanTransition Tests

    [Fact]
    public void CanTransition_FromDraftToActive_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Draft, DocumentStatus.Active);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanTransition_FromDraftToCancelled_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Draft, DocumentStatus.Cancelled);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanTransition_FromDraftToArchived_ReturnsFalse()
    {
        // Draft cannot jump directly to Archived
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Draft, DocumentStatus.Archived);

        Assert.False(result);
    }

    [Fact]
    public void CanTransition_FromActiveToDraft_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Active, DocumentStatus.Draft);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanTransition_FromActiveToCancelled_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Active, DocumentStatus.Cancelled);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanTransition_FromActiveToArchived_ReturnsTrue()
    {
        // Active can directly transition to Archived (terminal state)
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Active, DocumentStatus.Archived);

        Assert.True(result);
    }

    [Fact]
    public void CanTransition_FromCancelledToAnyState_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Cancelled, DocumentStatus.Draft));
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Cancelled, DocumentStatus.Active));
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Cancelled, DocumentStatus.Archived));
    }

    #endregion

    #region IsImmutable Tests

    [Fact]
    public void IsImmutable_CancelledStatus_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.IsImmutable(DocumentStatus.Cancelled);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsImmutable_ArchivedStatus_ReturnsTrue()
    {
        // Archived is a terminal state — no further transitions allowed
        var result = DocumentStateMachine.IsImmutable(DocumentStatus.Archived);

        Assert.True(result);
    }

    [Fact]
    public void IsImmutable_DraftStatus_ReturnsFalse()
    {
        // Act
        var result = DocumentStateMachine.IsImmutable(DocumentStatus.Draft);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsImmutable_ActiveStatus_ReturnsFalse()
    {
        // Act
        var result = DocumentStateMachine.IsImmutable(DocumentStatus.Active);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetAvailableTransitions Tests

    [Fact]
    public void GetAvailableTransitions_FromDraft_ReturnsActiveAndCancelled()
    {
        // Act
        var transitions = DocumentStateMachine.GetAvailableTransitions(DocumentStatus.Draft);

        // Assert
        Assert.Equal(2, transitions.Count);
        Assert.Contains(DocumentStatus.Active, transitions);
        Assert.Contains(DocumentStatus.Cancelled, transitions);
    }

    [Fact]
    public void GetAvailableTransitions_FromActive_ReturnsDraftCancelledAndArchived()
    {
        // Act
        var transitions = DocumentStateMachine.GetAvailableTransitions(DocumentStatus.Active);

        // Assert
        Assert.Equal(3, transitions.Count);
        Assert.Contains(DocumentStatus.Draft, transitions);
        Assert.Contains(DocumentStatus.Cancelled, transitions);
        Assert.Contains(DocumentStatus.Archived, transitions);
    }

    [Fact]
    public void GetAvailableTransitions_FromArchived_ReturnsEmpty()
    {
        // Archived is a terminal state
        var transitions = DocumentStateMachine.GetAvailableTransitions(DocumentStatus.Archived);

        Assert.Empty(transitions);
    }

    [Fact]
    public void CanTransition_FromArchivedToAnyState_ReturnsFalse()
    {
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Archived, DocumentStatus.Draft));
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Archived, DocumentStatus.Active));
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Archived, DocumentStatus.Cancelled));
    }

    [Fact]
    public void GetAvailableTransitions_FromCancelled_ReturnsEmpty()
    {
        // Act
        var transitions = DocumentStateMachine.GetAvailableTransitions(DocumentStatus.Cancelled);

        // Assert
        Assert.Empty(transitions);
    }

    #endregion

    #region ValidateTransition Tests - To Active

    [Fact]
    public void ValidateTransition_ToActive_WithMissingBusinessParty_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Draft,
            BusinessPartyId = Guid.Empty,
            DocumentTypeId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Active);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingBusinessParty, result.ErrorCode);
        Assert.Contains("cliente o fornitore", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToActive_WithMissingDocumentType_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Draft,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.Empty
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Active);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingDocumentType, result.ErrorCode);
        Assert.Contains("tipo di documento", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToActive_WithValidData_ReturnsValid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Draft,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Active);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region ValidateTransition Tests - To Archived

    [Fact]
    public void ValidateTransition_ToArchived_WithNoRows_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Active,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "DOC001",
            Rows = new List<DocumentRowDto>(),
            TotalGrossAmount = 100
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.NoRows, result.ErrorCode);
        Assert.Contains("almeno una riga", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToArchived_WithZeroTotal_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Active,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "DOC001",
            Rows = new List<DocumentRowDto> { new DocumentRowDto() },
            TotalGrossAmount = 0
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.ZeroTotal, result.ErrorCode);
        Assert.Contains("maggiore di zero", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToArchived_WithMissingNumber_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Active,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "",
            Rows = new List<DocumentRowDto> { new DocumentRowDto() },
            TotalGrossAmount = 100
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingNumber, result.ErrorCode);
        Assert.Contains("assegna un numero", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToArchived_WithValidData_ReturnsValid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Active,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "DOC001",
            Rows = new List<DocumentRowDto> { new DocumentRowDto() },
            TotalGrossAmount = 100
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region ValidateTransition Tests - To Cancelled

    [Fact]
    public void ValidateTransition_ToCancelled_FromArchived_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Archived,
            BusinessPartyId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Cancelled);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.InvalidTransition, result.ErrorCode);
        Assert.Contains("non consentita", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToCancelled_FromDraft_ReturnsValid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Draft,
            BusinessPartyId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Cancelled);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void ValidateTransition_ToCancelled_FromActive_ReturnsValid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Active,
            BusinessPartyId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Cancelled);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
    }

    #endregion

    #region ValidateTransition Tests - To Draft

    [Fact]
    public void ValidateTransition_ToDraft_FromActive_ReturnsValid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Active,
            BusinessPartyId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Draft);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void ValidateTransition_ToDraft_FromArchived_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Archived,
            BusinessPartyId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Draft);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.InvalidTransition, result.ErrorCode);
    }

    #endregion

    #region GetTransitionConfirmationMessage Tests

    [Fact]
    public void GetTransitionConfirmationMessage_DraftToActive_ReturnsCorrectMessage()
    {
        // Act
        var message = DocumentStateMachine.GetTransitionConfirmationMessage(DocumentStatus.Draft, DocumentStatus.Active);

        // Assert
        Assert.Contains("Attivare il documento", message);
        Assert.Contains("lavorazione", message);
    }

    [Fact]
    public void GetTransitionConfirmationMessage_ActiveToArchived_ReturnsWarning()
    {
        // Act
        var message = DocumentStateMachine.GetTransitionConfirmationMessage(DocumentStatus.Active, DocumentStatus.Archived);

        // Assert
        Assert.Contains("IRREVERSIBILE", message);
        Assert.Contains("immutabile", message);
    }

    [Fact]
    public void GetTransitionConfirmationMessage_ToCancelled_ReturnsWarning()
    {
        // Act
        var message = DocumentStateMachine.GetTransitionConfirmationMessage(DocumentStatus.Active, DocumentStatus.Cancelled);

        // Assert
        Assert.Contains("Annullare", message);
        Assert.Contains("IRREVERSIBILE", message);
    }

    #endregion
}
