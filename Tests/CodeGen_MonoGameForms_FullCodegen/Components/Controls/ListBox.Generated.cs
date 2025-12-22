//Code for Controls/ListBox (Container)
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
partial class ListBox : global::Gum.Forms.Controls.ListBox
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ListBox");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/ListBox - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ListBox(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ListBox)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.ListBox)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ListBox", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ListBoxCategory
    {
        Enabled,
        Disabled,
        Focused,
        DisabledFocused,
    }

    private ListBoxCategory? _listBoxCategoryState;
    public ListBoxCategory? ListBoxCategoryState
    {
        get => _listBoxCategoryState;
        set
        {
            _listBoxCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case ListBoxCategory.Enabled:
                        this.FocusedIndicator.Visible = false;
                        break;
                    case ListBoxCategory.Disabled:
                        this.FocusedIndicator.Visible = false;
                        break;
                    case ListBoxCategory.Focused:
                        this.FocusedIndicator.Visible = true;
                        break;
                    case ListBoxCategory.DisabledFocused:
                        this.FocusedIndicator.Visible = true;
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public ScrollBar VerticalScrollBarInstance { get; protected set; }
    public ContainerRuntime ClipContainerInstance { get; protected set; }
    public ContainerRuntime InnerPanelInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }
    public ContainerRuntime ClipAndScrollContainer { get; protected set; }
    public ContainerRuntime ClipContainerParent { get; protected set; }

    public ListBox(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public ListBox() : base(new ContainerRuntime())
    {

        this.Visual.Height = 256f;
         
        this.Visual.Width = 256f;

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
        ClipAndScrollContainer = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        ClipAndScrollContainer.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (ClipAndScrollContainer.ElementSave != null) ClipAndScrollContainer.AddStatesAndCategoriesRecursivelyToGue(ClipAndScrollContainer.ElementSave);
        if (ClipAndScrollContainer.ElementSave != null) ClipAndScrollContainer.SetInitialState();
        ClipAndScrollContainer.Name = "ClipAndScrollContainer";
        ClipContainerParent = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        ClipContainerParent.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (ClipContainerParent.ElementSave != null) ClipContainerParent.AddStatesAndCategoriesRecursivelyToGue(ClipContainerParent.ElementSave);
        if (ClipContainerParent.ElementSave != null) ClipContainerParent.SetInitialState();
        ClipContainerParent.Name = "ClipContainerParent";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        ClipAndScrollContainer.AddChild(VerticalScrollBarInstance);
        ClipContainerParent.AddChild(ClipContainerInstance);
        ClipContainerInstance.AddChild(InnerPanelInstance);
        this.AddChild(FocusedIndicator);
        this.AddChild(ClipAndScrollContainer);
        ClipAndScrollContainer.AddChild(ClipContainerParent);
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

        this.VerticalScrollBarInstance.Visual.Visible = true;
        this.VerticalScrollBarInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.VerticalScrollBarInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.VerticalScrollBarInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.VerticalScrollBarInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.ClipContainerInstance.ClipsChildren = true;
        this.ClipContainerInstance.Height = -4f;
        this.ClipContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ClipContainerInstance.Width = -4f;
        this.ClipContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ClipContainerInstance.X = 2f;
        this.ClipContainerInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.ClipContainerInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
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

        this.ClipAndScrollContainer.Height = -0f;
        this.ClipAndScrollContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ClipAndScrollContainer.Width = -0f;
        this.ClipAndScrollContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ClipAndScrollContainer.X = 0f;
        this.ClipAndScrollContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ClipAndScrollContainer.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.ClipAndScrollContainer.Y = 0f;
        this.ClipAndScrollContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.ClipAndScrollContainer.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.ClipContainerParent.Height = -0f;
        this.ClipContainerParent.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ClipContainerParent.Width = 1f;
        this.ClipContainerParent.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Ratio;
        this.ClipContainerParent.X = 0f;
        this.ClipContainerParent.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.ClipContainerParent.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.ClipContainerParent.Y = 0f;
        this.ClipContainerParent.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.ClipContainerParent.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    partial void CustomInitialize();
}
