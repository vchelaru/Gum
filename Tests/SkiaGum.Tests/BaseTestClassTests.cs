using Gum.Tests.Shared;
using RenderingLibrary.Content;
using Shouldly;

namespace SkiaGum.Tests;

public class BaseTestClassTests
{
    // A test that changes where files resolve from would otherwise change which font or file every
    // later test loads (#6502).
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
    // how later tests resolve elements and font paths (#6502).
    [Fact]
    public void Dispose_ResetsObjectFinder_WhenATestLeftAProjectAndFallbacks()
    {
        ObjectFinderStateAssert.DisposeResets(() => new BaseTestClass());
    }

    // A test that swaps in its own loader would otherwise leave every later test loading through it (#6585).
    [Fact]
    public void Dispose_RestoresContentLoader_WhenATestReplacedIt()
    {
        BaseTestClass testClass = new BaseTestClass();
        IContentLoader? before = LoaderManager.Self.ContentLoader;

        LoaderManager.Self.ContentLoader = new ContentLoader();
        testClass.Dispose();

        LoaderManager.Self.ContentLoader.ShouldBeSameAs(before);
    }

    // SystemManagers.Initialize assigns ContentLoader only on its first call in the process. Restoring a
    // null captured before that call would leave every later test with no loader, because no later
    // Initialize assigns it again (#6585).
    [Fact]
    public void Dispose_LeavesAContentLoader_WhenTheTestStartedBeforeAnyInitialize()
    {
        IContentLoader? original = LoaderManager.Self.ContentLoader;
        try
        {
            LoaderManager.Self.ContentLoader = null;
            BaseTestClass testClass = new BaseTestClass();

            testClass.Dispose();

            LoaderManager.Self.ContentLoader.ShouldNotBeNull();
        }
        finally
        {
            LoaderManager.Self.ContentLoader = original;
        }
    }
}
