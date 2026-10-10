//Code for Controls/Label (Text)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class Label : global::Gum.Forms.Controls.Label
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
            var visual = new global::Gum.GueDeriving.TextRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/Label") ?? throw new System.InvalidOperationException("Could not find an element named Controls/Label - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Label(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Label)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/Label", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }

    public Label(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public Label() : base(new global::Gum.GueDeriving.TextRuntime())
    {

        ((global::Gum.GueDeriving.TextRuntime)this.Visual).SetProperty("ColorCategoryState", "White");
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).Height = 0f;
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).SetProperty("StyleCategoryState", "Strong");
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).Width = 0f;
        ((global::Gum.GueDeriving.TextRuntime)this.Visual).WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
    }
    private void ApplyDefaultVariables()
    {
    }
    partial void CustomInitialize();
}
