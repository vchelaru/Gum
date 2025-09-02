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
public class GraphicalUiElementTests
{
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

    #region Parent and ParentChanged 

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

    #endregion

    #region Layout-related

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

    [Fact]
    public void SetRenderable_NameMatches()
    {
        string name = "name1";
        string name2 = "name2";

        var gue = new GraphicalUiElement(new InvisibleRenderable() { Name = name });

        gue.Name.ShouldNotBeNull();
        gue.Name.ShouldMatch(name);

        gue.SetContainedObject(new InvisibleRenderable() { Name = name2 });
        gue.Name.ShouldMatch(name2);

        var gue2 = new GraphicalUiElement();
        gue2.Name.ShouldBeNull();
        gue2.SetContainedObject(new InvisibleRenderable() { Name = name2 });
        gue2.Name.ShouldMatch(name2);
        gue2.SetContainedObject(new InvisibleRenderable() { Name = name });
        gue2.Name.ShouldMatch(name);
    }

}
