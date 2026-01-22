//Code for Controls/Keyboard (Container)
using CodeGenProject.Components.Controls;
using CodeGenProject.Components.Elements;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System.Linq;
namespace CodeGenProject.Components.Controls;
partial class Keyboard : global::Gum.Forms.Controls.FrameworkElement
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void RegisterRuntimeType()
    {
        var template = new global::Gum.Forms.VisualTemplate((vm, createForms) =>
        {
            var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();
            var element = ObjectFinder.Self.GetElementSave("Controls/Keyboard");
#if DEBUG
if(element == null) throw new System.InvalidOperationException("Could not find an element named Controls/Keyboard - did you forget to load a Gum project?");
#endif
            element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);
            if(createForms) visual.FormsControlAsObject = new Keyboard(visual);
            return visual;
        });
        global::Gum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof(Keyboard)] = template;
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
                    var category = ((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories.FirstOrDefault(item => item.Name == "CursorMoveCategory");
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

    public Keyboard(InteractiveGue visual) : base(visual)
    {
    }
    public Keyboard()
    {



    }
    protected override void ReactToVisualChanged()
    {
        base.ReactToVisualChanged();
        Row1Keys = this.Visual?.GetGraphicalUiElementByName("Row1Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        AllRows = this.Visual?.GetGraphicalUiElementByName("AllRows") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Key1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key1");
        KeyQ = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyQ");
        KeyA = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyA");
        KeyZ = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyZ");
        KeyParenLeft = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyParenLeft");
        KeyW = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyW");
        KeyS = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyS");
        KeyX = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyX");
        KeyParenRight = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyParenRight");
        KeyE = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyE");
        KeyD = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyD");
        KeyC = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyC");
        KeySpace = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeySpace");
        KeyR = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyR");
        KeyF = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyF");
        KeyV = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyV");
        KeyT = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyT");
        KeyG = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyG");
        KeyB = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyB");
        KeyY = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyY");
        KeyH = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyH");
        KeyN = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyN");
        KeyU = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyU");
        KeyJ = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyJ");
        KeyM = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyM");
        KeyI = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyI");
        KeyK = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyK");
        KeyComma = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyComma");
        KeyQuestion = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyQuestion");
        KeyO = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyO");
        KeyL = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyL");
        KeyPeriod = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyPeriod");
        KeyBang = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyBang");
        KeyP = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyP");
        KeyUnderscore = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyUnderscore");
        KeyHyphen = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyHyphen");
        KeyAmpersand = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyAmpersand");
        Key2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key2");
        Key3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key3");
        Key4 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key4");
        Key5 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key5");
        Key6 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key6");
        Key7 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key7");
        Key8 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key8");
        Key9 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key9");
        Key0 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"Key0");
        Row2Keys = this.Visual?.GetGraphicalUiElementByName("Row2Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row3Keys = this.Visual?.GetGraphicalUiElementByName("Row3Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row4Keys = this.Visual?.GetGraphicalUiElementByName("Row4Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        Row5Keys = this.Visual?.GetGraphicalUiElementByName("Row5Keys") as global::MonoGameGum.GueDeriving.ContainerRuntime;
        KeyBackspace = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyBackspace");
        KeyReturn = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyReturn");
        KeyLeft = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyLeft");
        KeyRight = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<KeyboardKey>(this.Visual,"KeyRight");
        HighlightRectangle = this.Visual?.GetGraphicalUiElementByName("HighlightRectangle") as global::MonoGameGum.GueDeriving.RectangleRuntime;
        IconInstance = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance");
        IconInstance1 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance1");
        IconInstance2 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance2");
        IconInstance3 = global::Gum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<Icon>(this.Visual,"IconInstance3");
        CustomInitialize();
    }
    //Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code
    partial void CustomInitialize();
}
