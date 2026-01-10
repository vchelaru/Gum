//Code for ComponentWithStates (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using MonoGameGum.GueDeriving;
using MonoGameGumCodeGeneration.Components;
using System.Linq;
namespace MonoGameGumCodeGeneration.Components;
partial class ComponentWithStates
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("ComponentWithStates");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named ComponentWithStates - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ComponentWithStates(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ComponentWithStates)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("ComponentWithStates", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ColorCategory
    {
        RedState,
        GreenState,
        BlueState,
    }

    private ColorCategory? _colorCategoryState;
    public ColorCategory? ColorCategoryState
    {
        get => _colorCategoryState;
        set
        {
            _colorCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case ColorCategory.RedState:
                        this.ColoredRectangleInstance.Blue = 0;
                        this.ColoredRectangleInstance.Green = 0;
                        this.ColoredRectangleInstance.Red = 255;
                        break;
                    case ColorCategory.GreenState:
                        this.ColoredRectangleInstance.Blue = 0;
                        this.ColoredRectangleInstance.Green = 255;
                        this.ColoredRectangleInstance.Red = 0;
                        break;
                    case ColorCategory.BlueState:
                        this.ColoredRectangleInstance.Blue = 255;
                        this.ColoredRectangleInstance.Green = 0;
                        this.ColoredRectangleInstance.Red = 0;
                        break;
                }
            }
        }
    }
    public ColoredRectangleRuntime ColoredRectangleInstance { get; protected set; }

    public ComponentWithStates(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public ComponentWithStates() : base(new ContainerRuntime())
    {

         

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        ColoredRectangleInstance = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        ColoredRectangleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (ColoredRectangleInstance.ElementSave != null) ColoredRectangleInstance.AddStatesAndCategoriesRecursivelyToGue(ColoredRectangleInstance.ElementSave);
        if (ColoredRectangleInstance.ElementSave != null) ColoredRectangleInstance.SetInitialState();
        ColoredRectangleInstance.Name = "ColoredRectangleInstance";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(ColoredRectangleInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.ColoredRectangleInstance.Height = 0f;
        this.ColoredRectangleInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ColoredRectangleInstance.Width = 0f;
        this.ColoredRectangleInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ColoredRectangleInstance.X = 0f;
        this.ColoredRectangleInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ColoredRectangleInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.ColoredRectangleInstance.Y = 0f;
        this.ColoredRectangleInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.ColoredRectangleInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    partial void CustomInitialize();
}
