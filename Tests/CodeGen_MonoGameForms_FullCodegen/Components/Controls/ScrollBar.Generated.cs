//Code for Controls/ScrollBar (Container)
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
partial class ScrollBar : global::MonoGameGum.Forms.Controls.ScrollBar
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/ScrollBar");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new ScrollBar(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(ScrollBar)] = template;
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::MonoGameGum.Forms.Controls.ScrollBar)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/ScrollBar", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum ScrollBarCategory
    {
    }

    private ScrollBarCategory? _scrollBarCategoryState;
    public ScrollBarCategory? ScrollBarCategoryState
    {
        get => _scrollBarCategoryState;
        set
        {
            _scrollBarCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                }
            }
        }
    }
    public ButtonIcon UpButtonInstance { get; protected set; }
    public ButtonIcon DownButtonInstance { get; protected set; }
    public ContainerRuntime TrackInstance { get; protected set; }
    public NineSliceRuntime TrackBackground { get; protected set; }
    public ButtonStandard ThumbInstance { get; protected set; }

    public ScrollBar(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public ScrollBar() : base(new ContainerRuntime())
    {

        this.Visual.Height = 0f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
         
        this.Visual.Width = 24f;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
        UpButtonInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonIcon();
        UpButtonInstance.Name = "UpButtonInstance";
        DownButtonInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonIcon();
        DownButtonInstance.Name = "DownButtonInstance";
        TrackInstance = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        TrackInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (TrackInstance.ElementSave != null) TrackInstance.AddStatesAndCategoriesRecursivelyToGue(TrackInstance.ElementSave);
        if (TrackInstance.ElementSave != null) TrackInstance.SetInitialState();
        TrackInstance.Name = "TrackInstance";
        TrackBackground = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        TrackBackground.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (TrackBackground.ElementSave != null) TrackBackground.AddStatesAndCategoriesRecursivelyToGue(TrackBackground.ElementSave);
        if (TrackBackground.ElementSave != null) TrackBackground.SetInitialState();
        TrackBackground.Name = "TrackBackground";
        ThumbInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard();
        ThumbInstance.Name = "ThumbInstance";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(UpButtonInstance);
        this.AddChild(DownButtonInstance);
        this.AddChild(TrackInstance);
        TrackInstance.AddChild(TrackBackground);
        TrackInstance.AddChild(ThumbInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.UpButtonInstance.IconCategory = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Arrow1;
        this.UpButtonInstance.Visual.Height = 24f;
        this.UpButtonInstance.Visual.Rotation = 90f;
        this.UpButtonInstance.Visual.Width = 24f;
        this.UpButtonInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.UpButtonInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;

        this.DownButtonInstance.IconCategory = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Arrow1;
        this.DownButtonInstance.Visual.Height = 24f;
        this.DownButtonInstance.Visual.Rotation = -90f;
        this.DownButtonInstance.Visual.Width = 24f;
        this.DownButtonInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.DownButtonInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.DownButtonInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.TrackInstance.Height = -48f;
        this.TrackInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TrackInstance.Width = 0f;
        this.TrackInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TrackInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TrackInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.TrackInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TrackInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.TrackBackground.SetProperty("ColorCategoryState", "Gray");
        this.TrackBackground.SetProperty("StyleCategoryState", "Solid");
        this.TrackBackground.Height = 0f;
        this.TrackBackground.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TrackBackground.Width = 0f;
        this.TrackBackground.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TrackBackground.X = 0f;
        this.TrackBackground.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.TrackBackground.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.TrackBackground.Y = 0f;
        this.TrackBackground.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TrackBackground.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.ThumbInstance.ButtonDisplayText = @"";
        this.ThumbInstance.Visual.Width = 0f;
        this.ThumbInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ThumbInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ThumbInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.ThumbInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.ThumbInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    partial void CustomInitialize();
}
