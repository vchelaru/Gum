//Code for Controls/ScrollViewer (Container)
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
partial class ScrollViewer : global::MonoGameGum.Forms.Controls.ScrollViewer
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ScrollViewer");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ScrollViewer(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] = template;
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::MonoGameGum.Forms.Controls.ScrollViewer)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ScrollViewer", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ScrollBarVisibility
    {
        NoScrollBar,
        VerticalScrollVisible,
    }
    public enum ScrollViewerCategory
    {
        Enabled,
        Focused,
    }

    private ScrollBarVisibility? _scrollBarVisibilityState;
    public ScrollBarVisibility? ScrollBarVisibilityState
    {
        get => _scrollBarVisibilityState;
        set
        {
            _scrollBarVisibilityState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case ScrollBarVisibility.NoScrollBar:
                        this.VerticalScrollBarInstance.Visual.Visible = false;
                        break;
                    case ScrollBarVisibility.VerticalScrollVisible:
                        this.VerticalScrollBarInstance.Visual.Visible = true;
                        break;
                }
            }
        }
    }

    private ScrollViewerCategory? _scrollViewerCategoryState;
    public ScrollViewerCategory? ScrollViewerCategoryState
    {
        get => _scrollViewerCategoryState;
        set
        {
            _scrollViewerCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case ScrollViewerCategory.Enabled:
                        this.FocusedIndicator.Visible = false;
                        break;
                    case ScrollViewerCategory.Focused:
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

    public ScrollViewer(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public ScrollViewer() : base(new ContainerRuntime())
    {

         

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
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
