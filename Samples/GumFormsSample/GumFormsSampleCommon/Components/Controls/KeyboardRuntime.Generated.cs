//Code for Controls/Keyboard (Container)
using GumRuntime;
using System.Linq;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GumFormsSample.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components;
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
        Key1 = this.GetGraphicalUiElementByName("Key1") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyQ = this.GetGraphicalUiElementByName("KeyQ") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyA = this.GetGraphicalUiElementByName("KeyA") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyZ = this.GetGraphicalUiElementByName("KeyZ") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyParenLeft = this.GetGraphicalUiElementByName("KeyParenLeft") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyW = this.GetGraphicalUiElementByName("KeyW") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyS = this.GetGraphicalUiElementByName("KeyS") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyX = this.GetGraphicalUiElementByName("KeyX") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyParenRight = this.GetGraphicalUiElementByName("KeyParenRight") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyE = this.GetGraphicalUiElementByName("KeyE") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyD = this.GetGraphicalUiElementByName("KeyD") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyC = this.GetGraphicalUiElementByName("KeyC") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeySpace = this.GetGraphicalUiElementByName("KeySpace") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyR = this.GetGraphicalUiElementByName("KeyR") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyF = this.GetGraphicalUiElementByName("KeyF") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyV = this.GetGraphicalUiElementByName("KeyV") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyT = this.GetGraphicalUiElementByName("KeyT") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyG = this.GetGraphicalUiElementByName("KeyG") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyB = this.GetGraphicalUiElementByName("KeyB") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyY = this.GetGraphicalUiElementByName("KeyY") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyH = this.GetGraphicalUiElementByName("KeyH") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyN = this.GetGraphicalUiElementByName("KeyN") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyU = this.GetGraphicalUiElementByName("KeyU") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyJ = this.GetGraphicalUiElementByName("KeyJ") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyM = this.GetGraphicalUiElementByName("KeyM") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyI = this.GetGraphicalUiElementByName("KeyI") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyK = this.GetGraphicalUiElementByName("KeyK") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyComma = this.GetGraphicalUiElementByName("KeyComma") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyQuestion = this.GetGraphicalUiElementByName("KeyQuestion") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyO = this.GetGraphicalUiElementByName("KeyO") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyL = this.GetGraphicalUiElementByName("KeyL") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyPeriod = this.GetGraphicalUiElementByName("KeyPeriod") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyBang = this.GetGraphicalUiElementByName("KeyBang") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyP = this.GetGraphicalUiElementByName("KeyP") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyUnderscore = this.GetGraphicalUiElementByName("KeyUnderscore") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyHyphen = this.GetGraphicalUiElementByName("KeyHyphen") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyAmpersand = this.GetGraphicalUiElementByName("KeyAmpersand") as GumFormsSample.Components.KeyboardKeyRuntime;
        Key2 = this.GetGraphicalUiElementByName("Key2") as GumFormsSample.Components.KeyboardKeyRuntime;
        Key3 = this.GetGraphicalUiElementByName("Key3") as GumFormsSample.Components.KeyboardKeyRuntime;
        Key4 = this.GetGraphicalUiElementByName("Key4") as GumFormsSample.Components.KeyboardKeyRuntime;
        Key5 = this.GetGraphicalUiElementByName("Key5") as GumFormsSample.Components.KeyboardKeyRuntime;
        Key6 = this.GetGraphicalUiElementByName("Key6") as GumFormsSample.Components.KeyboardKeyRuntime;
        Key7 = this.GetGraphicalUiElementByName("Key7") as GumFormsSample.Components.KeyboardKeyRuntime;
        Key8 = this.GetGraphicalUiElementByName("Key8") as GumFormsSample.Components.KeyboardKeyRuntime;
        Key9 = this.GetGraphicalUiElementByName("Key9") as GumFormsSample.Components.KeyboardKeyRuntime;
        Key0 = this.GetGraphicalUiElementByName("Key0") as GumFormsSample.Components.KeyboardKeyRuntime;
        Row2Keys = this.GetGraphicalUiElementByName("Row2Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row3Keys = this.GetGraphicalUiElementByName("Row3Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row4Keys = this.GetGraphicalUiElementByName("Row4Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row5Keys = this.GetGraphicalUiElementByName("Row5Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        KeyBackspace = this.GetGraphicalUiElementByName("KeyBackspace") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyReturn = this.GetGraphicalUiElementByName("KeyReturn") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyLeft = this.GetGraphicalUiElementByName("KeyLeft") as GumFormsSample.Components.KeyboardKeyRuntime;
        KeyRight = this.GetGraphicalUiElementByName("KeyRight") as GumFormsSample.Components.KeyboardKeyRuntime;
        HighlightRectangle = this.GetGraphicalUiElementByName("HighlightRectangle") as global::MonoGameGum.GueDeriving.RectangleRuntime;
        IconInstance = this.GetGraphicalUiElementByName("IconInstance") as GumFormsSample.Components.IconRuntime;
        IconInstance1 = this.GetGraphicalUiElementByName("IconInstance1") as GumFormsSample.Components.IconRuntime;
        IconInstance2 = this.GetGraphicalUiElementByName("IconInstance2") as GumFormsSample.Components.IconRuntime;
        IconInstance3 = this.GetGraphicalUiElementByName("IconInstance3") as GumFormsSample.Components.IconRuntime;
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
