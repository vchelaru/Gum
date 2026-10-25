//Code for DemoScreenGum
using CodeGen_MonoGameForms_FullCodegen.Components;
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using CodeGen_MonoGameForms_FullCodegen.Components.Elements;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGen_MonoGameForms_FullCodegen.Screens;
partial class DemoScreenGum : global::Gum.Forms.Controls.FrameworkElement
{
    #if UNITY_5_3_OR_NEWER
    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    #else
    [System.Runtime.CompilerServices.ModuleInitializer]
    #endif
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::Gum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("DemoScreenGum") ?? throw new System.InvalidOperationException("Could not find an element named DemoScreenGum - did you forget to load a Gum project?");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new DemoScreenGum(visual);
            visual.Width = 0;
            visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            visual.Height = 0;
            visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(DemoScreenGum)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("DemoScreenGum", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public @interface @this { get; protected set; }
    public ContainerRuntime DemoSettingsMenu { get; protected set; }
    public NineSliceRuntime Background { get; protected set; }
    public ContainerRuntime MenuTitle { get; protected set; }
    public ContainerRuntime MenuTitle1 { get; protected set; }
    public ContainerRuntime MenuItems { get; protected set; }
    public TextRuntime TitleText { get; protected set; }
    public TextRuntime TitleText1 { get; protected set; }
    public ButtonClose ButtonCloseInstance1 { get; protected set; }
    public DividerHorizontal DividerInstance { get; protected set; }
    public DividerHorizontal DividerInstance4 { get; protected set; }
    public Label ResolutionLabel { get; protected set; }
    public ListBox ResolutionBox { get; protected set; }
    public ButtonStandard DetectResolutionsButton { get; protected set; }
    public ButtonStandard ShowDialogButton { get; protected set; }
    public ButtonStandard ShowToastButton { get; protected set; }
    public CheckBox FullScreenCheckbox { get; protected set; }
    public DividerHorizontal DividerInstance1 { get; protected set; }
    public Label MusicLabel { get; protected set; }
    public Slider MusicSlider { get; protected set; }
    public Label SoundLabel { get; protected set; }
    public Slider SoundSlider { get; protected set; }
    public DividerHorizontal DividerInstance2 { get; protected set; }
    public Label ControlLabel { get; protected set; }
    public RadioButton RadioButtonInstance { get; protected set; }
    public RadioButton RadioButtonInstance1 { get; protected set; }
    public RadioButton RadioButtonInstance2 { get; protected set; }
    public DividerHorizontal DividerInstance3 { get; protected set; }
    public Label DifficultyLabel { get; protected set; }
    public NineSliceRuntime Background1 { get; protected set; }
    public ComboBox ComboBoxInstance { get; protected set; }
    public ContainerRuntime ButtonContainer { get; protected set; }
    public ButtonConfirm ButtonConfirmInstance { get; protected set; }
    public ButtonConfirm WindowOkButton { get; protected set; }
    public ButtonDeny ButtonDenyInstance { get; protected set; }
    public ContainerRuntime DemoDialog { get; protected set; }
    public ContainerRuntime MarginContainer { get; protected set; }
    public Label LabelInstance { get; protected set; }
    public TextBox TextBoxInstance { get; protected set; }
    public PasswordBox TextBoxInstance1 { get; protected set; }
    public TextBox MultiLineTextBox { get; protected set; }
    public NineSliceRuntime DemoHud { get; protected set; }
    public TextRuntime TitleText2 { get; protected set; }
    public ContainerRuntime MenuTitle2 { get; protected set; }
    public PercentBar PercentBarInstance { get; protected set; }
    public PercentBar HitpointsBar1 { get; protected set; }
    public PercentBar HitpointsBar2 { get; protected set; }
    public PercentBar HitpointsBar3 { get; protected set; }
    public PercentBar PercentBarInstance1 { get; protected set; }
    public PercentBar PercentBarInstance2 { get; protected set; }
    public DividerHorizontal DividerInstance5 { get; protected set; }
    public ButtonClose ButtonCloseInstance2 { get; protected set; }
    public PercentBar PercentBarInstance3 { get; protected set; }
    public PercentBar PercentBarInstance4 { get; protected set; }
    public PercentBarIcon PercentBarInstance5 { get; protected set; }
    public ContainerRuntime ContainerInstance { get; protected set; }
    public TreeView TreeViewInstance { get; protected set; }
    public DialogBox DialogBoxInstance { get; protected set; }
    public WindowStandard WindowStandardInstance { get; protected set; }
    public Label LabelInstance1 { get; protected set; }
    public Spaced_Component Spaced_Component_Instance { get; protected set; }
    public _123Component _123Instance { get; protected set; }

    public DemoScreenGum(InteractiveGue visual) : base(visual)
    {
        InitializeInstancesFromVisual();
        CustomInitialize();
    }
    public DemoScreenGum() : base(new ContainerRuntime())
    {

         

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        @this = new CodeGen_MonoGameForms_FullCodegen.Components.@interface();
        @this.Name = "this";
        DemoSettingsMenu = new global::Gum.GueDeriving.ContainerRuntime();
        DemoSettingsMenu.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (DemoSettingsMenu.ElementSave != null) DemoSettingsMenu.AddStatesAndCategoriesRecursivelyToGue(DemoSettingsMenu.ElementSave);
        if (DemoSettingsMenu.ElementSave != null) DemoSettingsMenu.SetInitialState();
        DemoSettingsMenu.Name = "DemoSettingsMenu";
        Background = new global::Gum.GueDeriving.NineSliceRuntime();
        Background.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (Background.ElementSave != null) Background.AddStatesAndCategoriesRecursivelyToGue(Background.ElementSave);
        if (Background.ElementSave != null) Background.SetInitialState();
        Background.Name = "Background";
        MenuTitle = new global::Gum.GueDeriving.ContainerRuntime();
        MenuTitle.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (MenuTitle.ElementSave != null) MenuTitle.AddStatesAndCategoriesRecursivelyToGue(MenuTitle.ElementSave);
        if (MenuTitle.ElementSave != null) MenuTitle.SetInitialState();
        MenuTitle.Name = "MenuTitle";
        MenuTitle1 = new global::Gum.GueDeriving.ContainerRuntime();
        MenuTitle1.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (MenuTitle1.ElementSave != null) MenuTitle1.AddStatesAndCategoriesRecursivelyToGue(MenuTitle1.ElementSave);
        if (MenuTitle1.ElementSave != null) MenuTitle1.SetInitialState();
        MenuTitle1.Name = "MenuTitle1";
        MenuItems = new global::Gum.GueDeriving.ContainerRuntime();
        MenuItems.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (MenuItems.ElementSave != null) MenuItems.AddStatesAndCategoriesRecursivelyToGue(MenuItems.ElementSave);
        if (MenuItems.ElementSave != null) MenuItems.SetInitialState();
        MenuItems.Name = "MenuItems";
        TitleText = new global::Gum.GueDeriving.TextRuntime();
        TitleText.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TitleText.ElementSave != null) TitleText.AddStatesAndCategoriesRecursivelyToGue(TitleText.ElementSave);
        if (TitleText.ElementSave != null) TitleText.SetInitialState();
        TitleText.Name = "TitleText";
        TitleText1 = new global::Gum.GueDeriving.TextRuntime();
        TitleText1.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TitleText1.ElementSave != null) TitleText1.AddStatesAndCategoriesRecursivelyToGue(TitleText1.ElementSave);
        if (TitleText1.ElementSave != null) TitleText1.SetInitialState();
        TitleText1.Name = "TitleText1";
        ButtonCloseInstance1 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonClose();
        ButtonCloseInstance1.Name = "ButtonCloseInstance1";
        DividerInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.DividerHorizontal();
        DividerInstance.Name = "DividerInstance";
        DividerInstance4 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.DividerHorizontal();
        DividerInstance4.Name = "DividerInstance4";
        ResolutionLabel = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        ResolutionLabel.Name = "ResolutionLabel";
        ResolutionBox = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ListBox();
        ResolutionBox.Name = "ResolutionBox";
        DetectResolutionsButton = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard();
        DetectResolutionsButton.Name = "DetectResolutionsButton";
        ShowDialogButton = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard();
        ShowDialogButton.Name = "ShowDialogButton";
        ShowToastButton = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonStandard();
        ShowToastButton.Name = "ShowToastButton";
        FullScreenCheckbox = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.CheckBox();
        FullScreenCheckbox.Name = "FullScreenCheckbox";
        DividerInstance1 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.DividerHorizontal();
        DividerInstance1.Name = "DividerInstance1";
        MusicLabel = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        MusicLabel.Name = "MusicLabel";
        MusicSlider = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Slider();
        MusicSlider.Name = "MusicSlider";
        SoundLabel = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        SoundLabel.Name = "SoundLabel";
        SoundSlider = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Slider();
        SoundSlider.Name = "SoundSlider";
        DividerInstance2 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.DividerHorizontal();
        DividerInstance2.Name = "DividerInstance2";
        ControlLabel = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        ControlLabel.Name = "ControlLabel";
        RadioButtonInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.RadioButton();
        RadioButtonInstance.Name = "RadioButtonInstance";
        RadioButtonInstance1 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.RadioButton();
        RadioButtonInstance1.Name = "RadioButtonInstance1";
        RadioButtonInstance2 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.RadioButton();
        RadioButtonInstance2.Name = "RadioButtonInstance2";
        DividerInstance3 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.DividerHorizontal();
        DividerInstance3.Name = "DividerInstance3";
        DifficultyLabel = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        DifficultyLabel.Name = "DifficultyLabel";
        Background1 = new global::Gum.GueDeriving.NineSliceRuntime();
        Background1.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (Background1.ElementSave != null) Background1.AddStatesAndCategoriesRecursivelyToGue(Background1.ElementSave);
        if (Background1.ElementSave != null) Background1.SetInitialState();
        Background1.Name = "Background1";
        ComboBoxInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ComboBox();
        ComboBoxInstance.Name = "ComboBoxInstance";
        ButtonContainer = new global::Gum.GueDeriving.ContainerRuntime();
        ButtonContainer.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (ButtonContainer.ElementSave != null) ButtonContainer.AddStatesAndCategoriesRecursivelyToGue(ButtonContainer.ElementSave);
        if (ButtonContainer.ElementSave != null) ButtonContainer.SetInitialState();
        ButtonContainer.Name = "ButtonContainer";
        ButtonConfirmInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonConfirm();
        ButtonConfirmInstance.Name = "ButtonConfirmInstance";
        WindowOkButton = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonConfirm();
        WindowOkButton.Name = "WindowOkButton";
        ButtonDenyInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonDeny();
        ButtonDenyInstance.Name = "ButtonDenyInstance";
        DemoDialog = new global::Gum.GueDeriving.ContainerRuntime();
        DemoDialog.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (DemoDialog.ElementSave != null) DemoDialog.AddStatesAndCategoriesRecursivelyToGue(DemoDialog.ElementSave);
        if (DemoDialog.ElementSave != null) DemoDialog.SetInitialState();
        DemoDialog.Name = "DemoDialog";
        MarginContainer = new global::Gum.GueDeriving.ContainerRuntime();
        MarginContainer.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (MarginContainer.ElementSave != null) MarginContainer.AddStatesAndCategoriesRecursivelyToGue(MarginContainer.ElementSave);
        if (MarginContainer.ElementSave != null) MarginContainer.SetInitialState();
        MarginContainer.Name = "MarginContainer";
        LabelInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        LabelInstance.Name = "LabelInstance";
        TextBoxInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.TextBox();
        TextBoxInstance.Name = "TextBoxInstance";
        TextBoxInstance1 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.PasswordBox();
        TextBoxInstance1.Name = "TextBoxInstance1";
        MultiLineTextBox = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.TextBox();
        MultiLineTextBox.Name = "MultiLineTextBox";
        DemoHud = new global::Gum.GueDeriving.NineSliceRuntime();
        DemoHud.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (DemoHud.ElementSave != null) DemoHud.AddStatesAndCategoriesRecursivelyToGue(DemoHud.ElementSave);
        if (DemoHud.ElementSave != null) DemoHud.SetInitialState();
        DemoHud.Name = "DemoHud";
        TitleText2 = new global::Gum.GueDeriving.TextRuntime();
        TitleText2.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (TitleText2.ElementSave != null) TitleText2.AddStatesAndCategoriesRecursivelyToGue(TitleText2.ElementSave);
        if (TitleText2.ElementSave != null) TitleText2.SetInitialState();
        TitleText2.Name = "TitleText2";
        MenuTitle2 = new global::Gum.GueDeriving.ContainerRuntime();
        MenuTitle2.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (MenuTitle2.ElementSave != null) MenuTitle2.AddStatesAndCategoriesRecursivelyToGue(MenuTitle2.ElementSave);
        if (MenuTitle2.ElementSave != null) MenuTitle2.SetInitialState();
        MenuTitle2.Name = "MenuTitle2";
        PercentBarInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar();
        PercentBarInstance.Name = "PercentBarInstance";
        HitpointsBar1 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar();
        HitpointsBar1.Name = "HitpointsBar1";
        HitpointsBar2 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar();
        HitpointsBar2.Name = "HitpointsBar2";
        HitpointsBar3 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar();
        HitpointsBar3.Name = "HitpointsBar3";
        PercentBarInstance1 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar();
        PercentBarInstance1.Name = "PercentBarInstance1";
        PercentBarInstance2 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar();
        PercentBarInstance2.Name = "PercentBarInstance2";
        DividerInstance5 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.DividerHorizontal();
        DividerInstance5.Name = "DividerInstance5";
        ButtonCloseInstance2 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.ButtonClose();
        ButtonCloseInstance2.Name = "ButtonCloseInstance2";
        PercentBarInstance3 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar();
        PercentBarInstance3.Name = "PercentBarInstance3";
        PercentBarInstance4 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar();
        PercentBarInstance4.Name = "PercentBarInstance4";
        PercentBarInstance5 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBarIcon();
        PercentBarInstance5.Name = "PercentBarInstance5";
        ContainerInstance = new global::Gum.GueDeriving.ContainerRuntime();
        ContainerInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (ContainerInstance.ElementSave != null) ContainerInstance.AddStatesAndCategoriesRecursivelyToGue(ContainerInstance.ElementSave);
        if (ContainerInstance.ElementSave != null) ContainerInstance.SetInitialState();
        ContainerInstance.Name = "ContainerInstance";
        TreeViewInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.TreeView();
        TreeViewInstance.Name = "TreeViewInstance";
        DialogBoxInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.DialogBox();
        DialogBoxInstance.Name = "DialogBoxInstance";
        WindowStandardInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.WindowStandard();
        WindowStandardInstance.Name = "WindowStandardInstance";
        LabelInstance1 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.Label();
        LabelInstance1.Name = "LabelInstance1";
        Spaced_Component_Instance = new CodeGen_MonoGameForms_FullCodegen.Components.Spaced_Component();
        Spaced_Component_Instance.Name = "Spaced Component Instance";
        _123Instance = new CodeGen_MonoGameForms_FullCodegen.Components._123Component();
        _123Instance.Name = "123Instance";
    }
    protected virtual void InitializeInstancesFromVisual()
    {
        @this = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<@interface>(this.Visual,"this");
        DemoSettingsMenu = this.Visual?.GetGraphicalUiElementByName("DemoSettingsMenu") as global::Gum.GueDeriving.ContainerRuntime;
        Background = this.Visual?.GetGraphicalUiElementByName("Background") as global::Gum.GueDeriving.NineSliceRuntime;
        MenuTitle = this.Visual?.GetGraphicalUiElementByName("MenuTitle") as global::Gum.GueDeriving.ContainerRuntime;
        MenuTitle1 = this.Visual?.GetGraphicalUiElementByName("MenuTitle1") as global::Gum.GueDeriving.ContainerRuntime;
        MenuItems = this.Visual?.GetGraphicalUiElementByName("MenuItems") as global::Gum.GueDeriving.ContainerRuntime;
        TitleText = this.Visual?.GetGraphicalUiElementByName("TitleText") as global::Gum.GueDeriving.TextRuntime;
        TitleText1 = this.Visual?.GetGraphicalUiElementByName("TitleText1") as global::Gum.GueDeriving.TextRuntime;
        ButtonCloseInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonClose>(this.Visual,"ButtonCloseInstance1");
        DividerInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<DividerHorizontal>(this.Visual,"DividerInstance");
        DividerInstance4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<DividerHorizontal>(this.Visual,"DividerInstance4");
        ResolutionLabel = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Label>(this.Visual,"ResolutionLabel");
        ResolutionBox = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ListBox>(this.Visual,"ResolutionBox");
        DetectResolutionsButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonStandard>(this.Visual,"DetectResolutionsButton");
        ShowDialogButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonStandard>(this.Visual,"ShowDialogButton");
        ShowToastButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonStandard>(this.Visual,"ShowToastButton");
        FullScreenCheckbox = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<CheckBox>(this.Visual,"FullScreenCheckbox");
        DividerInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<DividerHorizontal>(this.Visual,"DividerInstance1");
        MusicLabel = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Label>(this.Visual,"MusicLabel");
        MusicSlider = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Slider>(this.Visual,"MusicSlider");
        SoundLabel = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Label>(this.Visual,"SoundLabel");
        SoundSlider = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Slider>(this.Visual,"SoundSlider");
        DividerInstance2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<DividerHorizontal>(this.Visual,"DividerInstance2");
        ControlLabel = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Label>(this.Visual,"ControlLabel");
        RadioButtonInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<RadioButton>(this.Visual,"RadioButtonInstance");
        RadioButtonInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<RadioButton>(this.Visual,"RadioButtonInstance1");
        RadioButtonInstance2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<RadioButton>(this.Visual,"RadioButtonInstance2");
        DividerInstance3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<DividerHorizontal>(this.Visual,"DividerInstance3");
        DifficultyLabel = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Label>(this.Visual,"DifficultyLabel");
        Background1 = this.Visual?.GetGraphicalUiElementByName("Background1") as global::Gum.GueDeriving.NineSliceRuntime;
        ComboBoxInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ComboBox>(this.Visual,"ComboBoxInstance");
        ButtonContainer = this.Visual?.GetGraphicalUiElementByName("ButtonContainer") as global::Gum.GueDeriving.ContainerRuntime;
        ButtonConfirmInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonConfirm>(this.Visual,"ButtonConfirmInstance");
        WindowOkButton = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonConfirm>(this.Visual,"WindowOkButton");
        ButtonDenyInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonDeny>(this.Visual,"ButtonDenyInstance");
        DemoDialog = this.Visual?.GetGraphicalUiElementByName("DemoDialog") as global::Gum.GueDeriving.ContainerRuntime;
        MarginContainer = this.Visual?.GetGraphicalUiElementByName("MarginContainer") as global::Gum.GueDeriving.ContainerRuntime;
        LabelInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Label>(this.Visual,"LabelInstance");
        TextBoxInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<TextBox>(this.Visual,"TextBoxInstance");
        TextBoxInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PasswordBox>(this.Visual,"TextBoxInstance1");
        MultiLineTextBox = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<TextBox>(this.Visual,"MultiLineTextBox");
        DemoHud = this.Visual?.GetGraphicalUiElementByName("DemoHud") as global::Gum.GueDeriving.NineSliceRuntime;
        TitleText2 = this.Visual?.GetGraphicalUiElementByName("TitleText2") as global::Gum.GueDeriving.TextRuntime;
        MenuTitle2 = this.Visual?.GetGraphicalUiElementByName("MenuTitle2") as global::Gum.GueDeriving.ContainerRuntime;
        PercentBarInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PercentBar>(this.Visual,"PercentBarInstance");
        HitpointsBar1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PercentBar>(this.Visual,"HitpointsBar1");
        HitpointsBar2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PercentBar>(this.Visual,"HitpointsBar2");
        HitpointsBar3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PercentBar>(this.Visual,"HitpointsBar3");
        PercentBarInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PercentBar>(this.Visual,"PercentBarInstance1");
        PercentBarInstance2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PercentBar>(this.Visual,"PercentBarInstance2");
        DividerInstance5 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<DividerHorizontal>(this.Visual,"DividerInstance5");
        ButtonCloseInstance2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<ButtonClose>(this.Visual,"ButtonCloseInstance2");
        PercentBarInstance3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PercentBar>(this.Visual,"PercentBarInstance3");
        PercentBarInstance4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PercentBar>(this.Visual,"PercentBarInstance4");
        PercentBarInstance5 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<PercentBarIcon>(this.Visual,"PercentBarInstance5");
        ContainerInstance = this.Visual?.GetGraphicalUiElementByName("ContainerInstance") as global::Gum.GueDeriving.ContainerRuntime;
        TreeViewInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<TreeView>(this.Visual,"TreeViewInstance");
        DialogBoxInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<DialogBox>(this.Visual,"DialogBoxInstance");
        WindowStandardInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<WindowStandard>(this.Visual,"WindowStandardInstance");
        LabelInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Label>(this.Visual,"LabelInstance1");
        Spaced_Component_Instance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<Spaced_Component>(this.Visual,"Spaced Component Instance");
        _123Instance = global::Gum.Forms.GraphicalUiElementFormsExtensions.FindFormsControl<_123Component>(this.Visual,"123Instance");
    }
    protected virtual void AssignParents()
    {
        this.AddChild(@this);
        this.AddChild(DemoSettingsMenu);
        DemoSettingsMenu.AddChild(Background);
        MenuItems.AddChild(MenuTitle);
        MarginContainer.AddChild(MenuTitle1);
        DemoSettingsMenu.AddChild(MenuItems);
        MenuTitle.AddChild(TitleText);
        MenuTitle1.AddChild(TitleText1);
        MenuTitle1.AddChild(ButtonCloseInstance1.Visual);
        MenuTitle.AddChild(DividerInstance.Visual);
        MenuTitle1.AddChild(DividerInstance4.Visual);
        MenuItems.AddChild(ResolutionLabel.Visual);
        MenuItems.AddChild(ResolutionBox.Visual);
        MenuItems.AddChild(DetectResolutionsButton.Visual);
        this.AddChild(ShowDialogButton);
        this.AddChild(ShowToastButton);
        MenuItems.AddChild(FullScreenCheckbox.Visual);
        MenuItems.AddChild(DividerInstance1.Visual);
        MenuItems.AddChild(MusicLabel.Visual);
        MenuItems.AddChild(MusicSlider.Visual);
        MenuItems.AddChild(SoundLabel.Visual);
        MenuItems.AddChild(SoundSlider.Visual);
        MenuItems.AddChild(DividerInstance2.Visual);
        MenuItems.AddChild(ControlLabel.Visual);
        MenuItems.AddChild(RadioButtonInstance.Visual);
        MenuItems.AddChild(RadioButtonInstance1.Visual);
        MenuItems.AddChild(RadioButtonInstance2.Visual);
        MenuItems.AddChild(DividerInstance3.Visual);
        MenuItems.AddChild(DifficultyLabel.Visual);
        DemoDialog.AddChild(Background1);
        MenuItems.AddChild(ComboBoxInstance.Visual);
        MenuItems.AddChild(ButtonContainer);
        ButtonContainer.AddChild(ButtonConfirmInstance.Visual);
        WindowStandardInstance.AddChild(WindowOkButton);
        ButtonContainer.AddChild(ButtonDenyInstance.Visual);
        this.AddChild(DemoDialog);
        DemoDialog.AddChild(MarginContainer);
        MarginContainer.AddChild(LabelInstance.Visual);
        MarginContainer.AddChild(TextBoxInstance.Visual);
        MarginContainer.AddChild(TextBoxInstance1.Visual);
        MarginContainer.AddChild(MultiLineTextBox.Visual);
        this.AddChild(DemoHud);
        MenuTitle2.AddChild(TitleText2);
        ContainerInstance.AddChild(MenuTitle2);
        ContainerInstance.AddChild(PercentBarInstance.Visual);
        DemoHud.AddChild(HitpointsBar1.Visual);
        DemoHud.AddChild(HitpointsBar2.Visual);
        DemoHud.AddChild(HitpointsBar3.Visual);
        ContainerInstance.AddChild(PercentBarInstance1.Visual);
        ContainerInstance.AddChild(PercentBarInstance2.Visual);
        MenuTitle2.AddChild(DividerInstance5.Visual);
        MenuTitle2.AddChild(ButtonCloseInstance2.Visual);
        ContainerInstance.AddChild(PercentBarInstance3.Visual);
        ContainerInstance.AddChild(PercentBarInstance4.Visual);
        ContainerInstance.AddChild(PercentBarInstance5.Visual);
        DemoHud.AddChild(ContainerInstance);
        this.AddChild(TreeViewInstance);
        this.AddChild(DialogBoxInstance);
        this.AddChild(WindowStandardInstance);
        WindowStandardInstance.AddChild(LabelInstance1);
        this.AddChild(Spaced_Component_Instance);
        this.AddChild(_123Instance);
    }
    protected virtual void ApplyDefaultVariables()
    {
        this.Visual.Width = 0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Visual.Height = 0f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.DemoSettingsMenu.Height = 16f;
        this.DemoSettingsMenu.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.DemoSettingsMenu.Width = 400f;
        this.DemoSettingsMenu.X = 15f;
        this.DemoSettingsMenu.Y = 12f;

        this.Background.SetProperty("ColorCategoryState", "Primary");
        this.Background.SetProperty("StyleCategoryState", "Panel");

        this.MenuTitle.Height = 8f;
        this.MenuTitle.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.MenuTitle.Width = 0f;
        this.MenuTitle.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.MenuTitle1.Height = 8f;
        this.MenuTitle1.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.MenuTitle1.Width = 0f;
        this.MenuTitle1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.MenuItems.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.MenuItems.Height = 0f;
        this.MenuItems.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.MenuItems.Width = -16f;
        this.MenuItems.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.MenuItems.X = 0f;
        this.MenuItems.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.MenuItems.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.MenuItems.Y = 0f;
        this.MenuItems.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.MenuItems.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.TitleText.SetProperty("ColorCategoryState", "Primary");
        this.TitleText.SetProperty("StyleCategoryState", "Title");
        this.TitleText.Text = @"Settings";

        this.TitleText1.SetProperty("ColorCategoryState", "Primary");
        this.TitleText1.SetProperty("StyleCategoryState", "Title");
        this.TitleText1.Text = @"New Profile";

        this.ButtonCloseInstance1.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.ButtonCloseInstance1.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.DividerInstance.Visual.Width = 0f;
        this.DividerInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.DividerInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.DividerInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.DividerInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.DividerInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.DividerInstance4.Visual.Width = 0f;
        this.DividerInstance4.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.DividerInstance4.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.DividerInstance4.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.DividerInstance4.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.DividerInstance4.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.ResolutionLabel.Text = @"Resolution";

        this.ResolutionBox.Visual.Height = 128f;
        this.ResolutionBox.Visual.Width = 0f;
        this.ResolutionBox.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.DetectResolutionsButton.ButtonDisplayText = @"Detect Resolutions";
        this.DetectResolutionsButton.Visual.Height = 24f;
        this.DetectResolutionsButton.Visual.Width = 156f;
        this.DetectResolutionsButton.Visual.X = 0f;
        this.DetectResolutionsButton.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.DetectResolutionsButton.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.DetectResolutionsButton.Visual.Y = 9f;

        this.ShowDialogButton.ButtonDisplayText = @"Show Dialog Box";
        this.ShowDialogButton.Visual.Height = 24f;
        this.ShowDialogButton.Visual.X = 722f;
        this.ShowDialogButton.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.ShowDialogButton.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.ShowDialogButton.Visual.Y = 348f;
        this.ShowDialogButton.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.ShowDialogButton.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.ShowToastButton.ButtonDisplayText = @"Show Toast";
        this.ShowToastButton.Visual.Height = 24f;
        this.ShowToastButton.Visual.X = 788f;
        this.ShowToastButton.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.ShowToastButton.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.ShowToastButton.Visual.Y = 16f;
        this.ShowToastButton.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.ShowToastButton.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.FullScreenCheckbox.Text = @"Run Fullscreen";
        this.FullScreenCheckbox.Visual.X = 0f;
        this.FullScreenCheckbox.Visual.Y = -26f;

        this.DividerInstance1.Visual.Width = 0f;
        this.DividerInstance1.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.DividerInstance1.Visual.Y = 8f;

        this.MusicLabel.Text = @"Music Volume";
        ((global::Gum.GueDeriving.TextRuntime)this.MusicLabel.Visual).Y = 8f;

        this.MusicSlider.Visual.Width = 0f;
        this.MusicSlider.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.SoundLabel.Text = @"Sound Volume";

        this.SoundSlider.Visual.Width = 0f;
        this.SoundSlider.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.DividerInstance2.Visual.Width = 0f;
        this.DividerInstance2.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.DividerInstance2.Visual.Y = 8f;

        this.ControlLabel.Text = @"Control Scheme";
        ((global::Gum.GueDeriving.TextRuntime)this.ControlLabel.Visual).Y = 8f;

        this.RadioButtonInstance.RadioButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.RadioButton.RadioButtonCategory.EnabledOff;
        this.RadioButtonInstance.Text = @"Keyboard & Mouse";
        this.RadioButtonInstance.Visual.Width = 0f;
        this.RadioButtonInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.RadioButtonInstance1.RadioButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.RadioButton.RadioButtonCategory.EnabledOn;
        this.RadioButtonInstance1.Text = @"Gamepad";
        this.RadioButtonInstance1.Visual.Width = 0f;
        this.RadioButtonInstance1.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.RadioButtonInstance1.Visual.Y = 4f;

        this.RadioButtonInstance2.RadioButtonCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.RadioButton.RadioButtonCategory.EnabledOff;
        this.RadioButtonInstance2.Text = @"Touchscreen";
        this.RadioButtonInstance2.Visual.Width = 0f;
        this.RadioButtonInstance2.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.RadioButtonInstance2.Visual.Y = 4f;

        this.DividerInstance3.Visual.Width = 0f;
        this.DividerInstance3.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.DividerInstance3.Visual.Y = 8f;

        this.DifficultyLabel.Text = @"Difficulty";
        ((global::Gum.GueDeriving.TextRuntime)this.DifficultyLabel.Visual).Y = 8f;

        this.Background1.SetProperty("ColorCategoryState", "Primary");
        this.Background1.SetProperty("StyleCategoryState", "Panel");
        this.Background1.Height = 0f;
        this.Background1.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Background1.Width = 0f;
        this.Background1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Background1.X = 0f;
        this.Background1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Background1.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Background1.Y = 0f;
        this.Background1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Background1.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.ComboBoxInstance.Visual.Width = 0f;
        this.ComboBoxInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.ButtonContainer.Height = 32f;
        this.ButtonContainer.Width = 0f;
        this.ButtonContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ButtonContainer.Y = 16f;

        this.ButtonConfirmInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.ButtonConfirmInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.WindowOkButton.Visual.X = -8f;
        this.WindowOkButton.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.WindowOkButton.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.WindowOkButton.Visual.Y = -8f;
        this.WindowOkButton.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.WindowOkButton.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;


        this.DemoDialog.Height = 320f;
        this.DemoDialog.Width = 349f;
        this.DemoDialog.X = 425f;
        this.DemoDialog.Y = 13f;

        this.MarginContainer.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.MarginContainer.Height = -16f;
        this.MarginContainer.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.MarginContainer.Width = -16f;
        this.MarginContainer.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.MarginContainer.X = 0f;
        this.MarginContainer.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.MarginContainer.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.MarginContainer.Y = 0f;
        this.MarginContainer.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.MarginContainer.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.LabelInstance.Text = @"New Profile Name";
        ((global::Gum.GueDeriving.TextRuntime)this.LabelInstance.Visual).Y = 8f;

        this.TextBoxInstance.Visual.Width = 0f;
        this.TextBoxInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.TextBoxInstance1.Visual.Width = 0f;
        this.TextBoxInstance1.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.TextBoxInstance1.Visual.X = 0f;
        this.TextBoxInstance1.Visual.Y = 7f;

        this.MultiLineTextBox.LineModeCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Controls.TextBox.LineModeCategory.Multi;
        this.MultiLineTextBox.Visual.Height = 174f;
        this.MultiLineTextBox.PlaceholderText = @"Multi-line text box";
        this.MultiLineTextBox.Visual.Width = 0f;
        this.MultiLineTextBox.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.MultiLineTextBox.Visual.Y = 7f;

        this.DemoHud.SetProperty("ColorCategoryState", "Primary");
        this.DemoHud.SetProperty("StyleCategoryState", "Panel");
        this.DemoHud.Height = 207f;
        this.DemoHud.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.DemoHud.Width = 279f;
        this.DemoHud.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.DemoHud.X = 426f;
        this.DemoHud.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.DemoHud.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.DemoHud.Y = 346f;
        this.DemoHud.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.DemoHud.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.TitleText2.SetProperty("ColorCategoryState", "Primary");
        this.TitleText2.SetProperty("StyleCategoryState", "Title");
        this.TitleText2.Text = @"HUD Demo";

        this.MenuTitle2.Height = 8f;
        this.MenuTitle2.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.MenuTitle2.Width = 0f;
        this.MenuTitle2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.PercentBarInstance.Visual.Width = 0f;
        this.PercentBarInstance.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.PercentBarInstance.Visual.Y = 8f;

        this.HitpointsBar1.Visual.SetProperty("BarColor", "Success");
        this.HitpointsBar1.BarDecorCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar.BarDecorCategory.VerticalLines;
        this.HitpointsBar1.BarPercent = 75f;
        this.HitpointsBar1.Visual.Height = 8f;
        this.HitpointsBar1.Visual.Width = 24f;
        this.HitpointsBar1.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.HitpointsBar1.Visual.X = 184f;
        this.HitpointsBar1.Visual.Y = 10f;

        this.HitpointsBar2.Visual.SetProperty("BarColor", "Warning");
        this.HitpointsBar2.BarDecorCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar.BarDecorCategory.VerticalLines;
        this.HitpointsBar2.BarPercent = 50f;
        this.HitpointsBar2.Visual.Height = 8f;
        this.HitpointsBar2.Visual.Width = 24f;
        this.HitpointsBar2.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.HitpointsBar2.Visual.X = 184f;
        this.HitpointsBar2.Visual.Y = 20f;

        this.HitpointsBar3.Visual.SetProperty("BarColor", "Danger");
        this.HitpointsBar3.BarDecorCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar.BarDecorCategory.VerticalLines;
        this.HitpointsBar3.BarPercent = 25f;
        this.HitpointsBar3.Visual.Height = 8f;
        this.HitpointsBar3.Visual.Width = 24f;
        this.HitpointsBar3.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        this.HitpointsBar3.Visual.X = 184f;
        this.HitpointsBar3.Visual.Y = 31f;

        this.PercentBarInstance1.Visual.SetProperty("BarColor", "Success");
        this.PercentBarInstance1.BarDecorCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar.BarDecorCategory.CautionLines;
        this.PercentBarInstance1.Visual.Width = 0f;
        this.PercentBarInstance1.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.PercentBarInstance1.Visual.Y = 8f;

        this.PercentBarInstance2.Visual.SetProperty("BarColor", "Warning");
        this.PercentBarInstance2.BarDecorCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar.BarDecorCategory.VerticalLines;
        this.PercentBarInstance2.Visual.Width = 0f;
        this.PercentBarInstance2.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.PercentBarInstance2.Visual.Y = 8f;

        this.DividerInstance5.Visual.Width = 0f;
        this.DividerInstance5.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.DividerInstance5.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.DividerInstance5.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.DividerInstance5.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.DividerInstance5.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.ButtonCloseInstance2.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.ButtonCloseInstance2.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.PercentBarInstance3.Visual.SetProperty("BarColor", "Danger");
        this.PercentBarInstance3.Visual.Width = 0f;
        this.PercentBarInstance3.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.PercentBarInstance3.Visual.Y = 8f;

        this.PercentBarInstance4.Visual.SetProperty("BarColor", "Accent");
        this.PercentBarInstance4.BarDecorCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.PercentBar.BarDecorCategory.CautionLines;
        this.PercentBarInstance4.Visual.Width = 0f;
        this.PercentBarInstance4.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.PercentBarInstance4.Visual.Y = 8f;

        this.PercentBarInstance5.Visual.SetProperty("BarColor", "Danger");
        this.PercentBarInstance5.Visual.Width = 0f;
        this.PercentBarInstance5.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.PercentBarInstance5.Visual.Y = 8f;

        this.ContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.ContainerInstance.Height = -16f;
        this.ContainerInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ContainerInstance.Width = -16f;
        this.ContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ContainerInstance.X = 0f;
        this.ContainerInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ContainerInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.ContainerInstance.Y = 0f;
        this.ContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.ContainerInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.TreeViewInstance.Visual.Height = 155f;
        this.TreeViewInstance.Visual.Width = 227f;
        this.TreeViewInstance.Visual.X = 783f;
        this.TreeViewInstance.Visual.Y = 50f;

        this.DialogBoxInstance.Visual.X = 722f;
        this.DialogBoxInstance.Visual.Y = 376f;

        this.WindowStandardInstance.Visual.Height = 163f;
        this.WindowStandardInstance.Visual.Width = 313f;
        this.WindowStandardInstance.Visual.X = 15f;
        this.WindowStandardInstance.Visual.Y = 575f;

        this.LabelInstance1.Text = @"I am a label. I will line wrap if you resize this window. You can also grab the top part of the window to move it around. Try it!";
        ((global::Gum.GueDeriving.TextRuntime)this.LabelInstance1.Visual).Width = -16f;
        ((global::Gum.GueDeriving.TextRuntime)this.LabelInstance1.Visual).WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        ((global::Gum.GueDeriving.TextRuntime)this.LabelInstance1.Visual).X = 0f;
        ((global::Gum.GueDeriving.TextRuntime)this.LabelInstance1.Visual).XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        ((global::Gum.GueDeriving.TextRuntime)this.LabelInstance1.Visual).XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        ((global::Gum.GueDeriving.TextRuntime)this.LabelInstance1.Visual).Y = 22f;
        ((global::Gum.GueDeriving.TextRuntime)this.LabelInstance1.Visual).YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        ((global::Gum.GueDeriving.TextRuntime)this.LabelInstance1.Visual).YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;



    }
    partial void CustomInitialize();
}
