//Code for Controls/WindowStandard (Container)
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
partial class WindowStandard : global::MonoGameGum.Forms.Window
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/WindowStandard");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new WindowStandard(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(WindowStandard)] = template;
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::MonoGameGum.Forms.Window)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/WindowStandard", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public NineSliceRuntime Background { get; protected set; }
    public Panel InnerPanelInstance { get; protected set; }
    public Panel TitleBarInstance { get; protected set; }
    public Panel BorderTopLeftInstance { get; protected set; }
    public Panel BorderTopRightInstance { get; protected set; }
    public Panel BorderBottomLeftInstance { get; protected set; }
    public Panel BorderBottomRightInstance { get; protected set; }
    public Panel BorderTopInstance { get; protected set; }
    public Panel BorderBottomInstance { get; protected set; }
    public Panel BorderLeftInstance { get; protected set; }
    public Panel BorderRightInstance { get; protected set; }

    public WindowStandard(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public WindowStandard() : base(new ContainerRuntime())
    {

        this.Visual.MinHeight = 10f;
        this.Visual.MinWidth = 10f;

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
        InnerPanelInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        InnerPanelInstance.Name = "InnerPanelInstance";
        TitleBarInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        TitleBarInstance.Name = "TitleBarInstance";
        BorderTopLeftInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        BorderTopLeftInstance.Name = "BorderTopLeftInstance";
        BorderTopRightInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        BorderTopRightInstance.Name = "BorderTopRightInstance";
        BorderBottomLeftInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        BorderBottomLeftInstance.Name = "BorderBottomLeftInstance";
        BorderBottomRightInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        BorderBottomRightInstance.Name = "BorderBottomRightInstance";
        BorderTopInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        BorderTopInstance.Name = "BorderTopInstance";
        BorderBottomInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        BorderBottomInstance.Name = "BorderBottomInstance";
        BorderLeftInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        BorderLeftInstance.Name = "BorderLeftInstance";
        BorderRightInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Panel();
        BorderRightInstance.Name = "BorderRightInstance";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        this.AddChild(InnerPanelInstance);
        this.AddChild(TitleBarInstance);
        this.AddChild(BorderTopLeftInstance);
        this.AddChild(BorderTopRightInstance);
        this.AddChild(BorderBottomLeftInstance);
        this.AddChild(BorderBottomRightInstance);
        this.AddChild(BorderTopInstance);
        this.AddChild(BorderBottomInstance);
        this.AddChild(BorderLeftInstance);
        this.AddChild(BorderRightInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "Primary");
        this.Background.SetProperty("StyleCategoryState", "Panel");

        this.InnerPanelInstance.Visual.Height = 0f;
        this.InnerPanelInstance.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.InnerPanelInstance.Visual.Width = 0f;
        this.InnerPanelInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.InnerPanelInstance.Visual.X = 0f;
        this.InnerPanelInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.InnerPanelInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.InnerPanelInstance.Visual.Y = 0f;
        this.InnerPanelInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.InnerPanelInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.TitleBarInstance.Visual.Height = 24f;
        this.TitleBarInstance.Visual.Width = 0f;
        this.TitleBarInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TitleBarInstance.Visual.X = 0f;
        this.TitleBarInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TitleBarInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.TitleBarInstance.Visual.Y = 0f;
        this.TitleBarInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.TitleBarInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.BorderTopLeftInstance.Visual.Height = 10f;
        this.BorderTopLeftInstance.Visual.Width = 10f;
        this.BorderTopLeftInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.BorderTopLeftInstance.Visual.X = 0f;
        this.BorderTopLeftInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.BorderTopLeftInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.BorderTopLeftInstance.Visual.Y = 0f;
        this.BorderTopLeftInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.BorderTopLeftInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.BorderTopRightInstance.Visual.Height = 10f;
        this.BorderTopRightInstance.Visual.Width = 10f;
        this.BorderTopRightInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.BorderTopRightInstance.Visual.X = 0f;
        this.BorderTopRightInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.BorderTopRightInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.BorderTopRightInstance.Visual.Y = 0f;
        this.BorderTopRightInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.BorderTopRightInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.BorderBottomLeftInstance.Visual.Height = 10f;
        this.BorderBottomLeftInstance.Visual.Width = 10f;
        this.BorderBottomLeftInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.BorderBottomLeftInstance.Visual.X = 0f;
        this.BorderBottomLeftInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.BorderBottomLeftInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.BorderBottomLeftInstance.Visual.Y = 0f;
        this.BorderBottomLeftInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.BorderBottomLeftInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.BorderBottomRightInstance.Visual.Height = 10f;
        this.BorderBottomRightInstance.Visual.Width = 10f;
        this.BorderBottomRightInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.BorderBottomRightInstance.Visual.X = 0f;
        this.BorderBottomRightInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.BorderBottomRightInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.BorderBottomRightInstance.Visual.Y = 0f;
        this.BorderBottomRightInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.BorderBottomRightInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.BorderTopInstance.Visual.Height = 10f;
        this.BorderTopInstance.Visual.Width = -20f;
        this.BorderTopInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.BorderTopInstance.Visual.X = 0f;
        this.BorderTopInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.BorderTopInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.BorderTopInstance.Visual.Y = 0f;
        this.BorderTopInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.BorderTopInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.BorderBottomInstance.Visual.Height = 10f;
        this.BorderBottomInstance.Visual.Width = -20f;
        this.BorderBottomInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.BorderBottomInstance.Visual.X = 0f;
        this.BorderBottomInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.BorderBottomInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.BorderBottomInstance.Visual.Y = 0f;
        this.BorderBottomInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.BorderBottomInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.BorderLeftInstance.Visual.Height = -20f;
        this.BorderLeftInstance.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.BorderLeftInstance.Visual.Width = 10f;
        this.BorderLeftInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.BorderLeftInstance.Visual.X = 0f;
        this.BorderLeftInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.BorderLeftInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.BorderLeftInstance.Visual.Y = 0f;
        this.BorderLeftInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.BorderLeftInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.BorderRightInstance.Visual.Height = -20f;
        this.BorderRightInstance.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.BorderRightInstance.Visual.Width = 10f;
        this.BorderRightInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.BorderRightInstance.Visual.X = 0f;
        this.BorderRightInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.BorderRightInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.BorderRightInstance.Visual.Y = 0f;
        this.BorderRightInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.BorderRightInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    partial void CustomInitialize();
}
