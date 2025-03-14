//Code for Controls/SettingsView (Container)
using GumRuntime;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class SettingsViewRuntime:ContainerRuntime
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
            }

            this.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
             

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            FullscreenCheckboxInstance = new CheckBoxRuntime();
            FullscreenCheckboxInstance.Name = "FullscreenCheckboxInstance";
            MusicVolumeLabel = new LabelRuntime();
            MusicVolumeLabel.Name = "MusicVolumeLabel";
            MusicSliderInstance = new SliderRuntime();
            MusicSliderInstance.Name = "MusicSliderInstance";
            SoundVolumeLabel = new LabelRuntime();
            SoundVolumeLabel.Name = "SoundVolumeLabel";
            SoundSliderInstance = new SliderRuntime();
            SoundSliderInstance.Name = "SoundSliderInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(FullscreenCheckboxInstance);
            this.Children.Add(MusicVolumeLabel);
            this.Children.Add(MusicSliderInstance);
            this.Children.Add(SoundVolumeLabel);
            this.Children.Add(SoundSliderInstance);
        }
        private void ApplyDefaultVariables()
        {
            this.FullscreenCheckboxInstance.CheckboxDisplayText = @"Fullscreen";
            this.FullscreenCheckboxInstance.Width = 0f;
            this.FullscreenCheckboxInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.FullscreenCheckboxInstance.Y = 0f;

            this.MusicVolumeLabel.LabelText = @"Music Volume";
            this.MusicVolumeLabel.Width = 0f;
            this.MusicVolumeLabel.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.MusicVolumeLabel.Y = 12f;

            this.MusicSliderInstance.Width = 0f;
            this.MusicSliderInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.SoundVolumeLabel.LabelText = @"Sound Effect Volume";
            this.SoundVolumeLabel.Width = 0f;
            this.SoundVolumeLabel.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.SoundVolumeLabel.Y = 12f;

            this.SoundSliderInstance.Width = 0f;
            this.SoundSliderInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

        }
        partial void CustomInitialize();
    }
}
