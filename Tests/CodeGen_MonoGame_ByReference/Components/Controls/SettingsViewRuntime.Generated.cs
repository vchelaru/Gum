//Code for Controls/SettingsView (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGame_ByReference.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class SettingsViewRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/SettingsView", typeof(SettingsViewRuntime));
    }
    public CheckBoxRuntime FullscreenCheckboxInstance { get; protected set; }
    public LabelRuntime MusicVolumeLabel { get; protected set; }
    public SliderRuntime MusicSliderInstance { get; protected set; }
    public LabelRuntime SoundVolumeLabel { get; protected set; }
    public SliderRuntime SoundSliderInstance { get; protected set; }

    public SettingsViewRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/SettingsView");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        FullscreenCheckboxInstance = this.GetGraphicalUiElementByName("FullscreenCheckboxInstance") as CheckBoxRuntime;
        MusicVolumeLabel = this.GetGraphicalUiElementByName("MusicVolumeLabel") as LabelRuntime;
        MusicSliderInstance = this.GetGraphicalUiElementByName("MusicSliderInstance") as SliderRuntime;
        SoundVolumeLabel = this.GetGraphicalUiElementByName("SoundVolumeLabel") as LabelRuntime;
        SoundSliderInstance = this.GetGraphicalUiElementByName("SoundSliderInstance") as SliderRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
