//Code for Controls/PasswordBox (Container)
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
partial class PasswordBox : global::MonoGameGum.Forms.Controls.PasswordBox
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/PasswordBox");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new PasswordBox(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(PasswordBox)] = template;
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(global::MonoGameGum.Forms.Controls.PasswordBox)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/PasswordBox", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum PasswordBoxCategory
    {
        Enabled,
        Disabled,
        Highlighted,
        Selected,
    }

    PasswordBoxCategory? mPasswordBoxCategoryState;
    public PasswordBoxCategory? PasswordBoxCategoryState
    {
        get => mPasswordBoxCategoryState;
        set
        {
            mPasswordBoxCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case PasswordBoxCategory.Enabled:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = false;
                        this.PlaceholderTextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case PasswordBoxCategory.Disabled:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = false;
                        this.PlaceholderTextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                    case PasswordBoxCategory.Highlighted:
                        this.Background.SetProperty("ColorCategoryState", "Gray");
                        this.FocusedIndicator.Visible = false;
                        this.PlaceholderTextInstance.SetProperty("ColorCategoryState", "DarkGray");
                        break;
                    case PasswordBoxCategory.Selected:
                        this.Background.SetProperty("ColorCategoryState", "DarkGray");
                        this.FocusedIndicator.Visible = true;
                        this.PlaceholderTextInstance.SetProperty("ColorCategoryState", "Gray");
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public NineSliceRuntime SelectionInstance { get; protected set; }
    public TextRuntime TextInstance { get; protected set; }
    public TextRuntime PlaceholderTextInstance { get; protected set; }
    public NineSliceRuntime FocusedIndicator { get; protected set; }
    public SpriteRuntime CaretInstance { get; protected set; }

    public string PlaceholderText
    {
        get => PlaceholderTextInstance.Text;
        set => PlaceholderTextInstance.Text = value;
    }

    public PasswordBox(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public PasswordBox() : base(new ContainerRuntime())
    {

        this.Visual.Height = 24f;
         
        this.Visual.Width = 256f;

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
        SelectionInstance = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        SelectionInstance.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (SelectionInstance.ElementSave != null) SelectionInstance.AddStatesAndCategoriesRecursivelyToGue(SelectionInstance.ElementSave);
        if (SelectionInstance.ElementSave != null) SelectionInstance.SetInitialState();
        SelectionInstance.Name = "SelectionInstance";
        TextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        TextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TextInstance.ElementSave != null) TextInstance.AddStatesAndCategoriesRecursivelyToGue(TextInstance.ElementSave);
        if (TextInstance.ElementSave != null) TextInstance.SetInitialState();
        TextInstance.Name = "TextInstance";
        PlaceholderTextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        PlaceholderTextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (PlaceholderTextInstance.ElementSave != null) PlaceholderTextInstance.AddStatesAndCategoriesRecursivelyToGue(PlaceholderTextInstance.ElementSave);
        if (PlaceholderTextInstance.ElementSave != null) PlaceholderTextInstance.SetInitialState();
        PlaceholderTextInstance.Name = "PlaceholderTextInstance";
        FocusedIndicator = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        FocusedIndicator.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.AddStatesAndCategoriesRecursivelyToGue(FocusedIndicator.ElementSave);
        if (FocusedIndicator.ElementSave != null) FocusedIndicator.SetInitialState();
        FocusedIndicator.Name = "FocusedIndicator";
        CaretInstance = new global::MonoGameGum.GueDeriving.SpriteRuntime();
        CaretInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Sprite");
        if (CaretInstance.ElementSave != null) CaretInstance.AddStatesAndCategoriesRecursivelyToGue(CaretInstance.ElementSave);
        if (CaretInstance.ElementSave != null) CaretInstance.SetInitialState();
        CaretInstance.Name = "CaretInstance";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        this.AddChild(SelectionInstance);
        this.AddChild(TextInstance);
        this.AddChild(PlaceholderTextInstance);
        this.AddChild(FocusedIndicator);
        this.AddChild(CaretInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "DarkGray");
        this.Background.SetProperty("StyleCategoryState", "Bordered");

        this.SelectionInstance.SetProperty("ColorCategoryState", "Accent");
        this.SelectionInstance.Height = -4f;
        this.SelectionInstance.Width = 7f;
        this.SelectionInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.SelectionInstance.X = 15f;
        this.SelectionInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.SelectionInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.SelectionInstance.Y = 0f;

        this.TextInstance.SetProperty("ColorCategoryState", "White");
        this.TextInstance.SetProperty("StyleCategoryState", "Normal");
        this.TextInstance.Height = -4f;
        this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance.Text = @"";
        this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.Width = 0f;
        this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.TextInstance.X = 4f;
        this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.TextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.TextInstance.Y = 0f;
        this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.TextInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.PlaceholderTextInstance.SetProperty("ColorCategoryState", "Gray");
        this.PlaceholderTextInstance.SetProperty("StyleCategoryState", "Normal");
        this.PlaceholderTextInstance.Height = -4f;
        this.PlaceholderTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.PlaceholderTextInstance.Text = @"Password";
        this.PlaceholderTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.PlaceholderTextInstance.Width = -8f;
        this.PlaceholderTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.PlaceholderTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.PlaceholderTextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.PlaceholderTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.PlaceholderTextInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.FocusedIndicator.SetProperty("ColorCategoryState", "Warning");
        this.FocusedIndicator.SetProperty("StyleCategoryState", "Solid");
        this.FocusedIndicator.Height = 2f;
        this.FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.FocusedIndicator.Visible = false;
        this.FocusedIndicator.Y = 2f;
        this.FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.FocusedIndicator.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.CaretInstance.SetProperty("ColorCategoryState", "Primary");
        this.CaretInstance.Height = 14f;
        this.CaretInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.CaretInstance.SourceFileName = @"UISpriteSheet.png";
        this.CaretInstance.TextureAddress = global::Gum.Managers.TextureAddress.Custom;
        this.CaretInstance.TextureHeight = 24;
        this.CaretInstance.TextureLeft = 0;
        this.CaretInstance.TextureTop = 48;
        this.CaretInstance.TextureWidth = 24;
        this.CaretInstance.Width = 1f;
        this.CaretInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.CaretInstance.X = 4f;
        this.CaretInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.CaretInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.CaretInstance.Y = 0f;
        this.CaretInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.CaretInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    partial void CustomInitialize();
}
