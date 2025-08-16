//Code for Controls/SettingsView (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGenProject.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGenProject.Components.Controls;
partial class SettingsView : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/SettingsView");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new SettingsView(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(SettingsView)] = template;
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
        FullscreenCheckboxInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<CheckBox>(this.Visual,"FullscreenCheckboxInstance");
        MusicVolumeLabel = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Label>(this.Visual,"MusicVolumeLabel");
        MusicSliderInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Slider>(this.Visual,"MusicSliderInstance");
        SoundVolumeLabel = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Label>(this.Visual,"SoundVolumeLabel");
        SoundSliderInstance = global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Slider>(this.Visual,"SoundSliderInstance");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
