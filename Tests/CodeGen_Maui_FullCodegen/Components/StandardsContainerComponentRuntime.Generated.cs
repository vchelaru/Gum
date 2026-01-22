//Code for StandardsContainerComponent (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using SkiaGum.GueDeriving;
using System.Linq;
namespace CodeGen_Maui_FullCodegen.Components;
partial class StandardsContainerComponentRuntime : SkiaGum.GueDeriving.ContainerRuntime
{
    public ArcRuntime ArcInstance { get; protected set; }
    public CircleRuntime CircleInstance { get; protected set; }
    public ColoredCircleRuntime ColoredCircleInstance { get; protected set; }
    public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }
    public RoundedRectangleRuntime RoundedRectangleInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }

    public StandardsContainerComponentRuntime(bool fullInstantiation = true)
    {
        if(fullInstantiation)
        {
            this.SetContainedObject(new InvisibleRenderable());
        }

        this.Height = 274f;
        this.Width = 470f;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        ArcInstance = new global::SkiaGum.GueDeriving.ArcRuntime();
        ArcInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Arc");
        if (ArcInstance.ElementSave != null) ArcInstance.AddStatesAndCategoriesRecursivelyToGue(ArcInstance.ElementSave);
        if (ArcInstance.ElementSave != null) ArcInstance.SetInitialState();
        ArcInstance.Name = "ArcInstance";
        CircleInstance = new global::SkiaGum.GueDeriving.CircleRuntime();
        CircleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Circle");
        if (CircleInstance.ElementSave != null) CircleInstance.AddStatesAndCategoriesRecursivelyToGue(CircleInstance.ElementSave);
        if (CircleInstance.ElementSave != null) CircleInstance.SetInitialState();
        CircleInstance.Name = "CircleInstance";
        ColoredCircleInstance = new global::SkiaGum.GueDeriving.ColoredCircleRuntime();
        ColoredCircleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredCircle");
        if (ColoredCircleInstance.ElementSave != null) ColoredCircleInstance.AddStatesAndCategoriesRecursivelyToGue(ColoredCircleInstance.ElementSave);
        if (ColoredCircleInstance.ElementSave != null) ColoredCircleInstance.SetInitialState();
        ColoredCircleInstance.Name = "ColoredCircleInstance";
        ColoredRectangleInstance = new global::SkiaGum.GueDeriving.ColoredRectangleRuntime();
        ColoredRectangleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (ColoredRectangleInstance.ElementSave != null) ColoredRectangleInstance.AddStatesAndCategoriesRecursivelyToGue(ColoredRectangleInstance.ElementSave);
        if (ColoredRectangleInstance.ElementSave != null) ColoredRectangleInstance.SetInitialState();
        ColoredRectangleInstance.Name = "ColoredRectangleInstance";
        RoundedRectangleInstance = new global::SkiaGum.GueDeriving.RoundedRectangleRuntime();
        RoundedRectangleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("RoundedRectangle");
        if (RoundedRectangleInstance.ElementSave != null) RoundedRectangleInstance.AddStatesAndCategoriesRecursivelyToGue(RoundedRectangleInstance.ElementSave);
        if (RoundedRectangleInstance.ElementSave != null) RoundedRectangleInstance.SetInitialState();
        RoundedRectangleInstance.Name = "RoundedRectangleInstance";
        TextInstance = new global::SkiaGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
    }
    protected virtual void AssignParents()
    {
        this.Children.Add(ArcInstance);
        this.Children.Add(CircleInstance);
        this.Children.Add(ColoredCircleInstance);
        this.Children.Add(ColoredRectangleInstance);
        this.Children.Add(RoundedRectangleInstance);
        this.Children.Add(TextInstance);
    }
    private void ApplyDefaultVariables()
    {

        this.CircleInstance.X = 123f;
        this.CircleInstance.Y = 5f;

        this.ColoredCircleInstance.X = 15f;
        this.ColoredCircleInstance.Y = 68f;

        this.ColoredRectangleInstance.X = 130f;
        this.ColoredRectangleInstance.Y = 84f;

        this.RoundedRectangleInstance.CornerRadius = 29f;
        this.RoundedRectangleInstance.Height = 135f;
        this.RoundedRectangleInstance.Width = 143f;
        this.RoundedRectangleInstance.X = 221f;
        this.RoundedRectangleInstance.Y = 44f;

        this.TextInstance.X = 25f;
        this.TextInstance.Y = 203f;

    }
    partial void CustomInitialize();
}
