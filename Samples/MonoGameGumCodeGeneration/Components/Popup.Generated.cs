//Code for Popup (Container)
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
partial class Popup
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Popup");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Popup - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Popup(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Popup)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Popup", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }

    public Popup(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public Popup() : base(new ContainerRuntime())
    {

        this.Visual.Height = 176f;
         
        this.Visual.Width = 272f;
        this.Visual.X = 0f;
        this.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Visual.Y = 0f;
        this.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        NineSliceInstance = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        NineSliceInstance.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (NineSliceInstance.ElementSave != null) NineSliceInstance.AddStatesAndCategoriesRecursivelyToGue(NineSliceInstance.ElementSave);
        if (NineSliceInstance.ElementSave != null) NineSliceInstance.SetInitialState();
        NineSliceInstance.Name = "NineSliceInstance";
        TextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(NineSliceInstance);
        NineSliceInstance.AddChild(TextInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.NineSliceInstance.Blue = 137;
        this.NineSliceInstance.Green = 17;
        this.NineSliceInstance.Height = 0f;
        this.NineSliceInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.NineSliceInstance.Red = 0;
        this.NineSliceInstance.SourceFileName = @"examplespriteframe.png";
        this.NineSliceInstance.Width = 0f;
        this.NineSliceInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.NineSliceInstance.X = 0f;
        this.NineSliceInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.NineSliceInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.NineSliceInstance.Y = 0f;
        this.NineSliceInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.NineSliceInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.TextInstance.Height = 0f;
        this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ((TextRuntime)this.TextInstance).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance.Text = @"This is text inside of a popup. This text wraps automatically.";
        ((TextRuntime)this.TextInstance).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.TextInstance.Width = -16f;
        this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.X = 0f;
        this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.TextInstance.Y = 8f;
        this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.TextInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

    }
    partial void CustomInitialize();
}
