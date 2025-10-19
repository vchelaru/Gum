//Code for TestComponent (Container)
using GumRuntime;
using System.Linq;
using SkiaGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace MauiSkiaGum.Components;
partial class TestComponentRuntime : SkiaGum.GueDeriving.ContainerRuntime
{
    public RoundedRectangleRuntime RoundedRectangleInstance { get; protected set; }
    public ColoredCircleRuntime ColoredCircleInstance { get; protected set; }
    public SvgRuntime SvgInstance { get; protected set; }

    public TestComponentRuntime(bool fullInstantiation = true)
    {
        if(fullInstantiation)
        {
            this.SetContainedObject(new InvisibleRenderable());
        }

        this.Height = 364f;
        this.Width = 460f;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        RoundedRectangleInstance = new global::SkiaGum.GueDeriving.RoundedRectangleRuntime();
        RoundedRectangleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("RoundedRectangle");
        if (RoundedRectangleInstance.ElementSave != null) RoundedRectangleInstance.AddStatesAndCategoriesRecursivelyToGue(RoundedRectangleInstance.ElementSave);
        if (RoundedRectangleInstance.ElementSave != null) RoundedRectangleInstance.SetInitialState();
        RoundedRectangleInstance.Name = "RoundedRectangleInstance";
        ColoredCircleInstance = new global::SkiaGum.GueDeriving.ColoredCircleRuntime();
        ColoredCircleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredCircle");
        if (ColoredCircleInstance.ElementSave != null) ColoredCircleInstance.AddStatesAndCategoriesRecursivelyToGue(ColoredCircleInstance.ElementSave);
        if (ColoredCircleInstance.ElementSave != null) ColoredCircleInstance.SetInitialState();
        ColoredCircleInstance.Name = "ColoredCircleInstance";
        SvgInstance = new global::SkiaGum.GueDeriving.SvgRuntime();
        SvgInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Svg");
        if (SvgInstance.ElementSave != null) SvgInstance.AddStatesAndCategoriesRecursivelyToGue(SvgInstance.ElementSave);
        if (SvgInstance.ElementSave != null) SvgInstance.SetInitialState();
        SvgInstance.Name = "SvgInstance";
    }
    protected virtual void AssignParents()
    {
        this.Children.Add(RoundedRectangleInstance);
        this.Children.Add(ColoredCircleInstance);
        this.Children.Add(SvgInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.RoundedRectangleInstance.Blue2 = 228;
        this.RoundedRectangleInstance.CornerRadius = 38f;
        this.RoundedRectangleInstance.GradientX1 = 100f;
        this.RoundedRectangleInstance.GradientY2 = 153f;
        this.RoundedRectangleInstance.Green1 = 201;
        this.RoundedRectangleInstance.Green2 = 75;
        this.RoundedRectangleInstance.Height = 0f;
        this.RoundedRectangleInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.RoundedRectangleInstance.Red1 = 126;
        this.RoundedRectangleInstance.Red2 = 7;
        this.RoundedRectangleInstance.UseGradient = true;
        this.RoundedRectangleInstance.Width = 0f;
        this.RoundedRectangleInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.RoundedRectangleInstance.X = 0f;
        this.RoundedRectangleInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.RoundedRectangleInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.RoundedRectangleInstance.Y = 0f;
        this.RoundedRectangleInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.RoundedRectangleInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.ColoredCircleInstance.Blue1 = 0;
        this.ColoredCircleInstance.Blue2 = 0;
        this.ColoredCircleInstance.GradientX1 = 93f;
        this.ColoredCircleInstance.Green2 = 43;
        this.ColoredCircleInstance.Red2 = 204;
        this.ColoredCircleInstance.UseGradient = true;
        this.ColoredCircleInstance.X = 33f;
        this.ColoredCircleInstance.Y = 84f;

        this.SvgInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.MaintainFileAspectRatio;
        this.SvgInstance.SourceFile = @"Resources\gum-logo-reverse.svg";
        this.SvgInstance.Width = 158f;
        this.SvgInstance.X = 214f;
        this.SvgInstance.Y = 72f;

    }
    partial void CustomInitialize();
}
