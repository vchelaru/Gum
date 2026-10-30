#pragma warning disable CS0618
using System.IO;
using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumPreview;
using MonoGameGum.Tests.Hot;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumPreview;

/// <summary>
/// The project's canvas size drives Preview's window and canvas at launch and when a reload finds a
/// different saved size, and at no other time (#7167).
/// </summary>
public class PreviewCanvasSizeTests : BaseTestClass
{
    private static GumProjectSave Project(int width, int height) =>
        new GumProjectSave { DefaultCanvasWidth = width, DefaultCanvasHeight = height };

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void TheFirstLook_AppliesTheSavedSize()
    {
        PreviewCanvasSize canvas = new PreviewCanvasSize();

        canvas.TryTakeChange(Project(1280, 720), out int width, out int height).ShouldBeTrue();

        width.ShouldBe(1280);
        height.ShouldBe(720);
    }

    [Fact]
    public void ASavedSizeThatIsUnchanged_AppliesNothing()
    {
        PreviewCanvasSize canvas = new PreviewCanvasSize();
        canvas.TryTakeChange(Project(1280, 720), out _, out _);

        canvas.TryTakeChange(Project(1280, 720), out _, out _).ShouldBeFalse();
        canvas.TryTakeChange(Project(1280, 720), out _, out _).ShouldBeFalse();
    }

    [Theory]
    [InlineData(1000, 720)]
    [InlineData(1280, 800)]
    [InlineData(640, 480)]
    public void ASavedWidthOrHeightThatChanged_AppliesTheNewSizeOnce(int newWidth, int newHeight)
    {
        PreviewCanvasSize canvas = new PreviewCanvasSize();
        canvas.TryTakeChange(Project(1280, 720), out _, out _);

        canvas.TryTakeChange(Project(newWidth, newHeight), out int width, out int height).ShouldBeTrue();
        width.ShouldBe(newWidth);
        height.ShouldBe(newHeight);
        canvas.TryTakeChange(Project(newWidth, newHeight), out _, out _).ShouldBeFalse();
    }

    [Fact]
    public void ChangingTheSavedSizeBackToAnEarlierValue_AppliesItAgain()
    {
        PreviewCanvasSize canvas = new PreviewCanvasSize();
        canvas.TryTakeChange(Project(1280, 720), out _, out _);
        canvas.TryTakeChange(Project(1000, 700), out _, out _);

        canvas.TryTakeChange(Project(1280, 720), out int width, out int height).ShouldBeTrue();

        width.ShouldBe(1280);
        height.ShouldBe(720);
    }

    [Fact]
    public void NoProjectOrANonPositiveSize_UsesTheFallbackLikeALaunchDoes()
    {
        PreviewCanvasSize canvas = new PreviewCanvasSize();

        canvas.TryTakeChange(null, out int width, out int height).ShouldBeTrue();
        width.ShouldBe(PreviewCanvasSize.FallbackWidth);
        height.ShouldBe(PreviewCanvasSize.FallbackHeight);

        canvas.TryTakeChange(Project(0, -5), out _, out _).ShouldBeFalse();
        canvas.TryTakeChange(Project(1280, 0), out width, out height).ShouldBeTrue();
        width.ShouldBe(1280);
        height.ShouldBe(PreviewCanvasSize.FallbackHeight);
    }

    // The same flow as a hot reload through Preview: the manager loads the saved files into the finder.
    private static void SaveCanvasSize(SweepWorld world, int width, int height)
    {
        GumProjectSave project = GumProjectSave.Load(world.Gumx);
        project.DefaultCanvasWidth = width;
        project.DefaultCanvasHeight = height;
        project.Save(world.Gumx, saveElements: false);
        world.Notify(world.Gumx);
        world.Advance(300);
        world.Manager.Update(new System.Collections.Generic.List<GraphicalUiElement>(GumService.Default.Root.Children));
    }

    [Fact]
    public void AReloadAfterACanvasSizeEdit_AppliesTheNewSizeToThePercentBasedLayout()
    {
        using SweepWorld world = new SweepWorld();
        world.Boot(new SweepSpec());
        PreviewCanvasSize canvas = new PreviewCanvasSize();
        canvas.TryTakeChange(ObjectFinder.Self.GumProjectSave, out int width, out int height);
        PreviewCanvasSize.ApplyToCanvas(GumService.Default.Root, width, height);
        ContainerRuntime half = new ContainerRuntime { WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent, Width = 50 };
        GumService.Default.Root.AddChild(half);
        GumService.Default.Root.UpdateLayout();
        float before = half.GetAbsoluteWidth();

        SaveCanvasSize(world, width * 2, height * 2);

        canvas.TryTakeChange(ObjectFinder.Self.GumProjectSave, out width, out height).ShouldBeTrue();
        PreviewCanvasSize.ApplyToCanvas(GumService.Default.Root, width, height);
        GraphicalUiElement.CanvasWidth.ShouldBe(width);
        GraphicalUiElement.CanvasHeight.ShouldBe(height);
        half.GetAbsoluteWidth().ShouldBe(before * 2);
    }

    [Fact]
    public void AReloadThatOnlyEditsAnElement_AppliesNothing()
    {
        using SweepWorld world = new SweepWorld();
        world.Boot(new SweepSpec());
        PreviewCanvasSize canvas = new PreviewCanvasSize();
        canvas.TryTakeChange(ObjectFinder.Self.GumProjectSave, out _, out _);

        world.SaveAll(new SweepSpec { BoxX = 9 });
        world.Notify(world.Gumx);
        world.Notify(world.ScreenFile);
        world.Advance(300);
        world.Manager.Update(new System.Collections.Generic.List<GraphicalUiElement>(GumService.Default.Root.Children));

        canvas.TryTakeChange(ObjectFinder.Self.GumProjectSave, out _, out _).ShouldBeFalse();
    }
}
