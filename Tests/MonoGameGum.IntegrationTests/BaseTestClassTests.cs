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

    // A project or fallback Standard Element one test leaves on ObjectFinder.Self would otherwise change
    // how later tests resolve elements and font paths (#6517).
    [Fact]
    public void Dispose_ResetsObjectFinder_WhenATestLeftAProjectAndFallbacks()
    {
        ObjectFinderStateAssert.DisposeResets(() => new BaseTestClass());
    }
}
