//Code for Controls/SplitterStandard (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class SplitterStandard : global::Gum.Forms.Controls.Splitter
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/SplitterStandard");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/SplitterStandard - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new SplitterStandard(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(SplitterStandard)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.Splitter)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/SplitterStandard", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }

    public SplitterStandard(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public SplitterStandard() : base(new ContainerRuntime())
    {

        this.Visual.Height = 5f;
        this.Visual.Width = -0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Visual.X = 0f;
        this.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

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
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(NineSliceInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.NineSliceInstance.SetProperty("ColorCategoryState", "DarkGray");
        this.NineSliceInstance.SetProperty("StyleCategoryState", "Bordered");
        this.NineSliceInstance.Height = -0f;
        this.NineSliceInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.NineSliceInstance.Width = -0f;
        this.NineSliceInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.NineSliceInstance.X = 0f;
        this.NineSliceInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.NineSliceInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.NineSliceInstance.Y = 0f;
        this.NineSliceInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.NineSliceInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    partial void CustomInitialize();
}
