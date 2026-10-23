//Code for MainMenuFullGeneration
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGumCodeGeneration.Components;
using RenderingLibrary.Graphics;
using System.Linq;
namespace MonoGameGumCodeGeneration.Screens;
partial class MainMenuFullGeneration
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
            var element = ObjectFinder.Self.GetElementSave("MainMenuFullGeneration") ?? throw new System.InvalidOperationException("Could not find an element named MainMenuFullGeneration - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new MainMenuFullGeneration(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(MainMenuFullGeneration)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("MainMenuFullGeneration", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public Popup PopupInstance { get; protected set; }
    public ComponentWithStates ComponentWithStatesInstance { get; protected set; }
    public TextRuntime TextWithLotsOfPropertiesSet { get; protected set; }
    public PolygonRuntime PolygonInstance { get; protected set; }
    public CircleRuntime CircleInstance { get; protected set; }
    public PolygonRuntime PolygonInstance1 { get; protected set; }
    public RectangleRuntime RectangleInstance { get; protected set; }

    public MainMenuFullGeneration(InteractiveGue visual) : base(visual)
    {
        InitializeInstancesFromVisual();
        CustomInitialize();
    }
    public MainMenuFullGeneration() : base(new ContainerRuntime())
    {

         

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        PopupInstance = new MonoGameGumCodeGeneration.Components.Popup();
        PopupInstance.Name = "PopupInstance";
        ComponentWithStatesInstance = new MonoGameGumCodeGeneration.Components.ComponentWithStates();
        ComponentWithStatesInstance.Name = "ComponentWithStatesInstance";
        TextWithLotsOfPropertiesSet = new global::Gum.GueDeriving.TextRuntime();
        TextWithLotsOfPropertiesSet.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextWithLotsOfPropertiesSet.ElementSave != null) TextWithLotsOfPropertiesSet.AddStatesAndCategoriesRecursivelyToGue(TextWithLotsOfPropertiesSet.ElementSave);
        if (TextWithLotsOfPropertiesSet.ElementSave != null) TextWithLotsOfPropertiesSet.SetInitialState();
        TextWithLotsOfPropertiesSet.Name = "TextWithLotsOfPropertiesSet";
        PolygonInstance = new global::Gum.GueDeriving.PolygonRuntime();
        PolygonInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Polygon");
        if (PolygonInstance.ElementSave != null) PolygonInstance.AddStatesAndCategoriesRecursivelyToGue(PolygonInstance.ElementSave);
        if (PolygonInstance.ElementSave != null) PolygonInstance.SetInitialState();
        PolygonInstance.Name = "PolygonInstance";
        CircleInstance = new global::Gum.GueDeriving.CircleRuntime();
        CircleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Circle");
        if (CircleInstance.ElementSave != null) CircleInstance.AddStatesAndCategoriesRecursivelyToGue(CircleInstance.ElementSave);
        if (CircleInstance.ElementSave != null) CircleInstance.SetInitialState();
        CircleInstance.Name = "CircleInstance";
        PolygonInstance1 = new global::Gum.GueDeriving.PolygonRuntime();
        PolygonInstance1.ElementSave = ObjectFinder.Self.GetStandardElement("Polygon");
        if (PolygonInstance1.ElementSave != null) PolygonInstance1.AddStatesAndCategoriesRecursivelyToGue(PolygonInstance1.ElementSave);
        if (PolygonInstance1.ElementSave != null) PolygonInstance1.SetInitialState();
        PolygonInstance1.Name = "PolygonInstance1";
        RectangleInstance = new global::Gum.GueDeriving.RectangleRuntime();
        RectangleInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Rectangle");
        if (RectangleInstance.ElementSave != null) RectangleInstance.AddStatesAndCategoriesRecursivelyToGue(RectangleInstance.ElementSave);
        if (RectangleInstance.ElementSave != null) RectangleInstance.SetInitialState();
        RectangleInstance.Name = "RectangleInstance";
    }
    protected virtual void InitializeInstancesFromVisual()
    {
        PopupInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Popup>(this.Visual,"PopupInstance");
        ComponentWithStatesInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ComponentWithStates>(this.Visual,"ComponentWithStatesInstance");
        TextWithLotsOfPropertiesSet = this.Visual?.GetGraphicalUiElementByName("TextWithLotsOfPropertiesSet") as global::Gum.GueDeriving.TextRuntime;
        PolygonInstance = this.Visual?.GetGraphicalUiElementByName("PolygonInstance") as global::Gum.GueDeriving.PolygonRuntime;
        CircleInstance = this.Visual?.GetGraphicalUiElementByName("CircleInstance") as global::Gum.GueDeriving.CircleRuntime;
        PolygonInstance1 = this.Visual?.GetGraphicalUiElementByName("PolygonInstance1") as global::Gum.GueDeriving.PolygonRuntime;
        RectangleInstance = this.Visual?.GetGraphicalUiElementByName("RectangleInstance") as global::Gum.GueDeriving.RectangleRuntime;
    }
    protected virtual void AssignParents()
    {
        this.AddChild(PopupInstance);
        this.AddChild(ComponentWithStatesInstance);
        this.AddChild(TextWithLotsOfPropertiesSet);
        this.AddChild(PolygonInstance);
        this.AddChild(CircleInstance);
        this.AddChild(PolygonInstance1);
        this.AddChild(RectangleInstance);
    }
    protected virtual void ApplyDefaultVariables()
    {
        this.Visual.Width = 0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Visual.Height = 0f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.PopupInstance.Visual.X = 2f;
        this.PopupInstance.Visual.Y = -10f;

        this.ComponentWithStatesInstance.Visual.X = 85f;
        this.ComponentWithStatesInstance.Visual.Y = 65f;

        this.TextWithLotsOfPropertiesSet.Blue = 100;
        this.TextWithLotsOfPropertiesSet.FontSize = 24;
        this.TextWithLotsOfPropertiesSet.Green = 150;
        this.TextWithLotsOfPropertiesSet.Height = 10f;
        this.TextWithLotsOfPropertiesSet.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ((TextRuntime)this.TextWithLotsOfPropertiesSet).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextWithLotsOfPropertiesSet.LineHeightMultiplier = 1.2f;
        this.TextWithLotsOfPropertiesSet.Red = 200;
        this.TextWithLotsOfPropertiesSet.Text = @"I am a Text that has lots of properties set. I am testing all of the different properties that might be assigned on a Text object.";
        ((TextRuntime)this.TextWithLotsOfPropertiesSet).TextOverflowHorizontalMode = global::RenderingLibrary.Graphics.TextOverflowHorizontalMode.TruncateWord;
        this.TextWithLotsOfPropertiesSet.TextOverflowVerticalMode = global::RenderingLibrary.Graphics.TextOverflowVerticalMode.SpillOver;
        ((TextRuntime)this.TextWithLotsOfPropertiesSet).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.TextWithLotsOfPropertiesSet.Visible = true;
        this.TextWithLotsOfPropertiesSet.Width = 196f;
        this.TextWithLotsOfPropertiesSet.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.TextWithLotsOfPropertiesSet.X = 32f;
        this.TextWithLotsOfPropertiesSet.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextWithLotsOfPropertiesSet.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.TextWithLotsOfPropertiesSet.Y = 240f;
        this.TextWithLotsOfPropertiesSet.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.TextWithLotsOfPropertiesSet.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.PolygonInstance.X = 545f;
        this.PolygonInstance.Y = 55f;
        this.PolygonInstance.SetPoints(new System.Numerics.Vector2[]{
            new System.Numerics.Vector2(-32f, -32f),
            new System.Numerics.Vector2(32f, -32f),
            new System.Numerics.Vector2(108f, 71f),
            new System.Numerics.Vector2(-88f, 86f),
            new System.Numerics.Vector2(-83f, 13f),
            new System.Numerics.Vector2(-32f, -32f),
        });

        this.CircleInstance.StrokeAlpha = 255;
        this.CircleInstance.StrokeBlue = 255;
        this.CircleInstance.StrokeGreen = 0;
        this.CircleInstance.Height = 48f;
        this.CircleInstance.StrokeRed = 128;
        this.CircleInstance.Rotation = 0f;
        this.CircleInstance.Visible = true;
        this.CircleInstance.Width = 48f;
        this.CircleInstance.X = 664f;
        this.CircleInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.CircleInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.CircleInstance.Y = 220f;
        this.CircleInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.CircleInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.PolygonInstance1.Blue = 0;
        this.PolygonInstance1.Green = 255;
        this.PolygonInstance1.Red = 255;
        this.PolygonInstance1.Rotation = 0f;
        this.PolygonInstance1.X = 623f;
        this.PolygonInstance1.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.PolygonInstance1.Y = 317f;
        this.PolygonInstance1.SetPoints(new System.Numerics.Vector2[]{
            new System.Numerics.Vector2(-32f, -32f),
            new System.Numerics.Vector2(32f, -32f),
            new System.Numerics.Vector2(32f, 32f),
            new System.Numerics.Vector2(1f, 59f),
            new System.Numerics.Vector2(-32f, 32f),
            new System.Numerics.Vector2(-32f, -32f),
        });

        this.RectangleInstance.StrokeBlue = 219;
        this.RectangleInstance.StrokeGreen = 168;
        this.RectangleInstance.Height = 94f;
        this.RectangleInstance.StrokeRed = 74;
        this.RectangleInstance.Rotation = 0f;
        this.RectangleInstance.Width = 70f;
        this.RectangleInstance.X = 677f;
        this.RectangleInstance.Y = 408f;

    }
    partial void CustomInitialize();
}
