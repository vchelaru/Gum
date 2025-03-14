//Code for Elements/PercentBarIcon (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
public partial class PercentBarIconRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Elements/PercentBarIcon", typeof(PercentBarIconRuntime));
    }
    public enum BarDecorCategory
    {
        None,
        CautionLines,
        VerticalLines,
    }

    public BarDecorCategory BarDecorCategoryState
    {
        set
        {
            if(Categories.ContainsKey("BarDecorCategory"))
            {
                var category = Categories["BarDecorCategory"];
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
            else
            {
                var category = ((Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "BarDecorCategory");
                var state = category.States.Find(item => item.Name == value.ToString());
                this.ApplyState(state);
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public IconRuntime IconInstance { get; protected set; }
    public NineSliceRuntime BarContainer { get; protected set; }
    public NineSliceRuntime Bar { get; protected set; }
    public CautionLinesRuntime CautionLinesInstance { get; protected set; }
    public VerticalLinesRuntime VerticalLinesInstance { get; protected set; }

    public string BarColor
    {
        set => Bar.SetProperty("ColorCategoryState", value?.ToString());
    }

    public float BarPercent
    {
        get => Bar.Width;
        set => Bar.Width = value;
    }

    public IconRuntime.IconCategory BarIcon
    {
        set => IconInstance.IconCategoryState = value;
    }

    public string BarIconColor
    {
        set => IconInstance.SetProperty("IconColor", value?.ToString());
    }

    public PercentBarIconRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Elements/PercentBarIcon");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        Background = this.GetGraphicalUiElementByName("Background") as NineSliceRuntime;
        IconInstance = this.GetGraphicalUiElementByName("IconInstance") as IconRuntime;
        BarContainer = this.GetGraphicalUiElementByName("BarContainer") as NineSliceRuntime;
        Bar = this.GetGraphicalUiElementByName("Bar") as NineSliceRuntime;
        CautionLinesInstance = this.GetGraphicalUiElementByName("CautionLinesInstance") as CautionLinesRuntime;
        VerticalLinesInstance = this.GetGraphicalUiElementByName("VerticalLinesInstance") as VerticalLinesRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
