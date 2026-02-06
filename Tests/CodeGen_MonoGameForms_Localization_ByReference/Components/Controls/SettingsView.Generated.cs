//Code for Controls/SettingsView (Container)
using CodeGen_MonoGameForms_Localization_ByReference.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_Localization_ByReference.Components.Controls;
partial class SettingsView : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/SettingsView");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/SettingsView - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new SettingsView(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(SettingsView)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/SettingsView", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public CheckBox FullscreenCheckboxInstance { get; protected set; }
    public Label MusicVolumeLabel { get; protected set; }
    public Slider MusicSliderInstance { get; protected set; }
    public Label SoundVolumeLabel { get; protected set; }
    public Slider SoundSliderInstance { get; protected set; }

    public SettingsView(InteractiveGue visual) : base(visual)
    {
    }
    public SettingsView()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        FullscreenCheckboxInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<CheckBox>(this.Visual,"FullscreenCheckboxInstance");
        MusicVolumeLabel = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Label>(this.Visual,"MusicVolumeLabel");
        MusicSliderInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Slider>(this.Visual,"MusicSliderInstance");
        SoundVolumeLabel = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Label>(this.Visual,"SoundVolumeLabel");
        SoundSliderInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Slider>(this.Visual,"SoundSliderInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    public void ApplyLocalization()
    {
    }
    partial void CustomInitialize();
}
