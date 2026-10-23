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

    // A test that turns texture caching on or off would otherwise change whether later tests reload files (#6585).
    [Fact]
    public void Dispose_RestoresCacheTextures_WhenATestChangedIt()
    {
        BaseTestClass testClass = new BaseTestClass();
        bool before = LoaderManager.Self.CacheTextures;

        LoaderManager.Self.CacheTextures = !before;
        testClass.Dispose();

        LoaderManager.Self.CacheTextures.ShouldBe(before);
    }

    // [Custom=Name] callbacks are process-wide, so a name one test registers would otherwise draw a later
    // test's same-named tag with the earlier test's callback (#6580).
    [Fact]
    public void Dispose_RemovesLetterCustomizations_ATestRegistered()
    {
        BaseTestClass testClass = new BaseTestClass();

        global::SkiaGum.Text.Customizations["BaseTestClassTests"] = (index, block) => new global::SkiaGum.LetterCustomization();
        global::SkiaGum.Text.ContextCustomizations["BaseTestClassTests"] = (index, block, prior) => prior;
        testClass.Dispose();

        global::SkiaGum.Text.Customizations.ShouldNotContainKey("BaseTestClassTests");
        global::SkiaGum.Text.ContextCustomizations.ShouldNotContainKey("BaseTestClassTests");
    }
}
