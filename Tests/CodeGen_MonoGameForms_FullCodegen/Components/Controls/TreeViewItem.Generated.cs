//Code for Controls/TreeViewItem (Container)
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
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
[global::Gum.DataTypes.GumElement("Controls/TreeViewItem")]
partial class TreeViewItem : global::Gum.Forms.Controls.FrameworkElement
{
    #if UNITY_5_3_OR_NEWER && !GUM_NO_UNITY_ENGINE
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetRequiredElementSave("Controls/TreeViewItem");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new TreeViewItem(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(TreeViewItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/TreeViewItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public TreeViewToggle ToggleButtonInstance { get; protected set; }
    public ListBoxItem ListBoxItemInstance { get; protected set; }
    public ContainerRuntime InnerPanelInstance { get; protected set; }

    public TreeViewItem(InteractiveGue visual) : base(visual)
    {
        InitializeInstancesFromVisual();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    public TreeViewItem() : base(new ContainerRuntime())
    {

        this.Visual.Height = 0f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
         
        this.Visual.Width = 0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        ReactToGeneratedVisual();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        ToggleButtonInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.TreeViewToggle();
        ToggleButtonInstance.Name = "ToggleButtonInstance";
        ListBoxItemInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ListBoxItem();
        ListBoxItemInstance.Name = "ListBoxItemInstance";
        InnerPanelInstance = new global::Gum.GueDeriving.ContainerRuntime();
        InnerPanelInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (InnerPanelInstance.ElementSave != null) InnerPanelInstance.AddStatesAndCategoriesRecursivelyToGue(InnerPanelInstance.ElementSave);
        if (InnerPanelInstance.ElementSave != null) InnerPanelInstance.SetInitialState();
        InnerPanelInstance.Name = "InnerPanelInstance";
    }
    protected virtual void InitializeInstancesFromVisual()
    {
        ToggleButtonInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<TreeViewToggle>(this.Visual,"ToggleButtonInstance");
        ListBoxItemInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBoxItem>(this.Visual,"ListBoxItemInstance");
        InnerPanelInstance = this.Visual?.GetGraphicalUiElementByName("InnerPanelInstance") as global::Gum.GueDeriving.ContainerRuntime;
    }
    protected virtual void AssignParents()
    {
        this.Visual.AddChild(this.ToggleButtonInstance.Visual);
        this.Visual.AddChild(this.ListBoxItemInstance.Visual);
        this.Visual.AddChild(this.InnerPanelInstance);
    }
    protected virtual void ApplyDefaultVariables()
    {

        this.ListBoxItemInstance.Visual.Height = 24f;
        this.ListBoxItemInstance.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.ListBoxItemInstance.Visual.Width = -24f;
        this.ListBoxItemInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.ListBoxItemInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.InnerPanelInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.InnerPanelInstance.Height = 0f;
        this.InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.InnerPanelInstance.Width = -24f;
        this.InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.InnerPanelInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.InnerPanelInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.InnerPanelInstance.Y = 24f;

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
