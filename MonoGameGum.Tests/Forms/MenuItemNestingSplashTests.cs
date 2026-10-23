using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Splash-damage sweep over the MenuItem/Menu popup (#6685, #6691): submenus nested one to three
/// levels deep, how they stack, close together and stay on the canvas.
/// </summary>
public class MenuItemNestingSplashTests : BaseTestClass
{
    #region Helpers

    private class Bar
    {
        public ContainerRuntime Container = null!;
        public Menu Menu = null!;
        public MenuItem File = null!;
        public MenuItem Recent = null!;
        public MenuItem Deep = null!;
        public MenuItem Deeper = null!;
        public MenuItem NewItem = null!;
    }

    // File (New, Open, Recent (One, Deep (Alpha, Beta, Deeper (Leaf)))).
    private static Bar CreateBar(Anchor? anchor = null, float x = 0, float y = 0)
    {
        Bar bar = new();
        bar.Container = new ContainerRuntime();
        bar.Container.Width = 400;
        bar.Container.WidthUnits = DimensionUnitType.Absolute;
        bar.Container.Height = 40;
        bar.Container.HeightUnits = DimensionUnitType.Absolute;
        Gum.GumService.Default.Root.Children!.Add(bar.Container);
        if (anchor != null)
        {
            bar.Container.Anchor(anchor.Value);
        }
        bar.Container.X += x;
        bar.Container.Y += y;

        bar.Menu = new Menu();
        bar.Container.Children!.Add(bar.Menu.Visual);

        bar.File = new MenuItem { Header = "File" };
        bar.Menu.Items!.Add(bar.File);
        bar.NewItem = new MenuItem { Header = "New" };
        bar.File.Items!.Add(bar.NewItem);
        bar.File.Items.Add(new MenuItem { Header = "Open" });
        bar.Recent = new MenuItem { Header = "Recent" };
        bar.File.Items.Add(bar.Recent);
        bar.Recent.Items!.Add(new MenuItem { Header = "One" });
        bar.Deep = new MenuItem { Header = "Deep" };
        bar.Recent.Items.Add(bar.Deep);
        bar.Deep.Items!.Add(new MenuItem { Header = "Alpha" });
        bar.Deep.Items.Add(new MenuItem { Header = "Beta" });
        bar.Deeper = new MenuItem { Header = "Deeper" };
        bar.Deep.Items.Add(bar.Deeper);
        bar.Deeper.Items!.Add(new MenuItem { Header = "Leaf" });

        bar.Container.UpdateLayout();
        return bar;
    }

    private static void OpenAllLevels(Bar bar)
    {
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;
        bar.Deep.IsSelected = true;
        bar.Deeper.IsSelected = true;
    }

    /// <summary>Ticks the roots in the order GumService.Update does: Root, then PopupRoot.</summary>
    private static void Tick(double seconds = 0.016)
    {
        Gum.GumService.Default.Root.AnimateSelf(seconds);
        FrameworkElement.PopupRoot.AnimateSelf(seconds);
    }

    private static GraphicalUiElement PopupOf(MenuItem owner)
    {
        GraphicalUiElement visual = ((MenuItem)owner.Items![0]!).Visual;
        while (visual.Parent != null && visual.Parent != FrameworkElement.PopupRoot)
        {
            visual = (GraphicalUiElement)visual.Parent;
        }
        return visual;
    }

    // The popup of the owner when it lives in the argument root.
    private static GraphicalUiElement PopupIn(GraphicalUiElement root, MenuItem owner)
    {
        GraphicalUiElement visual = ((MenuItem)owner.Items![0]!).Visual;
        while (visual.Parent != null && visual.Parent != root)
        {
            visual = (GraphicalUiElement)visual.Parent;
        }
        return visual;
    }

    private static int PopupRootCount() => FrameworkElement.PopupRoot.Children!.Count;

    private static int PopupIndex(MenuItem owner) => FrameworkElement.PopupRoot.Children!.IndexOf(PopupOf(owner));

    private static void ResizeCanvas(float width, float height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Gum.GumService.Default.Root.UpdateLayout();
        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
    }

    private static void ShouldBeOnCanvas(GraphicalUiElement popup)
    {
        popup.AbsoluteLeft.ShouldBeGreaterThanOrEqualTo(-0.5f);
        popup.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(-0.5f);
        (popup.AbsoluteLeft + popup.AbsoluteWidth).ShouldBeLessThanOrEqualTo(GraphicalUiElement.CanvasWidth + 0.5f);
        (popup.AbsoluteTop + popup.AbsoluteHeight).ShouldBeLessThanOrEqualTo(GraphicalUiElement.CanvasHeight + 0.5f);
    }

    private static void ShouldBeBelow(MenuItem owner)
    {
        GraphicalUiElement popup = PopupOf(owner);
        popup.AbsoluteLeft.ShouldBe(owner.Visual.AbsoluteLeft, 0.5f);
        popup.AbsoluteTop.ShouldBe(owner.Visual.GetAbsoluteBottom(), 0.5f);
    }

    private static void ShouldBeBeside(MenuItem owner)
    {
        GraphicalUiElement popup = PopupOf(owner);
        popup.AbsoluteLeft.ShouldBe(owner.Visual.GetAbsoluteRight(), 0.5f);
        popup.AbsoluteTop.ShouldBe(owner.Visual.AbsoluteTop, 0.5f);
    }

    #endregion

    #region Placement

    [Fact]
    public void Open_OneLevel_PopupIsInThePopupRootBelowTheItem()
    {
        Bar bar = CreateBar();

        bar.File.IsSelected = true;

        PopupOf(bar.File).Parent.ShouldBe(FrameworkElement.PopupRoot);
        ShouldBeBelow(bar.File);
    }

    [Fact]
    public void Open_TwoLevels_SecondPopupIsBesideItsItem()
    {
        Bar bar = CreateBar();
        bar.File.IsSelected = true;

        bar.Recent.IsSelected = true;

        PopupOf(bar.Recent).Parent.ShouldBe(FrameworkElement.PopupRoot);
        ShouldBeBelow(bar.File);
        ShouldBeBeside(bar.Recent);
    }

    [Fact]
    public void Open_ThreeLevels_EachPopupIsBesideItsItem()
    {
        Bar bar = CreateBar();

        OpenAllLevels(bar);

        ShouldBeBelow(bar.File);
        ShouldBeBeside(bar.Recent);
        ShouldBeBeside(bar.Deep);
        ShouldBeBeside(bar.Deeper);
    }

    [Fact]
    public void Open_ThreeLevels_EachLevelDrawsAboveItsParent()
    {
        Bar bar = CreateBar();

        OpenAllLevels(bar);

        PopupIndex(bar.File).ShouldBeGreaterThanOrEqualTo(0);
        PopupIndex(bar.Recent).ShouldBeGreaterThan(PopupIndex(bar.File));
        PopupIndex(bar.Deep).ShouldBeGreaterThan(PopupIndex(bar.Recent));
        PopupIndex(bar.Deeper).ShouldBeGreaterThan(PopupIndex(bar.Deep));
    }

    [Fact]
    public void Open_ThreeLevels_AddsOnePopupPerLevel()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();

        OpenAllLevels(bar);

        PopupRootCount().ShouldBe(baseline + 4);
    }

    [Fact]
    public void Open_NestedLevelsInTheModalRoot_PopupsStayInTheModalRoot()
    {
        Bar bar = CreateBar();
        Gum.GumService.Default.Root.Children!.Remove(bar.Container);
        FrameworkElement.ModalRoot.Children!.Add(bar.Container);
        bar.Container.UpdateLayout();

        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;

        PopupIn(FrameworkElement.ModalRoot, bar.File).Parent.ShouldBe(FrameworkElement.ModalRoot);
        PopupIn(FrameworkElement.ModalRoot, bar.Recent).Parent.ShouldBe(FrameworkElement.ModalRoot);
        PopupIn(FrameworkElement.ModalRoot, bar.Recent).ShouldNotBe(FrameworkElement.ModalRoot);
    }

    #endregion

    #region Closing

    [Fact]
    public void Close_TopLevelWhileThreeLevelsOpen_ClosesEveryLevel()
    {
        Bar bar = CreateBar();
        int baseline = PopupRootCount();
        OpenAllLevels(bar);

        bar.File.IsSelected = false;

        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.Deep.IsPopupVisible.ShouldBeFalse();
        bar.Deeper.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline);
    }

    [Fact]
    public void Close_TopLevelWhileThreeLevelsOpen_DeselectsEveryLevel()
    {
        Bar bar = CreateBar();
        OpenAllLevels(bar);

        bar.File.IsSelected = false;

        bar.Recent.IsSelected.ShouldBeFalse();
        bar.Deep.IsSelected.ShouldBeFalse();
        bar.Deeper.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void Close_MiddleLevel_LeavesTheTopLevelOpenAndClosesBelow()
    {
        Bar bar = CreateBar();
        OpenAllLevels(bar);

        bar.Recent.IsSelected = false;

        bar.File.IsPopupVisible.ShouldBeTrue();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.Deep.IsPopupVisible.ShouldBeFalse();
        bar.Deeper.IsPopupVisible.ShouldBeFalse();
        ShouldBeBelow(bar.File);
    }

    [Fact]
    public void Close_DeepestLevel_LeavesTheOthersOpenAndInPlace()
    {
        Bar bar = CreateBar();
        OpenAllLevels(bar);

        bar.Deeper.IsSelected = false;

        bar.Deep.IsPopupVisible.ShouldBeTrue();
        bar.Deeper.IsPopupVisible.ShouldBeFalse();
        ShouldBeBelow(bar.File);
        ShouldBeBeside(bar.Recent);
        ShouldBeBeside(bar.Deep);
    }

    [Fact]
    public void Switch_SiblingAtTheSecondLevel_ClosesTheDeeperLevels()
    {
        Bar bar = CreateBar();
        OpenAllLevels(bar);
        int baseline = PopupRootCount();

        bar.NewItem.IsSelected = true;

        bar.Recent.IsPopupVisible.ShouldBeFalse();
        bar.Deep.IsPopupVisible.ShouldBeFalse();
        bar.Deeper.IsPopupVisible.ShouldBeFalse();
        bar.File.IsPopupVisible.ShouldBeTrue();
        PopupRootCount().ShouldBe(baseline - 3);
    }

    [Fact]
    public void Switch_SiblingAtTheThirdLevel_ClosesOnlyTheLevelBelowIt()
    {
        Bar bar = CreateBar();
        OpenAllLevels(bar);
        MenuItem one = (MenuItem)bar.Recent.Items![0]!;

        one.IsSelected = true;

        bar.Deep.IsPopupVisible.ShouldBeFalse();
        bar.Deeper.IsPopupVisible.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeTrue();
        bar.File.IsPopupVisible.ShouldBeTrue();
    }

    [Fact]
    public void Reopen_AfterClosingThreeLevels_ShowsOnlyTheTopLevelAndNoStaleSelection()
    {
        Bar bar = CreateBar();
        OpenAllLevels(bar);
        bar.File.IsSelected = false;
        int baseline = PopupRootCount();

        bar.File.IsSelected = true;

        bar.Recent.IsSelected.ShouldBeFalse();
        bar.Recent.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline + 1);
        ShouldBeBelow(bar.File);
    }

    [Fact]
    public void Reopen_ThreeLevelsAgain_PlacesEveryLevelCorrectly()
    {
        Bar bar = CreateBar();
        OpenAllLevels(bar);
        bar.File.IsSelected = false;

        OpenAllLevels(bar);

        ShouldBeBelow(bar.File);
        ShouldBeBeside(bar.Recent);
        ShouldBeBeside(bar.Deep);
        ShouldBeBeside(bar.Deeper);
    }

    [Fact]
    public void Switch_TopLevelItemWhileThreeLevelsOpen_ClosesEverythingUnderTheOldOne()
    {
        Bar bar = CreateBar();
        MenuItem other = new() { Header = "Other" };
        bar.Menu.Items!.Add(other);
        other.Items!.Add(new MenuItem { Header = "Thing" });
        bar.Container.UpdateLayout();
        int baseline = PopupRootCount();
        OpenAllLevels(bar);

        other.IsSelected = true;

        bar.File.IsPopupVisible.ShouldBeFalse();
        bar.Deeper.IsPopupVisible.ShouldBeFalse();
        PopupRootCount().ShouldBe(baseline + 1);
    }

    #endregion

    #region Canvas edges and following

    [Fact]
    public void Clamp_NearRightEdge_EveryLevelStaysOnTheCanvas()
    {
        Bar bar = CreateBar(x: GraphicalUiElement.CanvasWidth - 30);

        OpenAllLevels(bar);

        ShouldBeOnCanvas(PopupOf(bar.File));
        ShouldBeOnCanvas(PopupOf(bar.Recent));
        ShouldBeOnCanvas(PopupOf(bar.Deep));
        ShouldBeOnCanvas(PopupOf(bar.Deeper));
    }

    [Fact]
    public void Clamp_NearBottomEdge_EveryLevelStaysOnTheCanvas()
    {
        Bar bar = CreateBar(Anchor.BottomLeft);

        OpenAllLevels(bar);

        ShouldBeOnCanvas(PopupOf(bar.File));
        ShouldBeOnCanvas(PopupOf(bar.Recent));
        ShouldBeOnCanvas(PopupOf(bar.Deep));
        ShouldBeOnCanvas(PopupOf(bar.Deeper));
    }

    [Fact]
    public void Clamp_AboveTheTopEdge_PopupIsPulledBackOntoTheCanvas()
    {
        Bar bar = CreateBar(y: -30);

        bar.File.IsSelected = true;

        PopupOf(bar.File).AbsoluteTop.ShouldBeGreaterThanOrEqualTo(-0.5f);
    }

    [Fact]
    public void Clamp_CanvasShrinksWhileThreeLevelsOpen_EveryLevelStaysOnTheCanvas()
    {
        Bar bar = CreateBar(Anchor.TopRight);
        OpenAllLevels(bar);

        ResizeCanvas(500, 300);
        Tick();

        ShouldBeOnCanvas(PopupOf(bar.File));
        ShouldBeOnCanvas(PopupOf(bar.Recent));
        ShouldBeOnCanvas(PopupOf(bar.Deep));
        ShouldBeOnCanvas(PopupOf(bar.Deeper));
    }

    [Fact]
    public void Follow_MenuMovesWhileThreeLevelsOpen_EveryLevelFollows()
    {
        Bar bar = CreateBar(x: 50, y: 50);
        OpenAllLevels(bar);

        bar.Container.X += 40;
        bar.Container.Y += 25;
        bar.Container.UpdateLayout();
        Tick();

        ShouldBeBelow(bar.File);
        ShouldBeBeside(bar.Recent);
        ShouldBeBeside(bar.Deep);
        ShouldBeBeside(bar.Deeper);
    }

    [Fact]
    public void Follow_MenuMovesWhileThreeLevelsOpen_DrawOrderStaysParentBeforeChild()
    {
        Bar bar = CreateBar(x: 50, y: 50);
        OpenAllLevels(bar);

        bar.Container.X += 40;
        bar.Container.UpdateLayout();
        Tick();

        PopupIndex(bar.Recent).ShouldBeGreaterThan(PopupIndex(bar.File));
        PopupIndex(bar.Deep).ShouldBeGreaterThan(PopupIndex(bar.Recent));
        PopupIndex(bar.Deeper).ShouldBeGreaterThan(PopupIndex(bar.Deep));
    }

    [Fact]
    public void Follow_NestedLevelClosedAndReopenedAfterTheMenuMoved_IsPlacedBesideItsItem()
    {
        Bar bar = CreateBar(x: 50, y: 50);
        bar.File.IsSelected = true;
        bar.Recent.IsSelected = true;
        bar.Container.X += 80;
        bar.Container.UpdateLayout();
        Tick();

        bar.Recent.IsSelected = false;
        bar.Recent.IsSelected = true;

        ShouldBeBeside(bar.Recent);
    }

    #endregion
}
