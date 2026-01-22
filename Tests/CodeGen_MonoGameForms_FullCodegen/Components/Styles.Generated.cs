//Code for Styles (Container)
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_FullCodegen.Components;
partial class Styles : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Styles");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Styles - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Styles(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Styles)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Styles", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public ContainerRuntime Colors { get; protected set; }
    public ColoredRectangleRuntime Black { get; protected set; }
    public ColoredRectangleRuntime DarkGray { get; protected set; }
    public ColoredRectangleRuntime Gray { get; protected set; }
    public ColoredRectangleRuntime LightGray { get; protected set; }
    public ColoredRectangleRuntime White { get; protected set; }
    public ColoredRectangleRuntime PrimaryDark { get; protected set; }
    public ColoredRectangleRuntime Primary { get; protected set; }
    public ColoredRectangleRuntime PrimaryLight { get; protected set; }
    public ColoredRectangleRuntime Success { get; protected set; }
    public ColoredRectangleRuntime Warning { get; protected set; }
    public ColoredRectangleRuntime Danger { get; protected set; }
    public ColoredRectangleRuntime Accent { get; protected set; }
    public TextRuntime Tiny { get; protected set; }
    public TextRuntime Small { get; protected set; }
    public TextRuntime Normal { get; protected set; }
    public TextRuntime Emphasis { get; protected set; }
    public TextRuntime Strong { get; protected set; }
    public TextRuntime H3 { get; protected set; }
    public TextRuntime H2 { get; protected set; }
    public TextRuntime H1 { get; protected set; }
    public ContainerRuntime TextStyles { get; protected set; }
    public TextRuntime Title { get; protected set; }

    public Styles(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public Styles() : base(new ContainerRuntime())
    {

        this.Visual.Height = -0f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Visual.Width = -0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Visual.X = 0f;
        this.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
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
        Colors = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        Colors.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (Colors.ElementSave != null) Colors.AddStatesAndCategoriesRecursivelyToGue(Colors.ElementSave);
        if (Colors.ElementSave != null) Colors.SetInitialState();
        Colors.Name = "Colors";
        Black = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        Black.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (Black.ElementSave != null) Black.AddStatesAndCategoriesRecursivelyToGue(Black.ElementSave);
        if (Black.ElementSave != null) Black.SetInitialState();
        Black.Name = "Black";
        DarkGray = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        DarkGray.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (DarkGray.ElementSave != null) DarkGray.AddStatesAndCategoriesRecursivelyToGue(DarkGray.ElementSave);
        if (DarkGray.ElementSave != null) DarkGray.SetInitialState();
        DarkGray.Name = "DarkGray";
        Gray = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        Gray.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (Gray.ElementSave != null) Gray.AddStatesAndCategoriesRecursivelyToGue(Gray.ElementSave);
        if (Gray.ElementSave != null) Gray.SetInitialState();
        Gray.Name = "Gray";
        LightGray = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        LightGray.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (LightGray.ElementSave != null) LightGray.AddStatesAndCategoriesRecursivelyToGue(LightGray.ElementSave);
        if (LightGray.ElementSave != null) LightGray.SetInitialState();
        LightGray.Name = "LightGray";
        White = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        White.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (White.ElementSave != null) White.AddStatesAndCategoriesRecursivelyToGue(White.ElementSave);
        if (White.ElementSave != null) White.SetInitialState();
        White.Name = "White";
        PrimaryDark = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        PrimaryDark.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (PrimaryDark.ElementSave != null) PrimaryDark.AddStatesAndCategoriesRecursivelyToGue(PrimaryDark.ElementSave);
        if (PrimaryDark.ElementSave != null) PrimaryDark.SetInitialState();
        PrimaryDark.Name = "PrimaryDark";
        Primary = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        Primary.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (Primary.ElementSave != null) Primary.AddStatesAndCategoriesRecursivelyToGue(Primary.ElementSave);
        if (Primary.ElementSave != null) Primary.SetInitialState();
        Primary.Name = "Primary";
        PrimaryLight = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        PrimaryLight.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (PrimaryLight.ElementSave != null) PrimaryLight.AddStatesAndCategoriesRecursivelyToGue(PrimaryLight.ElementSave);
        if (PrimaryLight.ElementSave != null) PrimaryLight.SetInitialState();
        PrimaryLight.Name = "PrimaryLight";
        Success = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        Success.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (Success.ElementSave != null) Success.AddStatesAndCategoriesRecursivelyToGue(Success.ElementSave);
        if (Success.ElementSave != null) Success.SetInitialState();
        Success.Name = "Success";
        Warning = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        Warning.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (Warning.ElementSave != null) Warning.AddStatesAndCategoriesRecursivelyToGue(Warning.ElementSave);
        if (Warning.ElementSave != null) Warning.SetInitialState();
        Warning.Name = "Warning";
        Danger = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        Danger.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (Danger.ElementSave != null) Danger.AddStatesAndCategoriesRecursivelyToGue(Danger.ElementSave);
        if (Danger.ElementSave != null) Danger.SetInitialState();
        Danger.Name = "Danger";
        Accent = new global::MonoGameGum.GueDeriving.ColoredRectangleRuntime();
        Accent.ElementSave = ObjectFinder.Self.GetStandardElement("ColoredRectangle");
        if (Accent.ElementSave != null) Accent.AddStatesAndCategoriesRecursivelyToGue(Accent.ElementSave);
        if (Accent.ElementSave != null) Accent.SetInitialState();
        Accent.Name = "Accent";
        Tiny = new global::MonoGameGum.GueDeriving.TextRuntime();
        Tiny.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (Tiny.ElementSave != null) Tiny.AddStatesAndCategoriesRecursivelyToGue(Tiny.ElementSave);
        if (Tiny.ElementSave != null) Tiny.SetInitialState();
        Tiny.Name = "Tiny";
        Small = new global::MonoGameGum.GueDeriving.TextRuntime();
        Small.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (Small.ElementSave != null) Small.AddStatesAndCategoriesRecursivelyToGue(Small.ElementSave);
        if (Small.ElementSave != null) Small.SetInitialState();
        Small.Name = "Small";
        Normal = new global::MonoGameGum.GueDeriving.TextRuntime();
        Normal.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (Normal.ElementSave != null) Normal.AddStatesAndCategoriesRecursivelyToGue(Normal.ElementSave);
        if (Normal.ElementSave != null) Normal.SetInitialState();
        Normal.Name = "Normal";
        Emphasis = new global::MonoGameGum.GueDeriving.TextRuntime();
        Emphasis.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (Emphasis.ElementSave != null) Emphasis.AddStatesAndCategoriesRecursivelyToGue(Emphasis.ElementSave);
        if (Emphasis.ElementSave != null) Emphasis.SetInitialState();
        Emphasis.Name = "Emphasis";
        Strong = new global::MonoGameGum.GueDeriving.TextRuntime();
        Strong.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (Strong.ElementSave != null) Strong.AddStatesAndCategoriesRecursivelyToGue(Strong.ElementSave);
        if (Strong.ElementSave != null) Strong.SetInitialState();
        Strong.Name = "Strong";
        H3 = new global::MonoGameGum.GueDeriving.TextRuntime();
        H3.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (H3.ElementSave != null) H3.AddStatesAndCategoriesRecursivelyToGue(H3.ElementSave);
        if (H3.ElementSave != null) H3.SetInitialState();
        H3.Name = "H3";
        H2 = new global::MonoGameGum.GueDeriving.TextRuntime();
        H2.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (H2.ElementSave != null) H2.AddStatesAndCategoriesRecursivelyToGue(H2.ElementSave);
        if (H2.ElementSave != null) H2.SetInitialState();
        H2.Name = "H2";
        H1 = new global::MonoGameGum.GueDeriving.TextRuntime();
        H1.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (H1.ElementSave != null) H1.AddStatesAndCategoriesRecursivelyToGue(H1.ElementSave);
        if (H1.ElementSave != null) H1.SetInitialState();
        H1.Name = "H1";
        TextStyles = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        TextStyles.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (TextStyles.ElementSave != null) TextStyles.AddStatesAndCategoriesRecursivelyToGue(TextStyles.ElementSave);
        if (TextStyles.ElementSave != null) TextStyles.SetInitialState();
        TextStyles.Name = "TextStyles";
        Title = new global::MonoGameGum.GueDeriving.TextRuntime();
        Title.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (Title.ElementSave != null) Title.AddStatesAndCategoriesRecursivelyToGue(Title.ElementSave);
        if (Title.ElementSave != null) Title.SetInitialState();
        Title.Name = "Title";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Colors);
        Colors.AddChild(Black);
        Colors.AddChild(DarkGray);
        Colors.AddChild(Gray);
        Colors.AddChild(LightGray);
        Colors.AddChild(White);
        Colors.AddChild(PrimaryDark);
        Colors.AddChild(Primary);
        Colors.AddChild(PrimaryLight);
        Colors.AddChild(Success);
        Colors.AddChild(Warning);
        Colors.AddChild(Danger);
        Colors.AddChild(Accent);
        TextStyles.AddChild(Tiny);
        TextStyles.AddChild(Small);
        TextStyles.AddChild(Normal);
        TextStyles.AddChild(Emphasis);
        TextStyles.AddChild(Strong);
        TextStyles.AddChild(H3);
        TextStyles.AddChild(H2);
        TextStyles.AddChild(H1);
        this.AddChild(TextStyles);
        TextStyles.AddChild(Title);
    }
    private void ApplyDefaultVariables()
    {
        this.Colors.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.Colors.Height = 69f;
        this.Colors.StackSpacing = 4f;
        this.Colors.Width = -0f;
        this.Colors.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Colors.X = 0f;
        this.Colors.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Colors.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Colors.Y = 0f;
        this.Colors.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.Colors.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.Black.Blue = 0;
        this.Black.Green = 0;
        this.Black.Red = 0;

        this.DarkGray.Blue = 80;
        this.DarkGray.Green = 70;
        this.DarkGray.Red = 70;

        this.Gray.Blue = 130;
        this.Gray.Green = 130;
        this.Gray.Red = 130;

        this.LightGray.Blue = 170;
        this.LightGray.Green = 170;
        this.LightGray.Red = 170;


        this.PrimaryDark.Blue = 137;
        this.PrimaryDark.Green = 120;
        this.PrimaryDark.Red = 4;

        this.Primary.Blue = 177;
        this.Primary.Green = 159;
        this.Primary.Red = 6;

        this.PrimaryLight.Blue = 193;
        this.PrimaryLight.Green = 180;
        this.PrimaryLight.Red = 74;

        this.Success.Blue = 48;
        this.Success.Green = 167;
        this.Success.Red = 62;

        this.Warning.Blue = 25;
        this.Warning.Green = 171;
        this.Warning.Red = 232;

        this.Danger.Blue = 41;
        this.Danger.Green = 18;
        this.Danger.Red = 212;

        this.Accent.Blue = 138;
        this.Accent.Green = 48;
        this.Accent.Red = 140;

        this.Tiny.FontSize = 10;
        this.Tiny.Text = @"Tiny";

        this.Small.FontSize = 12;
        this.Small.Text = @"Small";

        this.Normal.FontSize = 14;
        this.Normal.Text = @"Normal";

        this.Emphasis.FontSize = 14;
        this.Emphasis.IsItalic = true;
        this.Emphasis.Text = @"Emphasis";

        this.Strong.FontSize = 14;
        this.Strong.IsBold = true;
        this.Strong.Text = @"Strong";

        this.H3.FontSize = 16;
        this.H3.IsBold = true;
        this.H3.Text = @"H3";

        this.H2.FontSize = 18;
        this.H2.IsBold = true;
        this.H2.Text = @"H2";

        this.H1.FontSize = 22;
        this.H1.IsBold = true;
        this.H1.Text = @"H1";

        this.TextStyles.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.TextStyles.Height = 20f;
        this.TextStyles.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.TextStyles.Width = 308f;
        this.TextStyles.Y = 81f;

        this.Title.FontSize = 28;
        this.Title.IsBold = true;
        this.Title.Text = @"Title";

    }
    partial void CustomInitialize();
}
