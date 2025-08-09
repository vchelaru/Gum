//Code for Controls/PlayerJoinViewItem (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGameForms_FullCodegen.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
partial class PlayerJoinViewItem : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/PlayerJoinViewItem");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new PlayerJoinViewItem(visual);
            return visual;
        });
        global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(PlayerJoinViewItem)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/PlayerJoinViewItem", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
    }
    public enum PlayerJoinCategory
    {
        NotConnected,
        Connected,
        ConnectedAndJoined,
    }
    public enum PlayerIndexCategory
    {
        Player1,
        Player2,
        Player3,
        Player4,
    }
    public enum GamepadLayoutCategory
    {
        Unknown,
        Keyboard,
        NES,
        SuperNintendo,
        Nintendo64,
        GameCube,
        SwitchPro,
        Genesis,
        Xbox360,
        PlayStationDualShock,
    }

    PlayerJoinCategory? mPlayerJoinCategoryState;
    public PlayerJoinCategory? PlayerJoinCategoryState
    {
        get => mPlayerJoinCategoryState;
        set
        {
            mPlayerJoinCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case PlayerJoinCategory.NotConnected:
                        this.Background.SetProperty("ColorCategoryState", "Gray");
                        this.ControllerDisplayNameTextInstance.Visible = false;
                        break;
                    case PlayerJoinCategory.Connected:
                        this.Background.SetProperty("ColorCategoryState", "Primary");
                        this.ControllerDisplayNameTextInstance.Visible = true;
                        break;
                    case PlayerJoinCategory.ConnectedAndJoined:
                        this.Background.SetProperty("ColorCategoryState", "Success");
                        this.ControllerDisplayNameTextInstance.Visible = true;
                        break;
                }
            }
        }
    }

    PlayerIndexCategory? mPlayerIndexCategoryState;
    public PlayerIndexCategory? PlayerIndexCategoryState
    {
        get => mPlayerIndexCategoryState;
        set
        {
            mPlayerIndexCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case PlayerIndexCategory.Player1:
                        break;
                    case PlayerIndexCategory.Player2:
                        break;
                    case PlayerIndexCategory.Player3:
                        break;
                    case PlayerIndexCategory.Player4:
                        break;
                }
            }
        }
    }

    GamepadLayoutCategory? mGamepadLayoutCategoryState;
    public GamepadLayoutCategory? GamepadLayoutCategoryState
    {
        get => mGamepadLayoutCategoryState;
        set
        {
            mGamepadLayoutCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case GamepadLayoutCategory.Unknown:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadXbox;
                        this.InputDeviceIcon.Visual.Visible = false;
                        break;
                    case GamepadLayoutCategory.Keyboard:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Keyboard;
                        this.InputDeviceIcon.Visual.Visible = true;
                        break;
                    case GamepadLayoutCategory.NES:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadNES;
                        this.InputDeviceIcon.Visual.Visible = true;
                        break;
                    case GamepadLayoutCategory.SuperNintendo:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadSNES;
                        this.InputDeviceIcon.Visual.Visible = true;
                        break;
                    case GamepadLayoutCategory.Nintendo64:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadNintendo64;
                        this.InputDeviceIcon.Visual.Visible = true;
                        break;
                    case GamepadLayoutCategory.GameCube:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadGamecube;
                        this.InputDeviceIcon.Visual.Visible = true;
                        break;
                    case GamepadLayoutCategory.SwitchPro:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadSwitchPro;
                        this.InputDeviceIcon.Visual.Visible = true;
                        break;
                    case GamepadLayoutCategory.Genesis:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadSegaGenesis;
                        this.InputDeviceIcon.Visual.Visible = true;
                        break;
                    case GamepadLayoutCategory.Xbox360:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadXbox;
                        this.InputDeviceIcon.Visual.Visible = true;
                        break;
                    case GamepadLayoutCategory.PlayStationDualShock:
                        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadPlaystationDualShock;
                        this.InputDeviceIcon.Visual.Visible = true;
                        break;
                }
            }
        }
    }
    public NineSliceRuntime Background { get; protected set; }
    public TextRuntime ControllerDisplayNameTextInstance { get; protected set; }
    public Icon InputDeviceIcon { get; protected set; }

    public PlayerJoinViewItem(InteractiveGue visual) : base(visual)
    {
        InitializeInstances();
        CustomInitialize();
    }
    public PlayerJoinViewItem() : base(new ContainerRuntime())
    {

        this.Visual.Height = 80f;
         

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        base.ReactToVisualChanged();
        Background = new global::MonoGameGum.GueDeriving.NineSliceRuntime();
        Background.ElementSave = ObjectFinder.Self.GetStandardElement("NineSlice");
        if (Background.ElementSave != null) Background.AddStatesAndCategoriesRecursivelyToGue(Background.ElementSave);
        if (Background.ElementSave != null) Background.SetInitialState();
        Background.Name = "Background";
        ControllerDisplayNameTextInstance = new global::MonoGameGum.GueDeriving.TextRuntime();
        ControllerDisplayNameTextInstance.ElementSave = ObjectFinder.Self.GetStandardElement("Text");
        if (ControllerDisplayNameTextInstance.ElementSave != null) ControllerDisplayNameTextInstance.AddStatesAndCategoriesRecursivelyToGue(ControllerDisplayNameTextInstance.ElementSave);
        if (ControllerDisplayNameTextInstance.ElementSave != null) ControllerDisplayNameTextInstance.SetInitialState();
        ControllerDisplayNameTextInstance.Name = "ControllerDisplayNameTextInstance";
        InputDeviceIcon = new Icon();
        InputDeviceIcon.Name = "InputDeviceIcon";
    }
    protected virtual void AssignParents()
    {
        this.AddChild(Background);
        this.AddChild(ControllerDisplayNameTextInstance);
        this.AddChild(InputDeviceIcon);
    }
    private void ApplyDefaultVariables()
    {
        this.Background.SetProperty("ColorCategoryState", "Black");
        this.Background.SetProperty("StyleCategoryState", "Panel");
        this.Background.Height = 0f;
        this.Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.Background.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.ControllerDisplayNameTextInstance.SetProperty("ColorCategoryState", "White");
        this.ControllerDisplayNameTextInstance.SetProperty("StyleCategoryState", "Tiny");
        this.ControllerDisplayNameTextInstance.Height = 0f;
        this.ControllerDisplayNameTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.ControllerDisplayNameTextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ControllerDisplayNameTextInstance.Text = @"<Controller Type>";
        this.ControllerDisplayNameTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.ControllerDisplayNameTextInstance.Width = -16f;
        this.ControllerDisplayNameTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.ControllerDisplayNameTextInstance.X = 0f;
        this.ControllerDisplayNameTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.ControllerDisplayNameTextInstance.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.ControllerDisplayNameTextInstance.Y = -29f;
        this.ControllerDisplayNameTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.ControllerDisplayNameTextInstance.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.InputDeviceIcon.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.GamepadXbox;
        this.InputDeviceIcon.Visual.X = 0f;
        this.InputDeviceIcon.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.InputDeviceIcon.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.InputDeviceIcon.Visual.Y = 0f;
        this.InputDeviceIcon.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.InputDeviceIcon.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    partial void CustomInitialize();
}
