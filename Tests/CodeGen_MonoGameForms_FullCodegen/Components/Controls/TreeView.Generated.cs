//Code for Controls/TreeView (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class TreeView : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/TreeView");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/TreeView - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new TreeView(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(TreeView)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/TreeView", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime Background { get; protected set; }
    public ScrollBar VerticalScrollBarInstance { get; protected set; }
    public ContainerRuntime ClipContainerInstance { get; protected set; }
    public ContainerRuntime InnerPanelInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }

    public TreeView(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public TreeView() : base(new ContainerRuntime())
    {

         

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        Background = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        Background.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (Background.ElementSave != null) Background.AddStatesAndCategoriesRecursivelyToGue(Background.ElementSave);
        if (Background.ElementSave != null) Background.SetInitialState();
        Background.Name = "Background";
        VerticalScrollBarInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ScrollBar();
        VerticalScrollBarInstance.Name = "VerticalScrollBarInstance";
        ClipContainerInstance = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        ClipContainerInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (ClipContainerInstance.ElementSave != null) ClipContainerInstance.AddStatesAndCategoriesRecursivelyToGue(ClipContainerInstance.ElementSave);
        if (ClipContainerInstance.ElementSave != null) ClipContainerInstance.SetInitialState();
        ClipContainerInstance.Name = "ClipContainerInstance";
        InnerPanelInstance = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        InnerPanelInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (InnerPanelInstance.ElementSave != null) InnerPanelInstance.AddStatesAndCategoriesRecursivelyToGue(InnerPanelInstance.ElementSave);
        if (InnerPanelInstance.ElementSave != null) InnerPanelInstance.SetInitialState();
        InnerPanelInstance.Name = "InnerPanelInstance";
        FocusedIndicator = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        FocusedIndicator.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.AddStatesAndCategoriesRecursivelyToGue(FocusedIndicator.ElementSave);
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.SetInitialState();
        FocusedIndicator.Name = "FocusedIndicator";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        this.AddChild(VerticalScrollBarInstance);
        this.AddChild(ClipContainerInstance);
        ClipContainerInstance.AddChild(InnerPanelInstance);
        this.AddChild(FocusedIndicator);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "DarkGray");
        this.Background.SetProperty("StyleCategoryState", "Bordered");
        this.Background.Height = 0f;
        this.Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Background.Width = 0f;
        this.Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Background.X = 0f;
        this.Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Background.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Background.Y = 0f;
        this.Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Background.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.VerticalScrollBarInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.VerticalScrollBarInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.VerticalScrollBarInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.VerticalScrollBarInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.ClipContainerInstance.ClipsChildren = true;
        this.ClipContainerInstance.Height = -4f;
        this.ClipContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ClipContainerInstance.Width = -27f;
        this.ClipContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ClipContainerInstance.X = 2f;
        this.ClipContainerInstance.Y = 2f;
        this.ClipContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.ClipContainerInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.InnerPanelInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.InnerPanelInstance.Height = 0f;
        this.InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.InnerPanelInstance.Width = 0f;
        this.InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.InnerPanelInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.InnerPanelInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.InnerPanelInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.InnerPanelInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.FocusedIndicator.SetProperty("ColorCategoryState", "Warning");
        this.FocusedIndicator.SetProperty("StyleCategoryState", "Solid");
        this.FocusedIndicator.Height = 2f;
        this.FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.FocusedIndicator.Visible = false;
        this.FocusedIndicator.Y = 2f;
        this.FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.FocusedIndicator.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

    }
    partial void CustomInitialize();
}
