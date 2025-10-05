//Code for Controls/SettingsView (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
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
        InitializeInstances();
        CustomInitialize();
    }
    public SettingsView() : base(new ContainerRuntime())
    {

        this.Visual.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
         

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        FullscreenCheckboxInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.CheckBox();
        FullscreenCheckboxInstance.Name = "FullscreenCheckboxInstance";
        MusicVolumeLabel = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        MusicVolumeLabel.Name = "MusicVolumeLabel";
        MusicSliderInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Slider();
        MusicSliderInstance.Name = "MusicSliderInstance";
        SoundVolumeLabel = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        SoundVolumeLabel.Name = "SoundVolumeLabel";
        SoundSliderInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Slider();
        SoundSliderInstance.Name = "SoundSliderInstance";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        this.AddChild(FullscreenCheckboxInstance);
        this.AddChild(MusicVolumeLabel);
        this.AddChild(MusicSliderInstance);
        this.AddChild(SoundVolumeLabel);
        this.AddChild(SoundSliderInstance);
    }
    private void ApplyDefaultVariables()
    {
        this.FullscreenCheckboxInstance.Text = @"Fullscreen";
        this.FullscreenCheckboxInstance.Visual.Width = 0f;
        this.FullscreenCheckboxInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.FullscreenCheckboxInstance.Visual.Y = 0f;

        this.MusicVolumeLabel.Text = @"Music Volume";
        this.MusicVolumeLabel.Visual.Width = 0f;
        this.MusicVolumeLabel.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.MusicVolumeLabel.Visual.Y = 12f;

        this.MusicSliderInstance.Visual.Width = 0f;
        this.MusicSliderInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.SoundVolumeLabel.Text = @"Sound Effect Volume";
        this.SoundVolumeLabel.Visual.Width = 0f;
        this.SoundVolumeLabel.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.SoundVolumeLabel.Visual.Y = 12f;

        this.SoundSliderInstance.Visual.Width = 0f;
        this.SoundSliderInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

    }
    partial void CustomInitialize();
}
