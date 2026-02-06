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
namespace CodeGen_MonoGameForms_Localization_ByReference.Components;
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
    }
    public Styles()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Colors = this.Visual?.GetGraphicalUiElementByName("Colors") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Black = this.Visual?.GetGraphicalUiElementByName("Black") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        DarkGray = this.Visual?.GetGraphicalUiElementByName("DarkGray") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        Gray = this.Visual?.GetGraphicalUiElementByName("Gray") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        LightGray = this.Visual?.GetGraphicalUiElementByName("LightGray") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        White = this.Visual?.GetGraphicalUiElementByName("White") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        PrimaryDark = this.Visual?.GetGraphicalUiElementByName("PrimaryDark") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        Primary = this.Visual?.GetGraphicalUiElementByName("Primary") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        PrimaryLight = this.Visual?.GetGraphicalUiElementByName("PrimaryLight") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        Success = this.Visual?.GetGraphicalUiElementByName("Success") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        Warning = this.Visual?.GetGraphicalUiElementByName("Warning") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        Danger = this.Visual?.GetGraphicalUiElementByName("Danger") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        Accent = this.Visual?.GetGraphicalUiElementByName("Accent") as global::MonoGameGum.GueDeriving.ColoredRectangleRuntime;
        Tiny = this.Visual?.GetGraphicalUiElementByName("Tiny") as global::MonoGameGum.GueDeriving.TextRuntime;
        Small = this.Visual?.GetGraphicalUiElementByName("Small") as global::MonoGameGum.GueDeriving.TextRuntime;
        Normal = this.Visual?.GetGraphicalUiElementByName("Normal") as global::MonoGameGum.GueDeriving.TextRuntime;
        Emphasis = this.Visual?.GetGraphicalUiElementByName("Emphasis") as global::MonoGameGum.GueDeriving.TextRuntime;
        Strong = this.Visual?.GetGraphicalUiElementByName("Strong") as global::MonoGameGum.GueDeriving.TextRuntime;
        H3 = this.Visual?.GetGraphicalUiElementByName("H3") as global::MonoGameGum.GueDeriving.TextRuntime;
        H2 = this.Visual?.GetGraphicalUiElementByName("H2") as global::MonoGameGum.GueDeriving.TextRuntime;
        H1 = this.Visual?.GetGraphicalUiElementByName("H1") as global::MonoGameGum.GueDeriving.TextRuntime;
        TextStyles = this.Visual?.GetGraphicalUiElementByName("TextStyles") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Title = this.Visual?.GetGraphicalUiElementByName("Title") as global::MonoGameGum.GueDeriving.TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    public void ApplyLocalization()
    {
    }
    partial void CustomInitialize();
}
