using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Gum.GueDeriving;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using ToolsUtilities;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class FontServiceTests : BaseTestClass
{
    private readonly Mock<IRuntimeFontService> _mockFontService;

    // Minimal valid .fnt header, enough for BitmapFont to construct a distinct instance from a
    // null texture. Used to make the font actually change so the assignment's layout branch fires.
    private const string FntPattern =
        "info face=\"Arial\" size=-18 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
        "common lineHeight=21 base=17 scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4\r\n" +
        "chars count=223\r\n";

    public FontServiceTests()
    {
        _mockFontService = new Mock<IRuntimeFontService>();
        _mockFontService.Setup(x => x.AbsoluteFontCacheFolder).Returns("C:/FontCache/");
    }

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.FontService = null;
        base.Dispose();
    }

    #region CreateFontIfNecessary

    [Fact]
    public void CreateFontIfNecessary_ShouldBeCalledWithCorrectProperties_WhenBbCodeFontSizeSet()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 12;

        // Act — the open tag pushes FontSize=24, the close tag pops back to 12
        textRuntime.Text = "[FontSize=24]hello[/FontSize]";

        // Assert — at least one call should have the overridden font size
        capturedCalls.ShouldContain(bmfc => bmfc.FontSize == 24 && bmfc.FontName == "Arial");
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldBeCalledWithCorrectProperties_WhenBbCodeFontNameSet()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;

        // Act
        textRuntime.Text = "[Font=Courier]hello[/Font]";

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.FontName == "Courier" && bmfc.FontSize == 18);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldBeCalledWithBoldAndItalic_WhenBbCodeSetsThose()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;

        // Act
        textRuntime.Text = "[IsBold=True][IsItalic=True]hello[/IsItalic][/IsBold]";

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.IsBold && bmfc.IsItalic);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldNotBeCalled_WhenFontServiceIsNull()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = null;

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;

        // Act — should not throw
        textRuntime.Text = "[FontSize=24]hello[/FontSize]";

        // Assert — no exception means success; verify mock was never touched
        _mockFontService.Verify(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()), Times.Never);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldUseAbsoluteFontCacheFolder_ForPathResolution()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        _mockFontService.Setup(x => x.AbsoluteFontCacheFolder).Returns("C:/TestFontCache/");

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.FontSize = 18;

        // Act
        textRuntime.Text = "[FontSize=24]hello[/FontSize]";

        // Assert — AbsoluteFontCacheFolder was accessed for path resolution
        _mockFontService.Verify(x => x.AbsoluteFontCacheFolder, Times.AtLeastOnce);
    }

    #endregion

    #region UpdateToFontValues

    [Fact]
    public void CreateFontIfNecessary_ShouldBeCalledFromUpdateToFontValues_WhenFontDoesNotExist()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";

        // Act — setting FontSize triggers UpdateToFontValues
        textRuntime.FontSize = 36;

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.FontSize == 36 && bmfc.FontName == "Arial");
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldNotBeCalledFromUpdateToFontValues_WhenFontServiceIsNull()
    {
        // Arrange
        CustomSetPropertyOnRenderable.FontService = null;

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";

        // Act — should not throw
        textRuntime.FontSize = 36;

        // Assert — no exception means success; verify mock was never touched
        _mockFontService.Verify(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()), Times.Never);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldPassBoldAndItalicFromUpdateToFontValues()
    {
        // Arrange — use FontSize 24 to avoid the stubbed Arial-18 embedded resources
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.IsBold = true;
        textRuntime.IsItalic = true;

        // Act — setting FontSize triggers UpdateToFontValues with the current bold/italic values
        textRuntime.FontSize = 24;

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.IsBold && bmfc.IsItalic && bmfc.FontSize == 24);
    }

    [Fact]
    public void CreateFontIfNecessary_ShouldPassOutlineAndSmoothing_FromUpdateToFontValues()
    {
        // Arrange — use FontSize 24 to avoid the stubbed Arial-18 embedded resources
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        List<BmfcSave> capturedCalls = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => capturedCalls.Add(bmfc));

        TextRuntime textRuntime = new();
        textRuntime.Font = "Arial";
        textRuntime.OutlineThickness = 2;
        textRuntime.UseFontSmoothing = false;

        // Act — setting FontSize triggers UpdateToFontValues with the current outline/smoothing values
        textRuntime.FontSize = 24;

        // Assert
        capturedCalls.ShouldContain(bmfc => bmfc.OutlineThickness == 2 && bmfc.UseSmoothing == false);
    }

    #endregion

    #region Layout Suspension / Font Batching

    // These tests document the runtime behavior described in issue #2694:
    // when font properties are changed in batches, the existing layout-suspension
    // machinery can coalesce the per-property UpdateToFontValues calls into a
    // single deferred font load.

    // Wires the mock and returns the capture list. Call AFTER constructing the
    // TextRuntime so the constructor's own font load isn't counted against the
    // behavior under test.
    private List<BmfcSave> StartCapturingFontCalls()
    {
        var captured = new List<BmfcSave>();
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(bmfc => captured.Add(bmfc));
        return captured;
    }

    [Fact]
    public void DirectSetter_ShouldCallFontGenerationOncePerProperty_WhenNotSuspended()
    {
        // Baseline: with no suspension, each font property setter independently
        // triggers a font generation. This is the wasted-work problem #2694 calls out.
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        textRuntime.Font = "Consolas";   // setter #1 -> generation #1
        textRuntime.FontSize = 24;       // setter #2 -> generation #2

        capturedCalls.Count.ShouldBe(2);
    }

    [Fact]
    public void DirectSetter_ShouldDeferFontGeneration_WhenIsAllLayoutSuspendedIsTrue()
    {
        // Setting properties while IsAllLayoutSuspended is true should NOT call
        // the font service. The element's IsFontDirty flag is set instead.
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        try
        {
            GraphicalUiElement.IsAllLayoutSuspended = true;
            textRuntime.Font = "Consolas";
            textRuntime.FontSize = 24;
            textRuntime.IsBold = true;

            capturedCalls.Count.ShouldBe(0);
            textRuntime.IsFontDirty.ShouldBeTrue();
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
    }

    [Fact]
    public void UpdateFontRecursive_ShouldRunSingleFontGeneration_AfterGlobalSuspendBatch()
    {
        // The pattern WireframeObjectManager uses: suspend globally, set many
        // properties, lift the global flag, then call UpdateFontRecursive to do
        // the single deferred font load with all properties at their final values.
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        textRuntime.Font = "Consolas";
        textRuntime.FontSize = 24;
        textRuntime.IsBold = true;
        GraphicalUiElement.IsAllLayoutSuspended = false;

        textRuntime.UpdateFontRecursive();

        capturedCalls.Count.ShouldBe(1);
        capturedCalls[0].FontName.ShouldBe("Consolas");
        capturedCalls[0].FontSize.ShouldBe(24);
        capturedCalls[0].IsBold.ShouldBeTrue();
        textRuntime.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void DirectSetter_ShouldDeferFontGeneration_WhenInstanceSuspendLayoutIsCalled()
    {
        // The direct-setter path (GraphicalUiElement.UpdateToFontValues) defers
        // for BOTH global and per-instance suspension. This is the easy case.
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        textRuntime.SuspendLayout();
        textRuntime.Font = "Consolas";
        textRuntime.FontSize = 24;

        capturedCalls.Count.ShouldBe(0);
        textRuntime.IsFontDirty.ShouldBeTrue();
    }

    [Fact]
    public void ResumeLayoutRecursive_ShouldFlushDeferredFontLoad_AfterInstanceSuspend()
    {
        // ResumeLayout(recursive: true) goes through ResumeLayoutUpdateIfDirtyRecursive,
        // which clears mIsLayoutSuspended and then calls UpdateFontRecursive.
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        textRuntime.SuspendLayout();
        textRuntime.Font = "Consolas";
        textRuntime.FontSize = 24;

        textRuntime.ResumeLayout(recursive: true);

        capturedCalls.Count.ShouldBe(1);
        capturedCalls[0].FontName.ShouldBe("Consolas");
        capturedCalls[0].FontSize.ShouldBe(24);
        textRuntime.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void ResumeLayoutNonRecursive_ShouldFlushDeferredFontLoad_AfterInstanceSuspend()
    {
        // ResumeLayout(recursive: false) takes a different path: it directly checks
        // isFontDirty and calls UpdateToFontValues on this element only.
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        textRuntime.SuspendLayout();
        textRuntime.Font = "Consolas";
        textRuntime.FontSize = 24;

        textRuntime.ResumeLayout();

        capturedCalls.Count.ShouldBe(1);
        textRuntime.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void StringSetPath_ShouldDeferFontGeneration_WhenIsAllLayoutSuspendedIsTrue()
    {
        // The string-set path (SetProperty -> CustomSetPropertyOnRenderable.UpdateToFontValues)
        // defers for the global flag. This is the path ApplyState uses.
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        try
        {
            GraphicalUiElement.IsAllLayoutSuspended = true;
            textRuntime.SetProperty("Font", "Consolas");
            textRuntime.SetProperty("FontSize", 24);

            capturedCalls.Count.ShouldBe(0);
            textRuntime.IsFontDirty.ShouldBeTrue();
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
    }

    [Fact]
    public void StringSetPath_ShouldDeferFontGeneration_WhenOnlyInstanceLayoutSuspended()
    {
        // Was pinned as a "KNOWN GAP" (#4567): the string-set path used to defer only for
        // IsAllLayoutSuspended, not the per-instance suspension flag, so ApplyState (which uses
        // SetProperty under instance-level SuspendLayout, not the global flag) loaded fonts
        // immediately. CustomSetPropertyOnRenderable.UpdateToFontValues now defers for both flags,
        // matching the direct-property-setter path.
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        textRuntime.SuspendLayout();
        textRuntime.SetProperty("Font", "Consolas");
        textRuntime.SetProperty("FontSize", 24);

        capturedCalls.Count.ShouldBe(0);
        textRuntime.IsFontDirty.ShouldBeTrue();

        textRuntime.ResumeLayout(recursive: true);

        capturedCalls.Count.ShouldBe(1);
        capturedCalls[0].FontName.ShouldBe("Consolas");
        capturedCalls[0].FontSize.ShouldBe(24);
        textRuntime.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void UpdateLayout_ShouldFlushDeferredFontLoad_AfterGlobalSuspendBatch()
    {
        // Repro for #2999. Fonts set while IsAllLayoutSuspended is true are deferred
        // (IsFontDirty). Users — and the font-performance docs — resume by calling
        // UpdateLayout(), NOT UpdateFontRecursive(). UpdateLayout() must therefore
        // realize the deferred font; otherwise text renders in the renderer's fallback
        // font until something else (e.g. a hover re-applying state) loads it.
        TextRuntime textRuntime = new();
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        textRuntime.SetProperty("Font", "Consolas");
        textRuntime.SetProperty("FontSize", 24);
        GraphicalUiElement.IsAllLayoutSuspended = false;

        textRuntime.UpdateLayout();

        capturedCalls.Count.ShouldBe(1);
        textRuntime.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void UpdateLayout_ShouldFlushDeferredChildFont_WhenCalledOnParent()
    {
        // Faithful to #2999's shape: layout suspended at a high level, a child Text
        // populated under suspension (font deferred), then a bare UpdateLayout() on the
        // PARENT must realize the child's font via the recursive layout pass — the user's
        // repro resumes with ItemList.Visual.UpdateLayout(), not UpdateFontRecursive().
        ContainerRuntime parent = new();
        TextRuntime childText = new();
        parent.AddChild(childText);
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        childText.SetProperty("Font", "Consolas");
        childText.SetProperty("FontSize", 24);
        GraphicalUiElement.IsAllLayoutSuspended = false;

        parent.UpdateLayout();

        capturedCalls.Count.ShouldBe(1);
        childText.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void UpdateLayout_ShouldNotLoadFont_WhenNothingIsDirty()
    {
        // Hot-path guarantee: when no font is deferred (isFontDirty false), UpdateLayout must do
        // ZERO font work. This is the steady-state case that runs on every resize/move/frame.
        TextRuntime textRuntime = new();
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();
        textRuntime.SetProperty("Font", "Consolas");   // not suspended -> loads now, clears dirty
        textRuntime.SetProperty("FontSize", 24);
        capturedCalls.Clear();                          // ignore the initial (legitimate) loads

        textRuntime.UpdateLayout();
        textRuntime.UpdateLayout();
        textRuntime.UpdateLayout();

        capturedCalls.Count.ShouldBe(0);
        textRuntime.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void UpdateLayout_ShouldNotFlushFont_WhileStillSuspended()
    {
        // UpdateLayout called while IsAllLayoutSuspended is true must early-out (MakeDirty) and
        // keep the font deferred — it must not load mid-suspension and defeat the batching.
        TextRuntime textRuntime = new();
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        try
        {
            GraphicalUiElement.IsAllLayoutSuspended = true;
            textRuntime.SetProperty("Font", "Consolas");
            textRuntime.SetProperty("FontSize", 24);

            textRuntime.UpdateLayout();   // still suspended

            capturedCalls.Count.ShouldBe(0);
            textRuntime.IsFontDirty.ShouldBeTrue();
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
    }

    [Fact]
    public void UpdateLayout_ShouldNotFlushDeferredChildFont_WhenChildrenAreNotUpdated()
    {
        // Scoping pin: a shallow UpdateLayout (updateChildren: false) visits only this node, so a
        // dirty CHILD stays deferred. The flush adds no traversal beyond what the layout requests.
        ContainerRuntime parent = new();
        TextRuntime childText = new();
        parent.AddChild(childText);
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        childText.SetProperty("Font", "Consolas");
        childText.SetProperty("FontSize", 24);
        GraphicalUiElement.IsAllLayoutSuspended = false;

        parent.UpdateLayout(updateParent: false, updateChildren: false);

        capturedCalls.Count.ShouldBe(0);
        childText.IsFontDirty.ShouldBeTrue();
    }

    [Fact]
    public void UpdateLayout_ShouldFlushDeferredGrandchildFont_WhenCalledOnRoot()
    {
        // Deep-recursion pin: a dirty grandchild is realized by a root UpdateLayout.
        ContainerRuntime root = new();
        ContainerRuntime middle = new();
        TextRuntime grandchildText = new();
        root.AddChild(middle);
        middle.AddChild(grandchildText);
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        grandchildText.SetProperty("Font", "Consolas");
        grandchildText.SetProperty("FontSize", 24);
        GraphicalUiElement.IsAllLayoutSuspended = false;

        root.UpdateLayout();

        capturedCalls.Count.ShouldBe(1);
        grandchildText.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void UpdateLayout_ShouldFlushOnlyDirtyChild_LeavingCleanSiblingUntouched()
    {
        // Only nodes that actually deferred a font flush; a clean sibling does no font work, and
        // the dirty one is realized exactly once.
        ContainerRuntime parent = new();
        TextRuntime dirtyChild = new();
        TextRuntime cleanChild = new();
        parent.AddChild(dirtyChild);
        parent.AddChild(cleanChild);
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();
        cleanChild.SetProperty("Font", "Arial");   // not suspended -> loads now, stays clean
        cleanChild.SetProperty("FontSize", 18);
        capturedCalls.Clear();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        dirtyChild.SetProperty("Font", "Consolas");
        dirtyChild.SetProperty("FontSize", 24);
        GraphicalUiElement.IsAllLayoutSuspended = false;

        parent.UpdateLayout();

        capturedCalls.Count.ShouldBe(1);
        dirtyChild.IsFontDirty.ShouldBeFalse();
        cleanChild.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void DeferredFontFlush_ShouldSuppressTheFontAssignmentLayout_DuringTheLoad()
    {
        // No-re-entrancy pin. Realizing a font on RelativeToChildren text normally triggers the
        // font assignment's own UpdateLayout (the RelativeToChildren branch in
        // CustomSetPropertyOnRenderable); during the #2999 flush that must be suppressed so the
        // in-progress layout pass isn't re-entered. We capture SuppressLayoutFromFontChange at the
        // moment of the load: it must be true during the flush and false for a normal load. (We
        // assert it here rather than via a layout-count delta because the mock harness resolves to
        // the existing DefaultBitmapFont, so the assignment's layout branch never fires to observe.)
        TextRuntime textRuntime = new();
        List<bool> suppressedAtLoadTime = new();
        _mockFontService.Setup(x => x.CreateFontIfNecessary(It.IsAny<BmfcSave>()))
            .Callback<BmfcSave>(_ => suppressedAtLoadTime.Add(GraphicalUiElement.SuppressLayoutFromFontChange));
        CustomSetPropertyOnRenderable.FontService = _mockFontService.Object;

        // Normal (non-suspended) load: the flag must be false.
        textRuntime.SetProperty("Font", "Consolas");
        textRuntime.SetProperty("FontSize", 18);
        suppressedAtLoadTime.ShouldNotBeEmpty();
        suppressedAtLoadTime.ShouldAllBe(suppressed => suppressed == false);
        suppressedAtLoadTime.Clear();

        // Deferred flush via UpdateLayout: the flag must be true while the font loads.
        GraphicalUiElement.IsAllLayoutSuspended = true;
        textRuntime.SetProperty("FontSize", 24);
        GraphicalUiElement.IsAllLayoutSuspended = false;
        textRuntime.UpdateLayout();

        suppressedAtLoadTime.ShouldNotBeEmpty();
        suppressedAtLoadTime.ShouldAllBe(suppressed => suppressed == true);
    }

    [Fact]
    public void DeferredFontFlush_ShouldNotReenterLayout_WhenTheFontActuallyChanges()
    {
        // Behavioral suppression pin using a REAL, distinct BitmapFont (registered under the cache
        // filename the properties resolve to). Because the resolved font differs from the element's
        // current BitmapFont, the assignment's RelativeToChildren UpdateLayout branch actually fires
        // — unlike the mock-default case, which resolves back to DefaultBitmapFont. The flush must
        // suppress that re-entrant layout: a standalone element runs exactly one layout pass.
        // (Without the suppression the font assignment re-enters UpdateLayout and the delta is 2.)
        BitmapFont distinctFont = new BitmapFont((Texture2D)null!, FntPattern);
        LoaderManager loaderManager = LoaderManager.Self;
        string fileName = FileManager.Standardize("FontCache\\Font24SomeFont.fnt", preserveCase: true, makeAbsolute: true);
        loaderManager.AddDisposable(fileName, distinctFont);

        TextRuntime textRuntime = new();   // RelativeToChildren default

        GraphicalUiElement.IsAllLayoutSuspended = true;
        textRuntime.SetProperty("Font", "SomeFont");
        textRuntime.SetProperty("FontSize", 24);
        GraphicalUiElement.IsAllLayoutSuspended = false;

        int countBefore = GraphicalUiElement.UpdateLayoutCallCount;
        textRuntime.UpdateLayout();
        int delta = GraphicalUiElement.UpdateLayoutCallCount - countBefore;

        // The distinct font was actually assigned, proving the assignment branch (and thus the
        // suppression site) was reached — this test genuinely exercises the suppression.
        textRuntime.Typeface.ShouldBe(distinctFont);
        textRuntime.IsFontDirty.ShouldBeFalse();
        delta.ShouldBe(1);
    }

    [Fact]
    public void UpdateLayout_ShouldFlushDeferredFontLoad_WhenFontSetViaDirectSetter()
    {
        // The font-performance docs use direct setters then UpdateLayout():
        //   textRuntime.Font = ...; textRuntime.FontSize = ...; textRuntime.UpdateLayout();
        // That path defers via the instance UpdateToFontValues (not the string SetProperty path),
        // so it must be flushed by UpdateLayout too.
        TextRuntime textRuntime = new();
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        textRuntime.Font = "Consolas";
        textRuntime.FontSize = 24;
        GraphicalUiElement.IsAllLayoutSuspended = false;

        textRuntime.UpdateLayout();

        capturedCalls.Count.ShouldBe(1);
        textRuntime.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void UpdateLayout_OnStackedChild_ShouldFlushItsDeferredFont_ViaParentReprocess()
    {
        // The #2999 topology is a stack. A child of a stacking parent delegates its UpdateLayout up
        // to the parent (GetIfShouldCallUpdateOnParent), which reprocesses it as a child. The
        // deferred-font flush must still happen through that reprocess.
        ContainerRuntime parent = new();
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        TextRuntime childText = new();
        parent.AddChild(childText);
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        childText.SetProperty("Font", "Consolas");
        childText.SetProperty("FontSize", 24);
        GraphicalUiElement.IsAllLayoutSuspended = false;

        childText.UpdateLayout();   // delegates to the stacking parent, which reprocesses this child

        capturedCalls.Count.ShouldBe(1);
        childText.IsFontDirty.ShouldBeFalse();
    }

    [Fact]
    public void UpdateLayout_ShouldClearFontDirty_EvenWhenFontCannotBeResolved()
    {
        // Hot-path guard against a retry storm: if a deferred font can't be resolved (no FontService,
        // unknown name), the flush must still clear isFontDirty so it isn't re-attempted on every
        // subsequent layout.
        CustomSetPropertyOnRenderable.FontService = null;
        TextRuntime textRuntime = new();

        GraphicalUiElement.IsAllLayoutSuspended = true;
        textRuntime.SetProperty("Font", "NonexistentFont12345");
        textRuntime.SetProperty("FontSize", 24);
        GraphicalUiElement.IsAllLayoutSuspended = false;
        textRuntime.IsFontDirty.ShouldBeTrue();   // deferred

        textRuntime.UpdateLayout();

        textRuntime.IsFontDirty.ShouldBeFalse();   // cleared — no per-layout retry
    }

    #endregion

    #region Order-Dependent Property Application (issue #4567)

    [Fact]
    public void ApplyState_ShouldNotRequestStalePropertyCombo_WhenFontSortsAlphabeticallyBeforeFontSizeAndIsBold()
    {
        // Repro for #4567. ApplyState (GraphicalUiElement.ApplyState) applies a state's variables
        // one SetProperty call at a time, in the order StateSaveExtensionMethods.Initialize() sorts
        // state.Variables into -- alphabetical. "Font" sorts before both "FontSize" and "IsBold", so
        // setting Font fires an immediate UpdateToFontValues call (see the KNOWN GAP comment on
        // CustomSetPropertyOnRenderable.UpdateToFontValues) using FontSize/IsBold's still-default
        // values (TextRuntime.DefaultFontSize = 18, IsBold = false) instead of this state's actual
        // FontSize=15/IsBold=true. That transient combo doesn't match anything gumcli fonts
        // pre-generates, so it logs a "no usable font" warning even though the text is ultimately
        // rendered correctly once FontSize and IsBold are applied moments later.
        TextRuntime textRuntime = new();
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        StateSave state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Type = "string", Name = "Font", Value = "Fonts/Baloo2-Bold.ttf", SetsValue = true });
        state.Variables.Add(new VariableSave { Type = "int", Name = "FontSize", Value = 15, SetsValue = true });
        state.Variables.Add(new VariableSave { Type = "bool", Name = "IsBold", Value = true, SetsValue = true });

        textRuntime.ApplyState(state);

        // Currently captures 3 calls: (FontSize=18, IsBold=false) when Font is set, then
        // (FontSize=15, IsBold=false) when FontSize is set, then (FontSize=15, IsBold=true) once
        // IsBold itself is finally set -- only the last one reflects the state's actual values.
        capturedCalls.ShouldNotBeEmpty();
        capturedCalls.ShouldAllBe(bmfc => bmfc.FontSize == 15 && bmfc.IsBold,
            "every font request during this ApplyState call should reflect the state's actual " +
            "FontSize/IsBold, not a stale default caught mid-way through applying the state");
    }

    [Fact]
    public void ApplyState_ShouldNotFlushFontPrematurely_WhenStateAppliesNestedCategoryState()
    {
        // Gap test found while scoping a fix for #4567. A state's own Variables can include a
        // category-state assignment (e.g. "ButtonCategoryState" = "Highlighted"). GraphicalUiElement
        // resolves that by calling ApplyState AGAIN, on the same instance, nested inside the outer
        // ApplyState's own variable-application loop. "ButtonCategoryState" sorts alphabetically
        // before "Font"/"FontSize"/"IsBold", so the nested call fires before the outer state's own
        // font properties are applied. Any fix that makes font realization defer-until-resume for a
        // single ApplyState call must also make that defer reentrancy-safe across this nesting --
        // otherwise the nested call's own resume flushes a font using the not-yet-applied outer
        // values, reintroducing #4567's staleness through a different door.
        TextRuntime textRuntime = new();
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        StateSaveCategory category = new StateSaveCategory { Name = "ButtonCategory" };
        StateSave highlightedState = new StateSave { Name = "Highlighted" };
        highlightedState.Variables.Add(new VariableSave { Type = "int", Name = "OutlineThickness", Value = 2, SetsValue = true });
        category.States.Add(highlightedState);
        textRuntime.AddCategory(category);

        StateSave outerState = new StateSave { Name = "Default" };
        outerState.Variables.Add(new VariableSave { Type = "string", Name = "ButtonCategoryState", Value = "Highlighted", SetsValue = true });
        outerState.Variables.Add(new VariableSave { Type = "string", Name = "Font", Value = "Fonts/Baloo2-Bold.ttf", SetsValue = true });
        outerState.Variables.Add(new VariableSave { Type = "int", Name = "FontSize", Value = 15, SetsValue = true });
        outerState.Variables.Add(new VariableSave { Type = "bool", Name = "IsBold", Value = true, SetsValue = true });

        textRuntime.ApplyState(outerState);

        capturedCalls.ShouldNotBeEmpty();
        capturedCalls.ShouldAllBe(bmfc => bmfc.FontSize == 15 && bmfc.IsBold && bmfc.OutlineThickness == 2,
            "no font request during this ApplyState call -- including one triggered by the nested " +
            "category-state ApplyState -- should reflect anything other than the final, fully " +
            "applied combination (outer Font/FontSize/IsBold plus the nested category's OutlineThickness)");
    }

    [Fact]
    public void ApplyState_ShouldDeferUntilGlobalResume_WhenCalledWhileGloballySuspended()
    {
        // Must-stay-green pin: the pre-existing global-suspend defer/flush path (unrelated to
        // #4567's per-instance gap) must keep working once ApplyState's own suspend condition is
        // touched to close that gap.
        TextRuntime textRuntime = new();
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        StateSave state = new StateSave { Name = "Default" };
        state.Variables.Add(new VariableSave { Type = "string", Name = "Font", Value = "Fonts/Baloo2-Bold.ttf", SetsValue = true });
        state.Variables.Add(new VariableSave { Type = "int", Name = "FontSize", Value = 15, SetsValue = true });
        state.Variables.Add(new VariableSave { Type = "bool", Name = "IsBold", Value = true, SetsValue = true });

        try
        {
            GraphicalUiElement.IsAllLayoutSuspended = true;
            textRuntime.ApplyState(state);

            capturedCalls.ShouldBeEmpty();
            textRuntime.IsFontDirty.ShouldBeTrue();
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }

        textRuntime.UpdateFontRecursive();

        capturedCalls.Count.ShouldBe(1);
        capturedCalls[0].FontSize.ShouldBe(15);
        capturedCalls[0].IsBold.ShouldBeTrue();
    }

    [Fact]
    public void ApplyState_AppliedTwiceInSequence_ShouldEachResolveItsOwnFinalCombo_NotLeftoversFromThePrevious()
    {
        // Worse-than-expected discovery while writing this gap test: a SECOND, unrelated ApplyState
        // call can silently get served a WRONG font from cache, with no warning at all, because of
        // #4567's staleness in a PRIOR call. The Default state's own #4567 staleness (see the first
        // test in this region) makes a transient, never-rendered lookup for (FontSize=15,
        // IsBold=false) during its OWN application -- that lookup fails to resolve a real font,
        // silently caches Text.DefaultBitmapFont under that filename in LoaderManager (same shape as
        // RaylibGum.Tests' font-cache-poisoning regression), and never calls CreateFontIfNecessary
        // again. So when Disabled later legitimately asks for that exact (15, IsBold=false) combo,
        // it is served the poisoned cache entry directly -- CreateFontIfNecessary is never invoked,
        // and the mock captures nothing. `ShouldNotBeEmpty()` below is what should be true (a real
        // font lookup must happen for Disabled's own combo); today it is false because the poisoned
        // cache from Default's OWN #4567 staleness swallows it first.
        TextRuntime textRuntime = new();
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        StateSave defaultState = new StateSave { Name = "Default" };
        defaultState.Variables.Add(new VariableSave { Type = "string", Name = "Font", Value = "Fonts/Baloo2-Bold.ttf", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Type = "int", Name = "FontSize", Value = 15, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Type = "bool", Name = "IsBold", Value = true, SetsValue = true });

        StateSave disabledState = new StateSave { Name = "Disabled" };
        disabledState.Variables.Add(new VariableSave { Type = "bool", Name = "IsBold", Value = false, SetsValue = true });

        textRuntime.ApplyState(defaultState);
        capturedCalls.Clear();

        textRuntime.ApplyState(disabledState);

        capturedCalls.ShouldNotBeEmpty(
            "Disabled's own (FontSize=15, IsBold=false) combo must trigger a real font lookup, not " +
            "silently reuse a cache entry poisoned by Default's own #4567 staleness");
        capturedCalls.ShouldAllBe(bmfc => bmfc.FontSize == 15 && bmfc.IsBold == false,
            "the second ApplyState call should resolve Font/FontSize carried over from the first " +
            "call plus its own IsBold override -- not a stale combination from mid-way through either call");
    }

    [Fact]
    public void ApplyState_OnStackedChild_ShouldNotRequestStalePropertyCombo()
    {
        // Extends the base #4567 repro onto a stacking parent's child, since UpdateLayout cascading
        // through a stack is a documented tricky area for font realization (see the "Layout
        // Suspension / Font Batching" region above) -- confirms the fix holds in that topology too.
        ContainerRuntime parent = new();
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        TextRuntime childText = new();
        parent.AddChild(childText);
        List<BmfcSave> capturedCalls = StartCapturingFontCalls();

        StateSave state = new StateSave { Name = "Highlighted" };
        state.Variables.Add(new VariableSave { Type = "string", Name = "Font", Value = "Fonts/Baloo2-Bold.ttf", SetsValue = true });
        state.Variables.Add(new VariableSave { Type = "int", Name = "FontSize", Value = 15, SetsValue = true });
        state.Variables.Add(new VariableSave { Type = "bool", Name = "IsBold", Value = true, SetsValue = true });

        childText.ApplyState(state);

        capturedCalls.ShouldNotBeEmpty();
        capturedCalls.ShouldAllBe(bmfc => bmfc.FontSize == 15 && bmfc.IsBold,
            "a stacked child's ApplyState call must resolve the same final combo as an unstacked one");
    }

    #endregion

    #region Drag-Scrub Suppression (issue #4005 follow-up)

    [Fact]
    public void DirectSetter_ShouldNotCallFontGeneration_WhenSuppressFontRegenerationIsTrue()
    {
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        try
        {
            GraphicalUiElement.SuppressFontRegeneration = true;
            textRuntime.Font = "Consolas";
            textRuntime.FontSize = 24;

            capturedCalls.Count.ShouldBe(0);
        }
        finally
        {
            GraphicalUiElement.SuppressFontRegeneration = false;
        }
    }

    [Fact]
    public void StringSetPath_ShouldNotCallFontGeneration_WhenSuppressFontRegenerationIsTrue()
    {
        // This is the path the tool's Variable Grid uses (SetProperty). A numeric drag-scrub (e.g.
        // DropshadowOffsetX) fires this on every intermediate tick, which is what floods the font
        // cache with one KernSmith bake per tick if not suppressed.
        TextRuntime textRuntime = new();
        var capturedCalls = StartCapturingFontCalls();

        try
        {
            GraphicalUiElement.SuppressFontRegeneration = true;
            textRuntime.SetProperty("Font", "Consolas");
            textRuntime.SetProperty("FontSize", 24);

            capturedCalls.Count.ShouldBe(0);
        }
        finally
        {
            GraphicalUiElement.SuppressFontRegeneration = false;
        }
    }

    [Fact]
    public void StringSetPath_ShouldGenerateOnceWithFinalValue_AfterSuppressionCleared()
    {
        // Mirrors a drag-scrub gesture: many intermediate ticks (suppressed, zero generations),
        // then the mouse-up commit (suppression cleared) with the final value actually generates.
        // Not asserting an exact count for the commit itself: the string-set path already invokes
        // font generation twice per SetProperty call regardless of suppression (the property setter's
        // own UpdateToFontValues() plus the explicit ReactToFontValueChange() redispatch) -- a
        // pre-existing, unrelated inefficiency this fix doesn't change.
        TextRuntime textRuntime = new();
        textRuntime.Font = "Consolas";
        var capturedCalls = StartCapturingFontCalls();

        try
        {
            GraphicalUiElement.SuppressFontRegeneration = true;
            textRuntime.SetProperty("FontSize", 20);
            textRuntime.SetProperty("FontSize", 21);
            textRuntime.SetProperty("FontSize", 22);

            capturedCalls.Count.ShouldBe(0);
        }
        finally
        {
            GraphicalUiElement.SuppressFontRegeneration = false;
        }

        textRuntime.SetProperty("FontSize", 24);

        capturedCalls.ShouldNotBeEmpty();
        capturedCalls.ShouldAllBe(bmfc => bmfc.FontSize == 24);
    }

    #endregion
}
