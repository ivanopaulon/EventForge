using EventForge.Server.Data.Entities.Store;
using EventForge.Server.Data.Entities.Teams;

namespace EventForge.Tests.Entities;

public class StoreEntityImageTests
{
    [Fact]
    public void StoreUser_ImageAliases_ShouldMirrorPhotoFields()
    {
        var documentId = Guid.NewGuid();
        var document = new DocumentReference { Id = documentId, FileName = "operator.png" };
        var entity = new StoreUser
        {
            ImageDocumentId = documentId,
            ImageDocument = document
        };

        Assert.Equal(documentId, entity.PhotoDocumentId);
        Assert.Same(document, entity.PhotoDocument);
    }

    [Fact]
    public void StoreUserGroup_ImageAliases_ShouldMirrorLogoFields()
    {
        var documentId = Guid.NewGuid();
        var document = new DocumentReference { Id = documentId, FileName = "group.png" };
        var entity = new StoreUserGroup
        {
            ImageDocumentId = documentId,
            ImageDocument = document
        };

        Assert.Equal(documentId, entity.LogoDocumentId);
        Assert.Same(document, entity.LogoDocument);
    }

    [Fact]
    public void StorePos_ShouldExposeImageDocument()
    {
        var documentId = Guid.NewGuid();
        var entity = new StorePos
        {
            ImageDocumentId = documentId
        };

        Assert.Equal(documentId, entity.ImageDocumentId);
    }

    [Fact]
    public void StoreUserPrivilege_ShouldExposeImageDocument()
    {
        var documentId = Guid.NewGuid();
        var document = new DocumentReference { Id = documentId, FileName = "privilege.png" };
        var entity = new StoreUserPrivilege
        {
            ImageDocumentId = documentId,
            ImageDocument = document
        };

        Assert.Equal(documentId, entity.ImageDocumentId);
        Assert.Same(document, entity.ImageDocument);
    }
}
