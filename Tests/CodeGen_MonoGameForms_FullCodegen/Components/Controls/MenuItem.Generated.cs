//Code for Controls/MenuItem (Container)
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
partial class MenuItem : global::Gum.Forms.Controls.MenuItem
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/MenuItem");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/MenuItem - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new MenuItem(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(MenuItem)] = template;
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::Gum.Forms.Controls.MenuItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/MenuItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum MenuItemCategory
    {
        Enabled,
        Highlighted,
        Selected,
        Focused,
        Disabled,
    }

    private MenuItemCategory? _menuItemCategoryState;
    public MenuItemCategory? MenuItemCategoryState
    {
        get => _menuItemCategoryState;
        set
        {
            _menuItemCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case MenuItemCategory.Enabled:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case MenuItemCategory.Highlighted:
                        this.Background.SetProperty("ColorCategoryState", "LightGray");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case MenuItemCategory.Selected:
                        this.Background.SetProperty("ColorCategoryState", "Primary");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case MenuItemCategory.Focused:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.TextInstance.SetProperty("ColorCategoryState", "White");
                        break;
                    case MenuItemCategory.Disabled:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.TextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime SubmenuIndicatorInstance { get; protected set; }
    public ContainerRuntime ContainerInstance { get; protected set; }

    public MenuItem(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public MenuItem() : base(new ContainerRuntime())
    {

        this.Visual.Height = 0f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
         
        this.Visual.Width = 0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.Visual.X = 0f;
        this.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.Visual.Y = 0f;
        this.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

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
        TextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        SubmenuIndicatorInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        SubmenuIndicatorInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (SubmenuIndicatorInstance.ElementSave != null) SubmenuIndicatorInstance.AddStatesAndCategoriesRecursivelyToGue(SubmenuIndicatorInstance.ElementSave);
        if (SubmenuIndicatorInstance.ElementSave != null) SubmenuIndicatorInstance.SetInitialState();
        SubmenuIndicatorInstance.Name = "SubmenuIndicatorInstance";
        ContainerInstance = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        ContainerInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (ContainerInstance.ElementSave != null) ContainerInstance.AddStatesAndCategoriesRecursivelyToGue(ContainerInstance.ElementSave);
        if (ContainerInstance.ElementSave != null) ContainerInstance.SetInitialState();
        ContainerInstance.Name = "ContainerInstance";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        ContainerInstance.AddChild(TextInstance);
        ContainerInstance.AddChild(SubmenuIndicatorInstance);
        this.AddChild(ContainerInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "DarkGray");
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

        this.TextInstance.SetProperty("ColorCategoryState", "White");
        this.TextInstance.SetProperty("StyleCategoryState", "Normal");
        this.TextInstance.Height = 0f;
        this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ((TextRuntime)this.TextInstance).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance.Text = @"Menu Item";
        ((TextRuntime)this.TextInstance).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.Width = 2f;
        this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.TextInstance.X = 0f;
        this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.TextInstance.Y = 0f;
        this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.SubmenuIndicatorInstance.SetProperty("ColorCategoryState", "White");
        this.SubmenuIndicatorInstance.SetProperty("StyleCategoryState", "Normal");
        this.SubmenuIndicatorInstance.Height = 0f;
        this.SubmenuIndicatorInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        ((TextRuntime)this.SubmenuIndicatorInstance).HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.SubmenuIndicatorInstance.Text = @">";
        ((TextRuntime)this.SubmenuIndicatorInstance).VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.SubmenuIndicatorInstance.Width = 2f;
        this.SubmenuIndicatorInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.SubmenuIndicatorInstance.X = 8f;
        this.SubmenuIndicatorInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.SubmenuIndicatorInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.SubmenuIndicatorInstance.Y = 0f;
        this.SubmenuIndicatorInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.SubmenuIndicatorInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.ContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.ContainerInstance.Height = 0f;
        this.ContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.ContainerInstance.Width = 0f;
        this.ContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;

    }
    partial void CustomInitialize();
}
