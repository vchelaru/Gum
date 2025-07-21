//Code for Controls/Keyboard (Container)
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using GameUiSamples.Components;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GameUiSamples.Components;
partial class Keyboard : MonoGameGum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new MonoGameGum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/Keyboard");
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Keyboard(visual);
            return visual;
        });
        MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Keyboard)] = template;
        ElementSaveExtensions.RegisterGueInstantiation("Controls/Keyboard", () => 
        {
            var gue = template.CreateContent(null, true) as InteractiveGue;
            return gue;
        });
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
                if(Visual.Categories.ContainsKey("CursorMoveCategory"))
                {
                    var category = Visual.Categories["CursorMoveCategory"];
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
                else
                {
                    var category = ((Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "CursorMoveCategory");
                    var state = category.States.Find(item => item.Name == value.ToString());
                    this.Visual.ApplyState(state);
                }
            }
        }
    }
    public ContainerRuntime Row1Keys { get; protected set; }
    public ContainerRuntime AllRows { get; protected set; }
    public KeyboardKey Key1 { get; protected set; }
    public KeyboardKey KeyQ { get; protected set; }
    public KeyboardKey KeyA { get; protected set; }
    public KeyboardKey KeyZ { get; protected set; }
    public KeyboardKey KeyParenLeft { get; protected set; }
    public KeyboardKey KeyW { get; protected set; }
    public KeyboardKey KeyS { get; protected set; }
    public KeyboardKey KeyX { get; protected set; }
    public KeyboardKey KeyParenRight { get; protected set; }
    public KeyboardKey KeyE { get; protected set; }
    public KeyboardKey KeyD { get; protected set; }
    public KeyboardKey KeyC { get; protected set; }
    public KeyboardKey KeySpace { get; protected set; }
    public KeyboardKey KeyR { get; protected set; }
    public KeyboardKey KeyF { get; protected set; }
    public KeyboardKey KeyV { get; protected set; }
    public KeyboardKey KeyT { get; protected set; }
    public KeyboardKey KeyG { get; protected set; }
    public KeyboardKey KeyB { get; protected set; }
    public KeyboardKey KeyY { get; protected set; }
    public KeyboardKey KeyH { get; protected set; }
    public KeyboardKey KeyN { get; protected set; }
    public KeyboardKey KeyU { get; protected set; }
    public KeyboardKey KeyJ { get; protected set; }
    public KeyboardKey KeyM { get; protected set; }
    public KeyboardKey KeyI { get; protected set; }
    public KeyboardKey KeyK { get; protected set; }
    public KeyboardKey KeyComma { get; protected set; }
    public KeyboardKey KeyQuestion { get; protected set; }
    public KeyboardKey KeyO { get; protected set; }
    public KeyboardKey KeyL { get; protected set; }
    public KeyboardKey KeyPeriod { get; protected set; }
    public KeyboardKey KeyBang { get; protected set; }
    public KeyboardKey KeyP { get; protected set; }
    public KeyboardKey KeyUnderscore { get; protected set; }
    public KeyboardKey KeyHyphen { get; protected set; }
    public KeyboardKey KeyAmpersand { get; protected set; }
    public KeyboardKey Key2 { get; protected set; }
    public KeyboardKey Key3 { get; protected set; }
    public KeyboardKey Key4 { get; protected set; }
    public KeyboardKey Key5 { get; protected set; }
    public KeyboardKey Key6 { get; protected set; }
    public KeyboardKey Key7 { get; protected set; }
    public KeyboardKey Key8 { get; protected set; }
    public KeyboardKey Key9 { get; protected set; }
    public KeyboardKey Key0 { get; protected set; }
    public ContainerRuntime Row2Keys { get; protected set; }
    public ContainerRuntime Row3Keys { get; protected set; }
    public ContainerRuntime Row4Keys { get; protected set; }
    public ContainerRuntime Row5Keys { get; protected set; }
    public KeyboardKey KeyBackspace { get; protected set; }
    public KeyboardKey KeyReturn { get; protected set; }
    public KeyboardKey KeyLeft { get; protected set; }
    public KeyboardKey KeyRight { get; protected set; }
    public RectangleRuntime HighlightRectangle { get; protected set; }
    public Icon IconInstance { get; protected set; }
    public Icon IconInstance1 { get; protected set; }
    public Icon IconInstance2 { get; protected set; }
    public Icon IconInstance3 { get; protected set; }

    public Keyboard(InteractiveGue visual) : base(visual) { }
    public Keyboard()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Row1Keys = this.Visual?.GetGraphicalUiElementByName("Row1Keys") as ContainerRuntime;
        AllRows = this.Visual?.GetGraphicalUiElementByName("AllRows") as ContainerRuntime;
        Key1 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key1");
        KeyQ = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyQ");
        KeyA = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyA");
        KeyZ = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyZ");
        KeyParenLeft = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyParenLeft");
        KeyW = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyW");
        KeyS = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyS");
        KeyX = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyX");
        KeyParenRight = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyParenRight");
        KeyE = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyE");
        KeyD = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyD");
        KeyC = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyC");
        KeySpace = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeySpace");
        KeyR = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyR");
        KeyF = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyF");
        KeyV = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyV");
        KeyT = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyT");
        KeyG = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyG");
        KeyB = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyB");
        KeyY = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyY");
        KeyH = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyH");
        KeyN = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyN");
        KeyU = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyU");
        KeyJ = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyJ");
        KeyM = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyM");
        KeyI = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyI");
        KeyK = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyK");
        KeyComma = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyComma");
        KeyQuestion = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyQuestion");
        KeyO = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyO");
        KeyL = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyL");
        KeyPeriod = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyPeriod");
        KeyBang = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyBang");
        KeyP = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyP");
        KeyUnderscore = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyUnderscore");
        KeyHyphen = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyHyphen");
        KeyAmpersand = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyAmpersand");
        Key2 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key2");
        Key3 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key3");
        Key4 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key4");
        Key5 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key5");
        Key6 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key6");
        Key7 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key7");
        Key8 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key8");
        Key9 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key9");
        Key0 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key0");
        Row2Keys = this.Visual?.GetGraphicalUiElementByName("Row2Keys") as ContainerRuntime;
        Row3Keys = this.Visual?.GetGraphicalUiElementByName("Row3Keys") as ContainerRuntime;
        Row4Keys = this.Visual?.GetGraphicalUiElementByName("Row4Keys") as ContainerRuntime;
        Row5Keys = this.Visual?.GetGraphicalUiElementByName("Row5Keys") as ContainerRuntime;
        KeyBackspace = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyBackspace");
        KeyReturn = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyReturn");
        KeyLeft = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyLeft");
        KeyRight = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyRight");
        HighlightRectangle = this.Visual?.GetGraphicalUiElementByName("HighlightRectangle") as RectangleRuntime;
        IconInstance = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance");
        IconInstance1 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance1");
        IconInstance2 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance2");
        IconInstance3 = MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance3");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
