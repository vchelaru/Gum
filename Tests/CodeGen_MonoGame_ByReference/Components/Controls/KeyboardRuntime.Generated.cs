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
        Key1 = this.GetGraphicalUiElementByName("Key1") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyQ = this.GetGraphicalUiElementByName("KeyQ") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyA = this.GetGraphicalUiElementByName("KeyA") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyZ = this.GetGraphicalUiElementByName("KeyZ") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyParenLeft = this.GetGraphicalUiElementByName("KeyParenLeft") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyW = this.GetGraphicalUiElementByName("KeyW") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyS = this.GetGraphicalUiElementByName("KeyS") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyX = this.GetGraphicalUiElementByName("KeyX") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyParenRight = this.GetGraphicalUiElementByName("KeyParenRight") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyE = this.GetGraphicalUiElementByName("KeyE") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyD = this.GetGraphicalUiElementByName("KeyD") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyC = this.GetGraphicalUiElementByName("KeyC") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeySpace = this.GetGraphicalUiElementByName("KeySpace") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyR = this.GetGraphicalUiElementByName("KeyR") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyF = this.GetGraphicalUiElementByName("KeyF") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyV = this.GetGraphicalUiElementByName("KeyV") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyT = this.GetGraphicalUiElementByName("KeyT") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyG = this.GetGraphicalUiElementByName("KeyG") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyB = this.GetGraphicalUiElementByName("KeyB") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyY = this.GetGraphicalUiElementByName("KeyY") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyH = this.GetGraphicalUiElementByName("KeyH") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyN = this.GetGraphicalUiElementByName("KeyN") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyU = this.GetGraphicalUiElementByName("KeyU") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyJ = this.GetGraphicalUiElementByName("KeyJ") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyM = this.GetGraphicalUiElementByName("KeyM") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyI = this.GetGraphicalUiElementByName("KeyI") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyK = this.GetGraphicalUiElementByName("KeyK") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyComma = this.GetGraphicalUiElementByName("KeyComma") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyQuestion = this.GetGraphicalUiElementByName("KeyQuestion") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyO = this.GetGraphicalUiElementByName("KeyO") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyL = this.GetGraphicalUiElementByName("KeyL") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyPeriod = this.GetGraphicalUiElementByName("KeyPeriod") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyBang = this.GetGraphicalUiElementByName("KeyBang") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyP = this.GetGraphicalUiElementByName("KeyP") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyUnderscore = this.GetGraphicalUiElementByName("KeyUnderscore") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyHyphen = this.GetGraphicalUiElementByName("KeyHyphen") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyAmpersand = this.GetGraphicalUiElementByName("KeyAmpersand") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Key2 = this.GetGraphicalUiElementByName("Key2") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Key3 = this.GetGraphicalUiElementByName("Key3") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Key4 = this.GetGraphicalUiElementByName("Key4") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Key5 = this.GetGraphicalUiElementByName("Key5") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Key6 = this.GetGraphicalUiElementByName("Key6") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Key7 = this.GetGraphicalUiElementByName("Key7") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Key8 = this.GetGraphicalUiElementByName("Key8") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Key9 = this.GetGraphicalUiElementByName("Key9") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Key0 = this.GetGraphicalUiElementByName("Key0") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        Row2Keys = this.GetGraphicalUiElementByName("Row2Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row3Keys = this.GetGraphicalUiElementByName("Row3Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row4Keys = this.GetGraphicalUiElementByName("Row4Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row5Keys = this.GetGraphicalUiElementByName("Row5Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        KeyBackspace = this.GetGraphicalUiElementByName("KeyBackspace") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyReturn = this.GetGraphicalUiElementByName("KeyReturn") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyLeft = this.GetGraphicalUiElementByName("KeyLeft") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        KeyRight = this.GetGraphicalUiElementByName("KeyRight") as global::MonoGameGum.GueDeriving.KeyboardKeyRuntime;
        HighlightRectangle = this.GetGraphicalUiElementByName("HighlightRectangle") as global::MonoGameGum.GueDeriving.RectangleRuntime;
        IconInstance = this.GetGraphicalUiElementByName("IconInstance") as global::MonoGameGum.GueDeriving.IconRuntime;
        IconInstance1 = this.GetGraphicalUiElementByName("IconInstance1") as global::MonoGameGum.GueDeriving.IconRuntime;
        IconInstance2 = this.GetGraphicalUiElementByName("IconInstance2") as global::MonoGameGum.GueDeriving.IconRuntime;
        IconInstance3 = this.GetGraphicalUiElementByName("IconInstance3") as global::MonoGameGum.GueDeriving.IconRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
