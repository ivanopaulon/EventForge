using Prym.DTOs.Common;
using Prym.DTOs.Documents;
using EventForge.Server.Services.Documents;

namespace EventForge.Tests.Services.Documents;

/// <summary>
/// Unit tests for DocumentStateMachine — validates allowed transitions and business rules.
/// </summary>
[Trait("Category", "Unit")]
public class DocumentStateMachineTests
{
    #region CanTransition

    [Fact]
    public void CanTransition_ActiveToArchived_ReturnsTrue()
    {
        Assert.True(DocumentStateMachine.CanTransition(DocumentStatus.Active, DocumentStatus.Archived));
    }

    [Fact]
    public void CanTransition_ArchivedToActive_ReturnsTrue()
    {
        Assert.True(DocumentStateMachine.CanTransition(DocumentStatus.Archived, DocumentStatus.Active));
    }

    [Fact]
    public void CanTransition_ActiveToActive_ReturnsFalse()
    {
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Active, DocumentStatus.Active));
    }

    [Fact]
    public void CanTransition_ArchivedToArchived_ReturnsFalse()
    {
        Assert.False(DocumentStateMachine.CanTransition(DocumentStatus.Archived, DocumentStatus.Archived));
    }

    #endregion

    #region GetAvailableTransitions

    [Fact]
    public void GetAvailableTransitions_FromActive_ReturnsArchivedOnly()
    {
        var transitions = DocumentStateMachine.GetAvailableTransitions(DocumentStatus.Active);

        Assert.Single(transitions);
        Assert.Contains(DocumentStatus.Archived, transitions);
    }

    [Fact]
    public void GetAvailableTransitions_FromArchived_ReturnsActiveOnly()
    {
        var transitions = DocumentStateMachine.GetAvailableTransitions(DocumentStatus.Archived);

        Assert.Single(transitions);
        Assert.Contains(DocumentStatus.Active, transitions);
    }

    #endregion

    #region ValidateTransition — Active → Archived

    [Fact]
    public void ValidateTransition_ActiveToArchived_ValidDocument_Succeeds()
    {
        var document = BuildValidDocument(DocumentStatus.Active);

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ValidateTransition_ActiveToArchived_NoRows_Fails_WithNoRowsCode()
    {
        var document = BuildValidDocument(DocumentStatus.Active);
        document.Rows = new List<DocumentRowDto>();

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.NoRows, result.ErrorCode);
    }

    [Fact]
    public void ValidateTransition_ActiveToArchived_ZeroTotal_Fails_WithZeroTotalCode()
    {
        var document = BuildValidDocument(DocumentStatus.Active);
        document.TotalGrossAmount = 0m;

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.ZeroTotal, result.ErrorCode);
    }

    [Fact]
    public void ValidateTransition_ActiveToArchived_MissingBusinessParty_Fails_WithMissingBusinessPartyCode()
    {
        var document = BuildValidDocument(DocumentStatus.Active);
        document.BusinessPartyId = Guid.Empty;

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingBusinessParty, result.ErrorCode);
    }

    [Fact]
    public void ValidateTransition_ActiveToArchived_MissingNumber_Fails_WithMissingNumberCode()
    {
        var document = BuildValidDocument(DocumentStatus.Active);
        document.Number = string.Empty;

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingNumber, result.ErrorCode);
    }

    #endregion

    #region ValidateTransition — Archived → Active

    [Fact]
    public void ValidateTransition_ArchivedToActive_ValidDocument_Succeeds()
    {
        var document = BuildValidDocument(DocumentStatus.Archived);

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Active);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateTransition_ArchivedToActive_MissingBusinessParty_Fails()
    {
        var document = BuildValidDocument(DocumentStatus.Archived);
        document.BusinessPartyId = Guid.Empty;

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Active);

        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingBusinessParty, result.ErrorCode);
    }

    [Fact]
    public void ValidateTransition_ArchivedToActive_MissingDocumentType_Fails()
    {
        var document = BuildValidDocument(DocumentStatus.Archived);
        document.DocumentTypeId = Guid.Empty;

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Active);

        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.MissingDocumentType, result.ErrorCode);
    }

    #endregion

    #region ValidateTransition — Invalid transitions

    [Fact]
    public void ValidateTransition_ActiveToActive_Fails_WithInvalidTransitionCode()
    {
        var document = BuildValidDocument(DocumentStatus.Active);

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Active);

        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.InvalidTransition, result.ErrorCode);
    }

    [Fact]
    public void ValidateTransition_ArchivedToArchived_Fails_WithInvalidTransitionCode()
    {
        var document = BuildValidDocument(DocumentStatus.Archived);

        var result = DocumentStateMachine.ValidateTransition(document, DocumentStatus.Archived);

        Assert.False(result.IsValid);
        Assert.Equal(StateTransitionErrorCode.InvalidTransition, result.ErrorCode);
    }

    #endregion

    #region GetTransitionConfirmationMessage

    [Fact]
    public void GetTransitionConfirmationMessage_ActiveToArchived_ReturnsNonEmptyMessage()
    {
        var message = DocumentStateMachine.GetTransitionConfirmationMessage(
            DocumentStatus.Active, DocumentStatus.Archived);

        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    [Fact]
    public void GetTransitionConfirmationMessage_ArchivedToActive_ReturnsNonEmptyMessage()
    {
        var message = DocumentStateMachine.GetTransitionConfirmationMessage(
            DocumentStatus.Archived, DocumentStatus.Active);

        Assert.False(string.IsNullOrWhiteSpace(message));
    }

    #endregion

    #region Helpers

    private static DocumentHeaderDto BuildValidDocument(DocumentStatus status)
    {
        return new DocumentHeaderDto
        {
            Id = Guid.NewGuid(),
            Status = status,
            BusinessPartyId = Guid.NewGuid(),
            DocumentTypeId = Guid.NewGuid(),
            Number = "DOC-001",
            TotalGrossAmount = 100m,
            Rows =
            [
                new DocumentRowDto
                {
                    Id = Guid.NewGuid(),
                    ProductId = Guid.NewGuid(),
                    Quantity = 1m,
                    UnitPrice = 100m
                }
            ]
        };
    }

    #endregion
}
