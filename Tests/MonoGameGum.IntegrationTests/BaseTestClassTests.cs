using Gum.Tests.Shared;
using Xunit;

namespace MonoGameGum.IntegrationTests;

[Collection(SharedGame.CollectionName)]
public class BaseTestClassTests
{
    // A test that changes where files resolve from would otherwise change which font or file every
    // later test loads (#6496).
    [Fact]
    public void Dispose_RestoresFileManagerState_WhenATestChangedIt()
    {
        FileManagerStateAssert.DisposeRestores(() => new BaseTestClass());
    }

    // SystemManagers.Initialize sets MissingFileBehavior to ThrowException, so a test that initializes
    // a runtime would otherwise make every later test throw on a missing file (#6556).
    [Fact]
    public void Dispose_RestoresGraphicalUiElementState_WhenATestChangedIt()
    {
        GraphicalUiElementStateAssert.DisposeRestores(() => new BaseTestClass());
    }

    // A project or fallback Standard Element one test leaves on ObjectFinder.Self would otherwise change
    // how later tests resolve elements and font paths (#6517).
    [Fact]
    public void Dispose_ResetsObjectFinder_WhenATestLeftAProjectAndFallbacks()
    {
        ObjectFinderStateAssert.DisposeResets(() => new BaseTestClass());
    }
}
