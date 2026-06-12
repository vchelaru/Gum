//Code for Styles (Container)
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGame_ByReference.Components;
partial class StylesRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Styles", typeof(StylesRuntime));
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

    public StylesRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Styles");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        Colors = this.GetGraphicalUiElementByName("Colors") as global::Gum.GueDeriving.ContainerRuntime;
        Black = this.GetGraphicalUiElementByName("Black") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        DarkGray = this.GetGraphicalUiElementByName("DarkGray") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        Gray = this.GetGraphicalUiElementByName("Gray") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        LightGray = this.GetGraphicalUiElementByName("LightGray") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        White = this.GetGraphicalUiElementByName("White") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        PrimaryDark = this.GetGraphicalUiElementByName("PrimaryDark") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        Primary = this.GetGraphicalUiElementByName("Primary") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        PrimaryLight = this.GetGraphicalUiElementByName("PrimaryLight") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        Success = this.GetGraphicalUiElementByName("Success") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        Warning = this.GetGraphicalUiElementByName("Warning") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        Danger = this.GetGraphicalUiElementByName("Danger") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        Accent = this.GetGraphicalUiElementByName("Accent") as global::Gum.GueDeriving.ColoredRectangleRuntime;
        Tiny = this.GetGraphicalUiElementByName("Tiny") as global::Gum.GueDeriving.TextRuntime;
        Small = this.GetGraphicalUiElementByName("Small") as global::Gum.GueDeriving.TextRuntime;
        Normal = this.GetGraphicalUiElementByName("Normal") as global::Gum.GueDeriving.TextRuntime;
        Emphasis = this.GetGraphicalUiElementByName("Emphasis") as global::Gum.GueDeriving.TextRuntime;
        Strong = this.GetGraphicalUiElementByName("Strong") as global::Gum.GueDeriving.TextRuntime;
        H3 = this.GetGraphicalUiElementByName("H3") as global::Gum.GueDeriving.TextRuntime;
        H2 = this.GetGraphicalUiElementByName("H2") as global::Gum.GueDeriving.TextRuntime;
        H1 = this.GetGraphicalUiElementByName("H1") as global::Gum.GueDeriving.TextRuntime;
        TextStyles = this.GetGraphicalUiElementByName("TextStyles") as global::Gum.GueDeriving.ContainerRuntime;
        Title = this.GetGraphicalUiElementByName("Title") as global::Gum.GueDeriving.TextRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
