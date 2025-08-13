//Code for Controls/TreeViewToggle (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class TreeViewToggle : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/TreeViewToggle");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new TreeViewToggle(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(TreeViewToggle)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/TreeViewToggle", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ToggleCategory
    {
        EnabledOn,
        EnabledOff,
        DisabledOn,
        DisabledOff,
        HighlightedOn,
        HighlightedOff,
        PushedOn,
        PushedOff,
    }

    private ToggleCategory? _toggleCategoryState;
    public ToggleCategory? ToggleCategoryState
    {
        get => _toggleCategoryState;
        set
        {
            _toggleCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case ToggleCategory.EnabledOn:
                        this.IconInstance.Visual.SetProperty("IconColor", "White");
                        this.IconInstance.Visual.Rotation = -90f;
                        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                        this.NineSliceInstance.SetProperty("ColorCategoryState", "Primary");
                        break;
                    case ToggleCategory.EnabledOff:
                        this.IconInstance.Visual.SetProperty("IconColor", "White");
                        this.IconInstance.Visual.Rotation = 0f;
                        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                        this.NineSliceInstance.SetProperty("ColorCategoryState", "Primary");
                        break;
                    case ToggleCategory.DisabledOn:
                        this.IconInstance.Visual.SetProperty("IconColor", "Gray");
                        this.IconInstance.Visual.Rotation = -90f;
                        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                        this.NineSliceInstance.SetProperty("ColorCategoryState", "DarkGray");
                        break;
                    case ToggleCategory.DisabledOff:
                        this.IconInstance.Visual.SetProperty("IconColor", "Gray");
                        this.IconInstance.Visual.Rotation = 0f;
                        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                        this.NineSliceInstance.SetProperty("ColorCategoryState", "DarkGray");
                        break;
                    case ToggleCategory.HighlightedOn:
                        this.IconInstance.Visual.SetProperty("IconColor", "White");
                        this.IconInstance.Visual.Rotation = -90f;
                        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                        this.NineSliceInstance.SetProperty("ColorCategoryState", "PrimaryLight");
                        break;
                    case ToggleCategory.HighlightedOff:
                        this.IconInstance.Visual.SetProperty("IconColor", "White");
                        this.IconInstance.Visual.Rotation = 0f;
                        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                        this.NineSliceInstance.SetProperty("ColorCategoryState", "PrimaryLight");
                        break;
                    case ToggleCategory.PushedOn:
                        this.IconInstance.Visual.SetProperty("IconColor", "White");
                        this.IconInstance.Visual.Rotation = -90f;
                        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
                        this.NineSliceInstance.SetProperty("ColorCategoryState", "PrimaryDark");
                        break;
                    case ToggleCategory.PushedOff:
                        this.IconInstance.Visual.SetProperty("IconColor", "White");
                        this.IconInstance.Visual.Rotation = 0f;
                        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
                        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
                        this.NineSliceInstance.SetProperty("ColorCategoryState", "PrimaryDark");
                        break;
                }
            }
        }
    }
    public NineSliceRuntime NineSliceInstance { get; protected set; }
    public Icon IconInstance { get; protected set; }

    public TreeViewToggle(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public TreeViewToggle() : base(new ContainerRuntime())
    {

        this.Visual.Height = 24f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
         
        this.Visual.Width = 24f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
        NineSliceInstance = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        NineSliceInstance.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (NineSliceInstance.ElementSave != null) NineSliceInstance.AddStatesAndCategoriesRecursivelyToGue(NineSliceInstance.ElementSave);
        if (NineSliceInstance.ElementSave != null) NineSliceInstance.SetInitialState();
        NineSliceInstance.Name = "NineSliceInstance";
        IconInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        IconInstance.Name = "IconInstance";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(NineSliceInstance);
        NineSliceInstance.AddChild(IconInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.NineSliceInstance.SetProperty("ColorCategoryState", "Primary");
        this.NineSliceInstance.SetProperty("StyleCategoryState", "Bordered");

        this.IconInstance.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Arrow2;
        this.IconInstance.Visual.Height = 0f;
        this.IconInstance.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.IconInstance.Visual.Width = 0f;
        this.IconInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

    }
    partial void CustomInitialize();
}
