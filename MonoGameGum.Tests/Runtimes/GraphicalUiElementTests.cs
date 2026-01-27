using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using Xunit;
using Moq;
using RenderingLibrary;
using System.Collections.ObjectModel;

namespace MonoGameGum.Tests.Runtimes;
public class GraphicalUiElementTests : BaseTestClass
{

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateValidInstance()
    {
        GraphicalUiElement sut = new();


        sut.Children.ShouldNotBeNull();
    }

    #endregion

    #region AddToRoot

    [Fact]
    public void AddToRoot_ShouldAddToRootCorrectly()
    {
        GraphicalUiElement child = new();
        child.AddToRoot();

        child.Parent.ShouldBe(GumService.Default.Root);
        GumService.Default.Root.Children.ShouldContain(child);
    }

    #endregion

    #region Animation
    static (ComponentSave element, AnimationRuntime animation) CreateElementAndAnimation()
    {
        ComponentSave element = new();
        element.Name = "Animated component";
        element.States.Add(new StateSave { Name = "Default" });

        var category = new StateSaveCategory { Name = "Category1" };
        element.Categories.Add(category);

        var state1 = new StateSave { Name = "State1" };
        state1.Variables.Add(new() { Name = "X", Value = 0f });
        category.States.Add(state1);

        var state2 = new StateSave { Name = "State2" };
        state2.Variables.Add(new() { Name = "X", Value = 100f });
        category.States.Add(state2);

        var key1 = new KeyframeRuntime
        {
            InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear,
            Time = 0,
            StateName = "Category1/State1"
        };

        var key2 = new KeyframeRuntime
        {
            Time = 1,
            StateName = "Category1/State2"
        };

        var animation = new AnimationRuntime { Name = "Anim1" };
        animation.Keyframes.Add(key1);
        animation.Keyframes.Add(key2);
        animation.RefreshCumulativeStates(element);

        return (element, animation);
    }

    [Fact]
    public void UpdateAnimation_ShouldApplyAnimation()
    {
        var (element, animation) = CreateElementAndAnimation();
        var gue = new GraphicalUiElement(new InvisibleRenderable()) { ElementSave = element };

        gue.ApplyAnimation(animation, 0.5);

        gue.X.ShouldBe(50f);
    }

    [Fact]
    public void UpdateAnimation_ByIndex_ShouldApplyAnimation()
    {
        var (element, animation) = CreateElementAndAnimation();
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            ElementSave = element,
            Animations = new() { animation }
        };

        gue.ApplyAnimation(0, 1.0);

        gue.X.ShouldBe(100f);
    }

    [Fact]
    public void ApplyAnimation_ByName_ShouldApplyAnimation()
    {
        var (element, animation) = CreateElementAndAnimation();
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            ElementSave = element,
            Animations = new() { animation }
        };

        gue.ApplyAnimation("Anim1", 1.0);

        gue.X.ShouldBe(100f);
    }

    [Fact]
    public void GetAnimation_ShouldReturnNull_IfIndexInvalid()
    {
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Animations = new()
        };

        gue.GetAnimation(1).ShouldBeNull();
    }

    [Fact]
    public void GetAnimation_ShouldReturnNull_IfNameInvalid()
    {
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            Animations = new()
        };

        gue.GetAnimation("Missing").ShouldBeNull();
    }

    [Fact]
    public void PlayAndStopAnimation_ShouldControlAnimation()
    {
        var (element, animation) = CreateElementAndAnimation();
        var gue = new GraphicalUiElement(new InvisibleRenderable())
        {
            ElementSave = element
        };

        gue.PlayAnimation(animation);
        gue.AnimateSelf(0.5);
        gue.X.ShouldBe(50f);

        gue.StopAnimation();
        gue.AnimateSelf(0.5);
        gue.X.ShouldBe(50f);
    }

    [Fact]
    public void ApplyAnimation_ShouldThrow_IfNull()
    {
        var gue = new GraphicalUiElement(new InvisibleRenderable());
        bool didThrow = false;
        try
        {
            gue.ApplyAnimation(animation: null!, timeInSeconds: 0);
        }
        catch (Exception)
        {
            didThrow = true;
        }
        didThrow.ShouldBeTrue();
    }

    [Fact]
    public void PlayAnimation_ShouldThrow_IfNull()
    {
        var gue = new GraphicalUiElement(new InvisibleRenderable());
        bool didThrow = false;
        try
        {
            gue.PlayAnimation(animation: null!);
        }
        catch (Exception)
        {
            didThrow = true;
        }
        didThrow.ShouldBeTrue();
    }
    #endregion

    [Fact]
    public void FillListWithChildrenByType_ShouldFillRecursively()
    {
        ContainerRuntime sut = new();

        sut.Children.Add(new SpriteRuntime());
        sut.Children.Add(new TextRuntime());
        ContainerRuntime childContainer = new();
        childContainer.Children.Add(new SpriteRuntime());
        sut.Children.Add(childContainer);

        var list = sut.FillListWithChildrenByTypeRecursively<SpriteRuntime>();

        list.Count.ShouldBe(2);
        list[0].ShouldBeOfType<SpriteRuntime>();
        list[1].ShouldBeOfType<SpriteRuntime>();
    }

    #region Layout-related (Units, Width, Height)

    [Fact]
    public void Dock_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new ();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime dockLeft = new();
        parent.AddChild (dockLeft);
        dockLeft.Dock(Dock.Left);
        dockLeft.X.ShouldBe(0);
        dockLeft.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        dockLeft.XOrigin.ShouldBe(HorizontalAlignment.Left);
        dockLeft.Y.ShouldBe(0);
        dockLeft.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        dockLeft.YOrigin.ShouldBe(VerticalAlignment.Center);
        dockLeft.Height.ShouldBe(0);
        dockLeft.HeightUnits.ShouldBe(DimensionUnitType.RelativeToParent);
    }

    [Fact]
    public void Anchor_TopLeft_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Center;
        anchor.YOrigin = VerticalAlignment.Center;

        // Perform action
        anchor.Anchor(Anchor.TopLeft);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Left);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Top);
    }

    [Fact]
    public void Anchor_Top_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Right;
        anchor.YOrigin = VerticalAlignment.Center;

        // Perform action
        anchor.Anchor(Anchor.Top);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Center);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Top);
    }

    [Fact]
    public void Anchor_TopRight_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Center;
        anchor.YOrigin = VerticalAlignment.Center;

        // Perform action
        anchor.Anchor(Anchor.TopRight);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Right);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Top);
    }

    [Fact]
    public void Anchor_Left_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Center;
        anchor.YOrigin = VerticalAlignment.Top;

        // Perform action
        anchor.Anchor(Anchor.Left);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Left);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Center);
    }

    [Fact]
    public void Anchor_Center_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Right;
        anchor.YOrigin = VerticalAlignment.Top;

        // Perform action
        anchor.Anchor(Anchor.Center);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Center);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Center);
    }

    [Fact]
    public void Anchor_Right_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Center;
        anchor.YOrigin = VerticalAlignment.Top;

        // Perform action
        anchor.Anchor(Anchor.Right);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Right);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Center);
    }


    [Fact]
    public void Anchor_BottomLeft_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Center;
        anchor.YOrigin = VerticalAlignment.Top;

        // Perform action
        anchor.Anchor(Anchor.BottomLeft);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromSmall);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Left);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Bottom);
    }

    [Fact]
    public void Anchor_Bottom_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Right;
        anchor.YOrigin = VerticalAlignment.Top;

        // Perform action
        anchor.Anchor(Anchor.Bottom);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Center);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Bottom);
    }

    [Fact]
    public void Anchor_BottomRight_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Center;
        anchor.YOrigin = VerticalAlignment.Top;

        // Perform action
        anchor.Anchor(Anchor.BottomRight);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Right);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromLarge);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Bottom);
    }


    [Fact]
    public void Anchor_CenterHorizontally_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Right;
        anchor.YOrigin = VerticalAlignment.Top;

        // Perform action
        anchor.Anchor(Anchor.CenterHorizontally);

        // Validate results
        anchor.X.ShouldBe(0);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Center);

        // These should not have changed!
        anchor.Y.ShouldBe(200);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.Percentage);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Top);
    }


    [Fact]
    public void Anchor_CenterVertically_ShouldSetCorrectValues()
    {
        ContainerRuntime parent = new();
        parent.Width = 100;
        parent.Height = 100;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        // Setup control with value we want to change
        ContainerRuntime anchor = new();
        parent.AddChild(anchor);
        anchor.X = 100;
        anchor.Y = 200;
        anchor.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.YUnits = Gum.Converters.GeneralUnitType.Percentage;
        anchor.XOrigin = HorizontalAlignment.Right;
        anchor.YOrigin = VerticalAlignment.Top;

        // Perform action
        anchor.Anchor(Anchor.CenterVertically);

        // Validate results
        anchor.X.ShouldBe(100);
        anchor.XUnits.ShouldBe(Gum.Converters.GeneralUnitType.Percentage);
        anchor.XOrigin.ShouldBe(HorizontalAlignment.Right);

        anchor.Y.ShouldBe(0);
        anchor.YUnits.ShouldBe(Gum.Converters.GeneralUnitType.PixelsFromMiddle);
        anchor.YOrigin.ShouldBe(VerticalAlignment.Center);
    }


    [Fact]
    public void HeightUnits_RelativeToChildren_ShouldUseChildrenHeight_AutoGrid()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;

        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
        parent.AutoGridHorizontalCells = 2;
        parent.AutoGridVerticalCells = 2;

        for (int i = 0; i < 4; i++)
        {
            ContainerRuntime child = new();
            child.Height = 100;
            child.HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(child);
        }

        parent.GetAbsoluteHeight().ShouldBe(200);

        parent.StackSpacing = 20;

        parent.GetAbsoluteHeight().ShouldBe(220);
    }

    [Fact]
    public void MaxHeight_ShouldNotWrapVerticalStack_UntilExceeded()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.MaxHeight = 300;
        parent.WrapsChildren = true;

        for(int i = 0; i < 2; i++)
        {
            ContainerRuntime child = new ();
            child.Height = 100;
            child.HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(child);
        }

        parent.GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void MaxHeight_ShouldWrapVerticalStack_IfExceeded()
    {
        ContainerRuntime parent = new();
        parent.Height = 0;
        parent.Name = "Parent";
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        parent.MaxHeight = 150;
        parent.WrapsChildren = true;

        for (int i = 0; i < 2; i++)
        {
            ContainerRuntime child = new();
            child.Name = "Child " + i;
            child.Height = 100;
            child.HeightUnits = DimensionUnitType.Absolute;
            parent.AddChild(child);
        }

        parent.GetAbsoluteHeight().ShouldBe(100);
    }

    [Fact]
    public void MaxWidth_ShouldWrapHorizontalStack_IfExceeded()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.Name = "Parent";
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        parent.MaxWidth = 150;
        parent.WrapsChildren = true;

        for (int i = 0; i < 2; i++)
        {
            ContainerRuntime child = new();
            child.Name = "Child " + i;
            child.Width = 100;
            child.WidthUnits = DimensionUnitType.Absolute;
            parent.AddChild(child);
        }

        parent.GetAbsoluteWidth().ShouldBe(100);
    }

    [Fact]
    public void WidthUnits_RelativeToChildren_ShouldUseChildrenWidth_AutoGrid()
    {
        ContainerRuntime parent = new();
        parent.Width = 0;
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;

        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
        parent.AutoGridHorizontalCells = 2;
        parent.AutoGridVerticalCells = 2;

        for (int i = 0; i < 4; i++)
        {
            ContainerRuntime child = new();
            child.Width = 100;
            child.WidthUnits = DimensionUnitType.Absolute;
            parent.AddChild(child);
        }

        parent.GetAbsoluteWidth().ShouldBe(200);

        parent.StackSpacing = 20;

        parent.GetAbsoluteWidth().ShouldBe(220);
    }

    [Fact]
    public void WidthUnits_Ratio_ShouldUseAvailableSpace()
    {
        ContainerRuntime parent = new ();
        parent.Width = 1000;

        ContainerRuntime sut = new();
        parent.Children.Add(sut);
        sut.Width = 1;
        sut.WidthUnits = DimensionUnitType.Ratio;

        sut.GetAbsoluteWidth().ShouldBe(1000);

        ContainerRuntime absoluteContainer = new();
        parent.AddChild(absoluteContainer);
        absoluteContainer.Width = 100;
        absoluteContainer.WidthUnits = DimensionUnitType.Absolute;

        sut.GetAbsoluteWidth().ShouldBe(900);

        ContainerRuntime percentContainer = new();
        parent.AddChild(percentContainer);
        percentContainer.Width = 5;
        percentContainer.WidthUnits = DimensionUnitType.PercentageOfParent;

        sut.GetAbsoluteWidth().ShouldBe(850);

        ContainerRuntime relativeToParentContainer = new();
        parent.AddChild(relativeToParentContainer);
        relativeToParentContainer.Width = -880; // 120 width
        relativeToParentContainer.WidthUnits = DimensionUnitType.RelativeToParent;

        sut.GetAbsoluteWidth().ShouldBe(850 - 120); // 730

        var mockSprite = new Mock<IPositionedSizedObject>()
            .As<IRenderable>()
            .As<IRenderableIpso>()
            .As<IVisible>()
            .As<ITextureCoordinate>();

        mockSprite
            .Setup(m=>m.SourceRectangle)
            .Returns(new System.Drawing.Rectangle(0, 0, 100, 100));
        ObservableCollection<IRenderableIpso> spriteChildren = new();
        mockSprite
            .As<IRenderableIpso>()
            .Setup(m => m.Children)
            .Returns(spriteChildren);
        mockSprite
            .As<IRenderableIpso>()
            .Setup(m => m.Width)
            .Returns(50);

        mockSprite
            .As<IVisible>()
            .Setup(m => m.AbsoluteVisible)
            .Returns(true);
        mockSprite
            .As<IVisible>()
            .Setup(m => m.Visible)
            .Returns(true);


        GraphicalUiElement percentOfSourceFile = new((IRenderable)mockSprite.Object);
        parent.AddChild(percentOfSourceFile);
        percentOfSourceFile.WidthUnits = DimensionUnitType.PercentageOfSourceFile;

        sut.GetAbsoluteWidth().ShouldBe(680);


        ContainerRuntime percentOtherDimension = new();
        parent.AddChild(percentOtherDimension);
        percentOtherDimension.Width = 50; // 50% of height
        percentOtherDimension.WidthUnits = DimensionUnitType.PercentageOfOtherDimension;
        percentOtherDimension.Height = 100; 
        sut.GetAbsoluteWidth().ShouldBe(630); 



    }

    [Fact]
    public void WidthUnits_Ratio_ShouldRespectAbsoluteMultipliedByFontScale()
    {
        GraphicalUiElement.GlobalFontScale = 2;

        ContainerRuntime parent = new();
        parent.Width = 1000;

        ContainerRuntime sut = new();
        parent.Children.Add(sut);
        sut.Width = 1;
        sut.WidthUnits = DimensionUnitType.Ratio;

        ContainerRuntime absoluteMultipliedByFontScaleContainer = new();
        parent.AddChild(absoluteMultipliedByFontScaleContainer);
        absoluteMultipliedByFontScaleContainer.Width = 100;
        absoluteMultipliedByFontScaleContainer.WidthUnits = DimensionUnitType.AbsoluteMultipliedByFontScale;

        sut.GetAbsoluteWidth().ShouldBe(800); // 1000 - (100 * 2)
    }

    [Fact]
    public void XValues_ShouldUpdateLayoutImmediately()
    {
        ContainerRuntime parent = new();
        parent.Width = 1000;
        parent.Height = 1000;

        ContainerRuntime child = new();
        parent.AddChild(child);

        child.AbsoluteX.ShouldBe(0);

        child.X = 80;
        child.AbsoluteX.ShouldBe(80);

        child.XUnits = Gum.Converters.GeneralUnitType.Percentage;
        child.AbsoluteX.ShouldBe(800, 
            "because changing XUnits should immediately update parent");

        parent.Width = 500;
        child.AbsoluteX.ShouldBe(400,
            "because changing the parent width should immediately update the child");
    }


    [Fact]
    public void YUnits_ShouldBeIgnored_ForSubsequentStackedSiblings()
    {
        ContainerRuntime parent = new();
        parent.Height = 300;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;

        ContainerRuntime child1 = new ContainerRuntime();
        parent.Children.Add(child1);
        child1.Height = 100;
        child1.HeightUnits = DimensionUnitType.Absolute;
        child1.AbsoluteTop.ShouldBe(0);

        ContainerRuntime child2 = new ();
        parent.Children.Add(child2);
        child2.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        child2.AbsoluteTop.ShouldBe(100);
    }

    #endregion

    #region Auto Grid

    [Fact]
    public void AutoGrid_ShouldPositionChildrenInGrid()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.Height = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
        parent.AutoGridHorizontalCells = 2;
        parent.AutoGridVerticalCells = 2;

        for (int i = 0; i < 4; i++)
        {
            ContainerRuntime child = new();
            parent.AddChild(child);
        }

        parent.Children[0].AbsoluteX.ShouldBe(0);
        parent.Children[0].AbsoluteY.ShouldBe(0);

        parent.Children[1].AbsoluteX.ShouldBe(200);
        parent.Children[1].AbsoluteY.ShouldBe(0);

        parent.Children[2].AbsoluteX.ShouldBe(0);
        parent.Children[2].AbsoluteY.ShouldBe(200);

        parent.Children[3].AbsoluteX.ShouldBe(200);
        parent.Children[3].AbsoluteY.ShouldBe(200);
    }

    [Fact]
    public void AutoGrid_ShouldPositionChildrenInGrid_WithSpacing()
    {
        ContainerRuntime parent = new();
        parent.Width = 405;
        parent.Height = 405;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;

        parent.StackSpacing = 5;

        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
        parent.AutoGridHorizontalCells = 2;
        parent.AutoGridVerticalCells = 2;

        for (int i = 0; i < 4; i++)
        {
            ContainerRuntime child = new();
            parent.AddChild(child);
        }

        parent.Children[0].AbsoluteX.ShouldBe(0);
        parent.Children[0].AbsoluteY.ShouldBe(0);

        parent.Children[1].AbsoluteX.ShouldBe(205);
        parent.Children[1].AbsoluteY.ShouldBe(0);

        parent.Children[2].AbsoluteX.ShouldBe(0);
        parent.Children[2].AbsoluteY.ShouldBe(205);

        parent.Children[3].AbsoluteX.ShouldBe(205);
        parent.Children[3].AbsoluteY.ShouldBe(205);
    }

    [Fact]
    public void AutoGrid_ShouldResizeChildrenToFitGrid()
    {
        ContainerRuntime parent = new();
        parent.Width = 400;
        parent.Height = 400;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
        parent.AutoGridHorizontalCells = 2;
        parent.AutoGridVerticalCells = 2;
        for (int i = 0; i < 4; i++)
        {
            ContainerRuntime child = new();
            child.Dock(Dock.Fill);
            parent.AddChild(child);
        }
        parent.Children[0].GetAbsoluteWidth().ShouldBe(200);
        parent.Children[0].GetAbsoluteHeight().ShouldBe(200);
        parent.Children[1].GetAbsoluteWidth().ShouldBe(200);
        parent.Children[1].GetAbsoluteHeight().ShouldBe(200);
        parent.Children[2].GetAbsoluteWidth().ShouldBe(200);
        parent.Children[2].GetAbsoluteHeight().ShouldBe(200);
        parent.Children[3].GetAbsoluteWidth().ShouldBe(200);
        parent.Children[3].GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void AutoGrid_ShouldResizeChildrenToFitGrid_WithSpacing()
    {
        ContainerRuntime parent = new();
        parent.Width = 405;
        parent.Height = 405;
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
        parent.AutoGridHorizontalCells = 2;
        parent.AutoGridVerticalCells = 2;

        parent.StackSpacing = 5;

        for (int i = 0; i < 4; i++)
        {
            ContainerRuntime child = new();
            child.Dock(Dock.Fill);
            parent.AddChild(child);
        }
        parent.Children[0].GetAbsoluteWidth().ShouldBe(200);
        parent.Children[0].GetAbsoluteHeight().ShouldBe(200);
        parent.Children[1].GetAbsoluteWidth().ShouldBe(200);
        parent.Children[1].GetAbsoluteHeight().ShouldBe(200);
        parent.Children[2].GetAbsoluteWidth().ShouldBe(200);
        parent.Children[2].GetAbsoluteHeight().ShouldBe(200);
        parent.Children[3].GetAbsoluteWidth().ShouldBe(200);
        parent.Children[3].GetAbsoluteHeight().ShouldBe(200);
    }

    [Fact]
    public void AutoGridHorizontal_ShouldSizeWidth_AccordingToColumnCount()
    {
        ContainerRuntime container = new();
        container.Width = 0;
        container.WidthUnits = DimensionUnitType.RelativeToChildren;

        container.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
        container.AutoGridHorizontalCells = 2;
        container.AutoGridVerticalCells = 2;

        container.GetAbsoluteWidth().ShouldBe(0);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(200);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(200);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(200);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(200);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(200);

        void AddChild()
        {
            ContainerRuntime child = new();
            child.WidthUnits = DimensionUnitType.Absolute;
            child.Width = 100;
            container.AddChild(child);
        }
    }

    [Fact]
    public void AutoGridHorizontal_ShouldSizeHeight_AccordingToColumnCount_WithSpillover()
    {
        ContainerRuntime container = new();
        container.Height = 0;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;

        container.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
        container.AutoGridHorizontalCells = 2;
        container.AutoGridVerticalCells = 2;

        container.GetAbsoluteHeight().ShouldBe(0);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(200);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(200);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(200);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(200);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(300);

        AddChild();
        AddChild();
        container.GetAbsoluteHeight().ShouldBe(400);

        void AddChild()
        {
            ContainerRuntime child = new();
            child.HeightUnits = DimensionUnitType.Absolute;
            child.Height = 100;
            container.AddChild(child);
        }
    }

    [Fact]
    public void AutoGridHorizontal_SizeRelativeToChildren_ShouldDistributExtraSize_ToAllCells()
    {
        ContainerRuntime container = new();
        container.Width = 100;
        container.WidthUnits = DimensionUnitType.RelativeToChildren;
        container.Height = 100;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;

        container.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridHorizontal;
        container.AutoGridHorizontalCells = 2;
        container.AutoGridVerticalCells = 2;

        ContainerRuntime absoluteContainer = new();
        container.AddChild(absoluteContainer);
        absoluteContainer.Width = 50;
        absoluteContainer.WidthUnits = DimensionUnitType.Absolute;
        absoluteContainer.Height = 50;
        absoluteContainer.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime fillContainer = new();
        container.AddChild(fillContainer);
        fillContainer.Dock(Dock.Fill);

        fillContainer.GetAbsoluteWidth().ShouldBe(100);
        fillContainer.GetAbsoluteHeight().ShouldBe(100);
        fillContainer.AbsoluteLeft.ShouldBe(100);

        ContainerRuntime fillContainer2 = new();
        fillContainer2.Name = nameof(fillContainer2);
        fillContainer2.Dock(Dock.Fill);
        container.AddChild(fillContainer2);

        fillContainer2.GetAbsoluteWidth().ShouldBe(100);
        fillContainer2.GetAbsoluteHeight().ShouldBe(100);
        fillContainer2.AbsoluteLeft.ShouldBe(0);
        fillContainer2.AbsoluteTop.ShouldBe(100);
    }

    [Fact]
    public void AutoGridVertical_ShouldSizeHeight_AccordingToRowCount()
    {
        ContainerRuntime container = new();
        container.Height = 0;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;

        container.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridVertical;
        container.AutoGridHorizontalCells = 2;
        container.AutoGridVerticalCells = 2;

        container.GetAbsoluteHeight().ShouldBe(0);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(200);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(200);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(200);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(200);

        AddChild();
        container.GetAbsoluteHeight().ShouldBe(200);

        void AddChild()
        {
            ContainerRuntime child = new();
            child.HeightUnits = DimensionUnitType.Absolute;
            child.Height = 100;
            container.AddChild(child);
        }
    }

    [Fact]
    public void AutoGridVertical_ShouldSizeWidth_AccordingToRowCount_WithSpillover()
    {
        ContainerRuntime container = new();
        container.Width = 0;
        container.WidthUnits = DimensionUnitType.RelativeToChildren;

        container.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridVertical;
        container.AutoGridHorizontalCells = 2;
        container.AutoGridVerticalCells = 2;

        container.GetAbsoluteWidth().ShouldBe(0);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(200);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(200);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(200);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(200);

        AddChild();
        container.GetAbsoluteWidth().ShouldBe(300);

        AddChild();
        AddChild();
        container.GetAbsoluteWidth().ShouldBe(400);

        void AddChild()
        {
            ContainerRuntime child = new();
            child.WidthUnits = DimensionUnitType.Absolute;
            child.Width = 100;
            container.AddChild(child);
        }
    }


    [Fact]
    public void AutoGridVertical_SizeRelativeToChildren_ShouldDistributExtraSize_ToAllCells()
    {
        ContainerRuntime container = new();
        container.Width = 100;
        container.WidthUnits = DimensionUnitType.RelativeToChildren;
        container.Height = 100;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;

        container.ChildrenLayout = Gum.Managers.ChildrenLayout.AutoGridVertical;
        container.AutoGridHorizontalCells = 2;
        container.AutoGridVerticalCells = 2;

        ContainerRuntime absoluteContainer = new();
        container.AddChild(absoluteContainer);
        absoluteContainer.Width = 50;
        absoluteContainer.WidthUnits = DimensionUnitType.Absolute;
        absoluteContainer.Height = 50;
        absoluteContainer.HeightUnits = DimensionUnitType.Absolute;

        ContainerRuntime fillContainer = new();
        container.AddChild(fillContainer);
        fillContainer.Dock(Dock.Fill);

        fillContainer.GetAbsoluteWidth().ShouldBe(100);
        fillContainer.GetAbsoluteHeight().ShouldBe(100);
        fillContainer.AbsoluteLeft.ShouldBe(0);
        fillContainer.AbsoluteTop.ShouldBe(100);

        ContainerRuntime fillContainer2 = new();
        fillContainer2.Name = nameof(fillContainer2);
        fillContainer2.Dock(Dock.Fill);
        container.AddChild(fillContainer2);

        fillContainer2.GetAbsoluteWidth().ShouldBe(100);
        fillContainer2.GetAbsoluteHeight().ShouldBe(100);
        fillContainer2.AbsoluteLeft.ShouldBe(100);
        fillContainer2.AbsoluteTop.ShouldBe(0);
    }

    #endregion

    #region Parent/Children related

    [Fact]
    public void AddChild_ShouldSetParentOnChild()
    {
        ContainerRuntime parent = new ();
        ContainerRuntime child = new ();
        parent.AddChild(child);
        child.Parent.ShouldBe(parent);
    }

    [Fact]
    public void AddChild_ShouldPopulateChildren()
    {
        ContainerRuntime parent = new ();
        ContainerRuntime child = new ();
        parent.AddChild(child);
        parent.Children.ShouldContain(child);
    }

    [Fact]
    public void AddChild_ShouldThrowException_OnAddingSelf()
    {
        ContainerRuntime container = new();
        bool didThrow = false;
        try
        {
            container.AddChild(container);
        }
        catch(Exception e)
        {
            e.Message.ShouldContain("cannot be added as a child of itself");
            didThrow = true;
        }

        didThrow.ShouldBeTrue();
    }

    [Fact]
    public void AssignParent_ShouldAddToParentsChildren()
    {
        ContainerRuntime parent = new ();
        ContainerRuntime child = new ();
        child.Parent = parent;
        parent.Children.ShouldContain(child);
    }

    [Fact]
    public void AssignParent_ShouldRemoveFromOldParentsChildren()
    {
        ContainerRuntime parent1 = new ();
        ContainerRuntime parent2 = new ();
        ContainerRuntime child = new ();
        parent1.AddChild(child);
        child.Parent = parent2;
        parent1.Children.ShouldNotContain(child);
        parent2.Children.ShouldContain(child);
    }

    [Fact]
    public void AssignParent_ToNull_ShouldRemoveFromOldParentsChildren()
    {
        ContainerRuntime parent1 = new ();
        ContainerRuntime child = new ();
        parent1.AddChild(child);
        child.Parent = null;
        parent1.Children.ShouldNotContain(child);
    }

    [Fact]
    public void ParentChanged_ShouldRaiseWhenParentChanges()
    {
        ContainerRuntime child = new ();

        int parentChangedCount = 0;
        child.ParentChanged += (_, _) => parentChangedCount++;
        child.Name = "Child";

        child.Parent = new ContainerRuntime() { Name = "ParentA" };
        parentChangedCount.ShouldBe(1);
        child.Parent = null;
        parentChangedCount.ShouldBeGreaterThan(1);

        // parent changes can be called multiple times, we want to make sure it was called
        // by checking the starting point
        var startingPoint = parentChangedCount;
        // Setting to the same parent should not raise the event:
        var parent = new ContainerRuntime();
        child.Parent = parent;
        parentChangedCount.ShouldBeGreaterThan(startingPoint);

        startingPoint = parentChangedCount;
        child.Parent = parent;
        parentChangedCount.ShouldBe(startingPoint);

        startingPoint = parentChangedCount;
        child.Parent = null;
        parentChangedCount.ShouldBeGreaterThan(startingPoint);


        var parent2 = new ContainerRuntime();
        startingPoint = parentChangedCount;
        parent.AddChild(child);
        parentChangedCount.ShouldBeGreaterThan(startingPoint);

        startingPoint = parentChangedCount;
        parent.Children.Remove(child);
        parentChangedCount.ShouldBeGreaterThan(startingPoint);

    }

    [Fact]
    public void ChildrenClear_ShouldSetChildParentsToNull()
    {
        ContainerRuntime parent = new ();

        ContainerRuntime child1 = new ();
        parent.AddChild(child1);

        child1.Parent.ShouldBe(parent);

        parent.Children!.Clear();

        child1.Parent.ShouldBeNull();
    }

    #endregion

    [Fact]
    public void RemoveFromRoot_ShouldRemoveFromRootCorrectly()
    {
        GraphicalUiElement child = new();
        child.AddToRoot();
        child.RemoveFromRoot();
        child.Parent.ShouldBeNull();
        GumService.Default.Root.Children.ShouldNotContain(child);
    }

    [Fact]
    public void SetRenderable_NameMatches()
    {
        string name = "name1";
        string name2 = "name2";

        // Initial inner obj set with name
        var gue = new GraphicalUiElement(new InvisibleRenderable() { Name = name });

        // Names should match
        gue.Name.ShouldNotBeNull();
        gue.Name.ShouldMatch(name);
        gue.Name.ShouldMatch(((InvisibleRenderable)gue.RenderableComponent).Name);

        // Name changed, should match
        gue.SetContainedObject(new InvisibleRenderable() { Name = name2 });
        gue.Name.ShouldMatch(name2);
        gue.Name.ShouldMatch(((InvisibleRenderable)gue.RenderableComponent).Name);

        // Inner obj is new obj with no name, name should match to previous name
        gue.SetContainedObject(new InvisibleRenderable());
        gue.Name.ShouldMatch(name2);
        gue.Name.ShouldMatch(((InvisibleRenderable)gue.RenderableComponent).Name);

        // RESTART: No inner obj set
        gue = new GraphicalUiElement();
        gue.Name.ShouldBeNull();

        // Inner obj set, but still null name
        gue.SetContainedObject(new InvisibleRenderable());
        gue.Name.ShouldBeNull();
        ((InvisibleRenderable)gue.RenderableComponent).Name.ShouldBeNull();

        // Change outer obj, inner obj should match
        gue.Name = name;
        gue.Name.ShouldMatch(name);
        gue.Name.ShouldMatch(((InvisibleRenderable)gue.RenderableComponent).Name);

        // Change inner obj with new name, should match inner obj
        gue.SetContainedObject(new InvisibleRenderable() { Name = name2 });
        gue.Name.ShouldMatch(name2);
        gue.Name.ShouldMatch(((InvisibleRenderable)gue.RenderableComponent).Name);
    }


    [Fact]
    public void Visible_ShouldUpdateChildren_IfWidthUnitsRatio()
    {
        ContainerRuntime parent = new();
        parent.Width = 200;
        parent.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime leftChild = new();
        parent.AddChild(leftChild);
        leftChild.Width = 100;
        leftChild.WidthUnits = DimensionUnitType.Absolute;

        ContainerRuntime rightChild = new();
        parent.AddChild(rightChild);
        // Doesn't really matter if we anchor but let's do it to make this test more realistic
        rightChild.Anchor(Anchor.Right);
        rightChild.Width = 1;
        rightChild.WidthUnits = DimensionUnitType.Ratio;

        ContainerRuntime sut = new();
        rightChild.AddChild(sut);
        sut.Dock(Dock.Fill);

        leftChild.Visible = false;

        rightChild.GetAbsoluteWidth().ShouldBe(200);
        sut.GetAbsoluteWidth().ShouldBe(200);

        leftChild.Visible = true;

        rightChild.GetAbsoluteWidth().ShouldBe(100);
        sut.GetAbsoluteWidth().ShouldBe(100);

    }
}
