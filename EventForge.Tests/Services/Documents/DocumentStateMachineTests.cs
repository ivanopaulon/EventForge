using EventForge.DTOs.Common;
using EventForge.DTOs.Documents;
using EventForge.Server.Services.Documents;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentStateMachine focusing on state transition logic and business rules.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentStateMachineTests
{
    #region CanTransition Tests

    [Fact]
    public void CanTransition_FromDraftToOpen_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Draft, DocumentStatus.Open);

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
    public void CanTransition_FromDraftToClosed_ReturnsFalse()
    {
        // Act
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Draft, DocumentStatus.Closed);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanTransition_FromOpenToClosed_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Open, DocumentStatus.Closed);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanTransition_FromOpenToDraft_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Open, DocumentStatus.Draft);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanTransition_FromOpenToCancelled_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.CanTransition(DocumentStatus.Open, DocumentStatus.Cancelled);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanTransition_FromClosedToAnyState_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Closed, DocumentStatus.Draft));
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Closed, DocumentStatus.Open));
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Closed, DocumentStatus.Cancelled));
    }

    [Fact]
    public void CanTransition_FromCancelledToAnyState_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Cancelled, DocumentStatus.Draft));
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Cancelled, DocumentStatus.Open));
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Cancelled, DocumentStatus.Closed));
    }

    #endregion

    #region IsImmutable Tests

    [Fact]
    public void IsImmutable_ClosedStatus_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.IsImmutable(DocumentStatus.Closed);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsImmutable_CancelledStatus_ReturnsTrue()
    {
        // Act
        var result = DocumentStateMachine.IsImmutable(DocumentStatus.Cancelled);

        // Assert
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
    public void IsImmutable_OpenStatus_ReturnsFalse()
    {
        // Act
        var result = DocumentStateMachine.IsImmutable(DocumentStatus.Open);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetAvailableTransitions Tests

    [Fact]
    public void GetAvailableTransitions_FromDraft_ReturnsOpenAndCancelled()
    {
        // Act
        var transitions = DocumentStateMachine.GetAvailableTransitions(DocumentStatus.Draft);

        // Assert
        Assert.Equal(2, transitions.Count);
        Assert.Contains(DocumentStatus.Open, transitions);
        Assert.Contains(DocumentStatus.Cancelled, transitions);
    }

    [Fact]
    public void GetAvailableTransitions_FromOpen_ReturnsClosedDraftAndCancelled()
    {
        // Act
        var transitions = DocumentStateMachine.GetAvailableTransitions(DocumentStatus.Open);

        // Assert
        Assert.Equal(3, transitions.Count);
        Assert.Contains(DocumentStatus.Closed, transitions);
        Assert.Contains(DocumentStatus.Draft, transitions);
        Assert.Contains(DocumentStatus.Cancelled, transitions);
    }

    [Fact]
    public void GetAvailableTransitions_FromClosed_ReturnsEmpty()
    {
        // Act
        var transitions = DocumentStateMachine.GetAvailableTransitions(DocumentStatus.Closed);

        // Assert
        Assert.Empty(transitions);
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

    #region ValidateTransition Tests - To Open

    [Fact]
    public void ValidateTransition_ToOpen_WithMissingBusinessParty_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Draft,
            BusinessPartyId = Guid.Empty,
            DocumentTypeId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Open);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingBusinessParty, result.ErrorCode);
        Assert.Contains("cliente o fornitore", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToOpen_WithMissingDocumentType_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Draft,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.Empty
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Open);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingDocumentType, result.ErrorCode);
        Assert.Contains("tipo di documento", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToOpen_WithValidData_ReturnsValid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Draft,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Open);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region ValidateTransition Tests - To Closed

    [Fact]
    public void ValidateTransition_ToClosed_WithNoRows_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Open,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "DOC001",
            Rows = new List<DocumentRowDto>(),
            TotalGrossAmount = 100
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Closed);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.NoRows, result.ErrorCode);
        Assert.Contains("almeno una riga", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToClosed_WithZeroTotal_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Open,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "DOC001",
            Rows = new List<DocumentRowDto> { new DocumentRowDto() },
            TotalGrossAmount = 0
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Closed);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.ZeroTotal, result.ErrorCode);
        Assert.Contains("maggiore di zero", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToClosed_WithMissingNumber_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Open,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "",
            Rows = new List<DocumentRowDto> { new DocumentRowDto() },
            TotalGrossAmount = 100
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Closed);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingNumber, result.ErrorCode);
        Assert.Contains("assegna un numero", result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ToClosed_WithValidData_ReturnsValid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Open,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "DOC001",
            Rows = new List<DocumentRowDto> { new DocumentRowDto() },
            TotalGrossAmount = 100
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Closed);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    #endregion

    #region ValidateTransition Tests - To Cancelled

    [Fact]
    public void ValidateTransition_ToCancelled_FromClosed_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Closed,
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
    public void ValidateTransition_ToCancelled_FromOpen_ReturnsValid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Open,
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
    public void ValidateTransition_ToDraft_FromOpen_ReturnsValid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Open,
            BusinessPartyId = Guid.NewGuid()
        };

        // Act
        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Draft);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void ValidateTransition_ToDraft_FromClosed_ReturnsInvalid()
    {
        // Arrange
        var document = new DocumentHeaderDto
        {
            Status = DocumentStatus.Closed,
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
    public void GetTransitionConfirmationMessage_DraftToOpen_ReturnsCorrectMessage()
    {
        // Act
        var message = DocumentStateMachine.GetTransitionConfirmationMessage(DocumentStatus.Draft, DocumentStatus.Open);

        // Assert
        Assert.Contains("Aprire il documento", message);
        Assert.Contains("lavorazione", message);
    }

    [Fact]
    public void GetTransitionConfirmationMessage_OpenToClosed_ReturnsWarning()
    {
        // Act
        var message = DocumentStateMachine.GetTransitionConfirmationMessage(DocumentStatus.Open, DocumentStatus.Closed);

        // Assert
        Assert.Contains("IRREVERSIBILE", message);
        Assert.Contains("immutabile", message);
    }

    [Fact]
    public void GetTransitionConfirmationMessage_ToCancelled_ReturnsWarning()
    {
        // Act
        var message = DocumentStateMachine.GetTransitionConfirmationMessage(DocumentStatus.Open, DocumentStatus.Cancelled);

        // Assert
        Assert.Contains("Annullare", message);
        Assert.Contains("IRREVERSIBILE", message);
    }

    #endregion
}
