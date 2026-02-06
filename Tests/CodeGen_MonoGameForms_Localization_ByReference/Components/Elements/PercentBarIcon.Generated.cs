//Code for Elements/PercentBarIcon (Container)
using CodeGen_MonoGameForms_Localization_ByReference.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_Localization_ByReference.Components.Elements;
partial class PercentBarIcon : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Elements/PercentBarIcon");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Elements/PercentBarIcon - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new PercentBarIcon(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(PercentBarIcon)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Elements/PercentBarIcon", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum BarDecorCategory
    {
        None,
        CautionLines,
        VerticalLines,
    }

    BarDecorCategory? _barDecorCategoryState;
    public BarDecorCategory? BarDecorCategoryState
    {
        get => _barDecorCategoryState;
        set
        {
            _barDecorCategoryState = value;
            if(value != null)
            {
                if(Visual.Categories.ContainsKey("BarDecorCategory"))
                {
                    var category = Visual.Categories["BarDecorCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "BarDecorCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public Icon IconInstance { get; protected set; }
    public NineSliceRuntime BarContainer { get; protected set; }
    public NineSliceRuntime Bar { get; protected set; }
    public CautionLines CautionLinesInstance { get; protected set; }
    public VerticalLines VerticalLinesInstance { get; protected set; }


    public float BarPercent
    {
        get => Bar.Width;
        set => Bar.Width = value;
    }

    public Icon.IconCategory? BarIcon
    {
        get => IconInstance.IconCategoryState;
        set => IconInstance.IconCategoryState = value;
    }


    public PercentBarIcon(InteractiveGue visual) : base(visual)
    {
    }
    public PercentBarIcon()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        IconInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance");
        BarContainer = this.Visual?.GetGraphicalUiElementByName("BarContainer") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        Bar = this.Visual?.GetGraphicalUiElementByName("Bar") as global::MonoGameGum.GueDeriving.NineSliceRuntime;
        CautionLinesInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<CautionLines>(this.Visual,"CautionLinesInstance");
        VerticalLinesInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<VerticalLines>(this.Visual,"VerticalLinesInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    public void ApplyLocalization()
    {
    }
    partial void CustomInitialize();
}
