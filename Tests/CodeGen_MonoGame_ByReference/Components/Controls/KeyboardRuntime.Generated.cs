//Code for Controls/Keyboard (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using CodeGen_MonoGame_ByReference.Components.Controls;
using CodeGen_MonoGame_ByReference.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

namespace CodeGen_MonoGame_ByReference.Components.Controls;
partial class KeyboardRuntime : ContainerRuntime
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/Keyboard", typeof(KeyboardRuntime));
    }
    public enum CursorMoveCategory
    {
        LeftRightMoveSupported,
        NoMovement,
    }

    CursorMoveCategory? _cursorMoveCategoryState;
    public CursorMoveCategory? CursorMoveCategoryState
    {
        get => _cursorMoveCategoryState;
        set
        {
            _cursorMoveCategoryState = value;
            if(value != null)
            {
                if(Categories.ContainsKey("CursorMoveCategory"))
                {
                    var category = Categories["CursorMoveCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
                else
                {
                    var category = ((global::Gum.DataTypes.ElementSave)this.Tag).Categories.FirstOrDefault(item => item.Name == "CursorMoveCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.ApplyState(state);
                }
            }
        }
    }
    public ContainerRuntime Row1Keys { get; protected set; }
    public ContainerRuntime AllRows { get; protected set; }
    public KeyboardKeyRuntime Key1 { get; protected set; }
    public KeyboardKeyRuntime KeyQ { get; protected set; }
    public KeyboardKeyRuntime KeyA { get; protected set; }
    public KeyboardKeyRuntime KeyZ { get; protected set; }
    public KeyboardKeyRuntime KeyParenLeft { get; protected set; }
    public KeyboardKeyRuntime KeyW { get; protected set; }
    public KeyboardKeyRuntime KeyS { get; protected set; }
    public KeyboardKeyRuntime KeyX { get; protected set; }
    public KeyboardKeyRuntime KeyParenRight { get; protected set; }
    public KeyboardKeyRuntime KeyE { get; protected set; }
    public KeyboardKeyRuntime KeyD { get; protected set; }
    public KeyboardKeyRuntime KeyC { get; protected set; }
    public KeyboardKeyRuntime KeySpace { get; protected set; }
    public KeyboardKeyRuntime KeyR { get; protected set; }
    public KeyboardKeyRuntime KeyF { get; protected set; }
    public KeyboardKeyRuntime KeyV { get; protected set; }
    public KeyboardKeyRuntime KeyT { get; protected set; }
    public KeyboardKeyRuntime KeyG { get; protected set; }
    public KeyboardKeyRuntime KeyB { get; protected set; }
    public KeyboardKeyRuntime KeyY { get; protected set; }
    public KeyboardKeyRuntime KeyH { get; protected set; }
    public KeyboardKeyRuntime KeyN { get; protected set; }
    public KeyboardKeyRuntime KeyU { get; protected set; }
    public KeyboardKeyRuntime KeyJ { get; protected set; }
    public KeyboardKeyRuntime KeyM { get; protected set; }
    public KeyboardKeyRuntime KeyI { get; protected set; }
    public KeyboardKeyRuntime KeyK { get; protected set; }
    public KeyboardKeyRuntime KeyComma { get; protected set; }
    public KeyboardKeyRuntime KeyQuestion { get; protected set; }
    public KeyboardKeyRuntime KeyO { get; protected set; }
    public KeyboardKeyRuntime KeyL { get; protected set; }
    public KeyboardKeyRuntime KeyPeriod { get; protected set; }
    public KeyboardKeyRuntime KeyBang { get; protected set; }
    public KeyboardKeyRuntime KeyP { get; protected set; }
    public KeyboardKeyRuntime KeyUnderscore { get; protected set; }
    public KeyboardKeyRuntime KeyHyphen { get; protected set; }
    public KeyboardKeyRuntime KeyAmpersand { get; protected set; }
    public KeyboardKeyRuntime Key2 { get; protected set; }
    public KeyboardKeyRuntime Key3 { get; protected set; }
    public KeyboardKeyRuntime Key4 { get; protected set; }
    public KeyboardKeyRuntime Key5 { get; protected set; }
    public KeyboardKeyRuntime Key6 { get; protected set; }
    public KeyboardKeyRuntime Key7 { get; protected set; }
    public KeyboardKeyRuntime Key8 { get; protected set; }
    public KeyboardKeyRuntime Key9 { get; protected set; }
    public KeyboardKeyRuntime Key0 { get; protected set; }
    public ContainerRuntime Row2Keys { get; protected set; }
    public ContainerRuntime Row3Keys { get; protected set; }
    public ContainerRuntime Row4Keys { get; protected set; }
    public ContainerRuntime Row5Keys { get; protected set; }
    public KeyboardKeyRuntime KeyBackspace { get; protected set; }
    public KeyboardKeyRuntime KeyReturn { get; protected set; }
    public KeyboardKeyRuntime KeyLeft { get; protected set; }
    public KeyboardKeyRuntime KeyRight { get; protected set; }
    public RectangleRuntime HighlightRectangle { get; protected set; }
    public IconRuntime IconInstance { get; protected set; }
    public IconRuntime IconInstance1 { get; protected set; }
    public IconRuntime IconInstance2 { get; protected set; }
    public IconRuntime IconInstance3 { get; protected set; }

    public KeyboardRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
    {
        if(fullInstantiation)
        {
            var element = ObjectFinder.Self.GetElementSave("Controls/Keyboard");
            element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);
        }



    }
    public override void AfterFullCreation()
    {
        Row1Keys = this.GetGraphicalUiElementByName("Row1Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        AllRows = this.GetGraphicalUiElementByName("AllRows") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Key1 = this.GetGraphicalUiElementByName("Key1") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyQ = this.GetGraphicalUiElementByName("KeyQ") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyA = this.GetGraphicalUiElementByName("KeyA") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyZ = this.GetGraphicalUiElementByName("KeyZ") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyParenLeft = this.GetGraphicalUiElementByName("KeyParenLeft") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyW = this.GetGraphicalUiElementByName("KeyW") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyS = this.GetGraphicalUiElementByName("KeyS") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyX = this.GetGraphicalUiElementByName("KeyX") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyParenRight = this.GetGraphicalUiElementByName("KeyParenRight") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyE = this.GetGraphicalUiElementByName("KeyE") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyD = this.GetGraphicalUiElementByName("KeyD") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyC = this.GetGraphicalUiElementByName("KeyC") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeySpace = this.GetGraphicalUiElementByName("KeySpace") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyR = this.GetGraphicalUiElementByName("KeyR") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyF = this.GetGraphicalUiElementByName("KeyF") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyV = this.GetGraphicalUiElementByName("KeyV") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyT = this.GetGraphicalUiElementByName("KeyT") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyG = this.GetGraphicalUiElementByName("KeyG") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyB = this.GetGraphicalUiElementByName("KeyB") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyY = this.GetGraphicalUiElementByName("KeyY") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyH = this.GetGraphicalUiElementByName("KeyH") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyN = this.GetGraphicalUiElementByName("KeyN") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyU = this.GetGraphicalUiElementByName("KeyU") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyJ = this.GetGraphicalUiElementByName("KeyJ") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyM = this.GetGraphicalUiElementByName("KeyM") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyI = this.GetGraphicalUiElementByName("KeyI") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyK = this.GetGraphicalUiElementByName("KeyK") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyComma = this.GetGraphicalUiElementByName("KeyComma") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyQuestion = this.GetGraphicalUiElementByName("KeyQuestion") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyO = this.GetGraphicalUiElementByName("KeyO") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyL = this.GetGraphicalUiElementByName("KeyL") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyPeriod = this.GetGraphicalUiElementByName("KeyPeriod") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyBang = this.GetGraphicalUiElementByName("KeyBang") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyP = this.GetGraphicalUiElementByName("KeyP") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyUnderscore = this.GetGraphicalUiElementByName("KeyUnderscore") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyHyphen = this.GetGraphicalUiElementByName("KeyHyphen") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyAmpersand = this.GetGraphicalUiElementByName("KeyAmpersand") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Key2 = this.GetGraphicalUiElementByName("Key2") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Key3 = this.GetGraphicalUiElementByName("Key3") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Key4 = this.GetGraphicalUiElementByName("Key4") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Key5 = this.GetGraphicalUiElementByName("Key5") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Key6 = this.GetGraphicalUiElementByName("Key6") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Key7 = this.GetGraphicalUiElementByName("Key7") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Key8 = this.GetGraphicalUiElementByName("Key8") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Key9 = this.GetGraphicalUiElementByName("Key9") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Key0 = this.GetGraphicalUiElementByName("Key0") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        Row2Keys = this.GetGraphicalUiElementByName("Row2Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row3Keys = this.GetGraphicalUiElementByName("Row3Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row4Keys = this.GetGraphicalUiElementByName("Row4Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row5Keys = this.GetGraphicalUiElementByName("Row5Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        KeyBackspace = this.GetGraphicalUiElementByName("KeyBackspace") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyReturn = this.GetGraphicalUiElementByName("KeyReturn") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyLeft = this.GetGraphicalUiElementByName("KeyLeft") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        KeyRight = this.GetGraphicalUiElementByName("KeyRight") as CodeGen_MonoGame_ByReference.Components.Controls.KeyboardKeyRuntime;
        HighlightRectangle = this.GetGraphicalUiElementByName("HighlightRectangle") as global::MonoGameGum.GueDeriving.RectangleRuntime;
        IconInstance = this.GetGraphicalUiElementByName("IconInstance") as CodeGen_MonoGame_ByReference.Components.Elements.IconRuntime;
        IconInstance1 = this.GetGraphicalUiElementByName("IconInstance1") as CodeGen_MonoGame_ByReference.Components.Elements.IconRuntime;
        IconInstance2 = this.GetGraphicalUiElementByName("IconInstance2") as CodeGen_MonoGame_ByReference.Components.Elements.IconRuntime;
        IconInstance3 = this.GetGraphicalUiElementByName("IconInstance3") as CodeGen_MonoGame_ByReference.Components.Elements.IconRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
