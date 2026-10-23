//Code for ComponentWithStates (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using KniSkiaGumCodeGeneration.Components;
using RenderingLibrary.Graphics;
using System.Linq;
namespace KniSkiaGumCodeGeneration.Components;
partial class ComponentWithStates
{
    #if UNITY_5_3_OR_NEWER
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("ComponentWithStates") ?? throw new System.InvalidOperationException("Could not find an element named ComponentWithStates - did you forget to load a Gum project?");
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
                        this.ColoredRectangleInstance.FillBlue = 0;
                        this.ColoredRectangleInstance.FillGreen = 0;
                        this.ColoredRectangleInstance.FillRed = 255;
                        break;
                    case ColorCategory.GreenState:
                        this.ColoredRectangleInstance.FillBlue = 0;
                        this.ColoredRectangleInstance.FillGreen = 255;
                        this.ColoredRectangleInstance.FillRed = 0;
                        break;
                    case ColorCategory.BlueState:
                        this.ColoredRectangleInstance.FillBlue = 255;
                        this.ColoredRectangleInstance.FillGreen = 0;
                        this.ColoredRectangleInstance.FillRed = 0;
                        break;
                }
            }
        }
    }
    public RectangleRuntime ColoredRectangleInstance { get; protected set; }

    public ComponentWithStates(InteractiveGue visual) : base(visual)
    {
        InitializeInstancesFromVisual();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    public ComponentWithStates() : base(new ContainerRuntime())
    {

         

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        ColoredRectangleInstance = new global::Gum.GueDeriving.RectangleRuntime();
        ColoredRectangleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (ColoredRectangleInstance.ElementSave != null) ColoredRectangleInstance.AddStatesAndCategoriesRecursivelyToGue(ColoredRectangleInstance.ElementSave);
        if (ColoredRectangleInstance.ElementSave != null) ColoredRectangleInstance.SetInitialState();
        ColoredRectangleInstance.IsFilled = true;
        ColoredRectangleInstance.StrokeWidth = 0f;
        ColoredRectangleInstance.Name = "ColoredRectangleInstance";
    }
    protected virtual void InitializeInstancesFromVisual()
    {
        ColoredRectangleInstance = this.Visual?.GetGraphicalUiElementByName("ColoredRectangleInstance") as global::Gum.GueDeriving.RectangleRuntime;
    }
    protected virtual void AssignParents()
    {
        this.Visual.AddChild(this.ColoredRectangleInstance);
    }
    protected virtual void ApplyDefaultVariables()
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
    private bool _isGeneratedVisualBuilt;
    protected override void ReactToVisualChanged()
    {
        if (_isGeneratedVisualBuilt) base.ReactToVisualChanged();
    }
    private void ReactToGeneratedVisual()
    {
        _isGeneratedVisualBuilt = true;
        ReactToVisualChanged();
    }
    partial void CustomInitialize();
}
