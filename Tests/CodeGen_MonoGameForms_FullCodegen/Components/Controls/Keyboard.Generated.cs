//Code for Controls/Keyboard (Container)
using CodeGen_MonoGameForms_FullCodegen.Components.Controls;
using CodeGen_MonoGameForms_FullCodegen.Components.Elements;
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
namespace CodeGen_MonoGameForms_FullCodegen.Components.Controls;
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

    private CursorMoveCategory? _cursorMoveCategoryState;
    public CursorMoveCategory? CursorMoveCategoryState
    {
        get => _cursorMoveCategoryState;
        set
        {
            _cursorMoveCategoryState = value;
            var appliedDynamically = false;
            if(!appliedDynamically)
            {
                switch (value)
                {
                    case CursorMoveCategory.LeftRightMoveSupported:
                        this.KeyLeft.Visual.Visible = true;
                        this.KeyRight.Visual.Visible = true;
                        break;
                    case CursorMoveCategory.NoMovement:
                        this.KeyLeft.Visual.Visible = false;
                        this.KeyRight.Visual.Visible = false;
                        break;
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
        InitializeInstances();
        CustomInitialize();
    }
    public Keyboard() : base(new ContainerRuntime())
    {

        this.Visual.Height = 0f;
        this.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
         
         
        this.Visual.Width = 0f;
        this.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;

        InitializeInstances();

        ApplyDefaultVariables();
        AssignParents();
        CustomInitialize();
    }
    protected virtual void InitializeInstances()
    {
        Row1Keys = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        Row1Keys.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (Row1Keys.ElementSave != null) Row1Keys.AddStatesAndCategoriesRecursivelyToGue(Row1Keys.ElementSave);
        if (Row1Keys.ElementSave != null) Row1Keys.SetInitialState();
        Row1Keys.Name = "Row1Keys";
        AllRows = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        AllRows.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (AllRows.ElementSave != null) AllRows.AddStatesAndCategoriesRecursivelyToGue(AllRows.ElementSave);
        if (AllRows.ElementSave != null) AllRows.SetInitialState();
        AllRows.Name = "AllRows";
        Key1 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key1.Name = "Key1";
        KeyQ = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyQ.Name = "KeyQ";
        KeyA = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyA.Name = "KeyA";
        KeyZ = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyZ.Name = "KeyZ";
        KeyParenLeft = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyParenLeft.Name = "KeyParenLeft";
        KeyW = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyW.Name = "KeyW";
        KeyS = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyS.Name = "KeyS";
        KeyX = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyX.Name = "KeyX";
        KeyParenRight = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyParenRight.Name = "KeyParenRight";
        KeyE = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyE.Name = "KeyE";
        KeyD = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyD.Name = "KeyD";
        KeyC = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyC.Name = "KeyC";
        KeySpace = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeySpace.Name = "KeySpace";
        KeyR = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyR.Name = "KeyR";
        KeyF = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyF.Name = "KeyF";
        KeyV = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyV.Name = "KeyV";
        KeyT = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyT.Name = "KeyT";
        KeyG = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyG.Name = "KeyG";
        KeyB = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyB.Name = "KeyB";
        KeyY = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyY.Name = "KeyY";
        KeyH = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyH.Name = "KeyH";
        KeyN = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyN.Name = "KeyN";
        KeyU = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyU.Name = "KeyU";
        KeyJ = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyJ.Name = "KeyJ";
        KeyM = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyM.Name = "KeyM";
        KeyI = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyI.Name = "KeyI";
        KeyK = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyK.Name = "KeyK";
        KeyComma = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyComma.Name = "KeyComma";
        KeyQuestion = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyQuestion.Name = "KeyQuestion";
        KeyO = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyO.Name = "KeyO";
        KeyL = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyL.Name = "KeyL";
        KeyPeriod = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyPeriod.Name = "KeyPeriod";
        KeyBang = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyBang.Name = "KeyBang";
        KeyP = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyP.Name = "KeyP";
        KeyUnderscore = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyUnderscore.Name = "KeyUnderscore";
        KeyHyphen = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyHyphen.Name = "KeyHyphen";
        KeyAmpersand = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyAmpersand.Name = "KeyAmpersand";
        Key2 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key2.Name = "Key2";
        Key3 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key3.Name = "Key3";
        Key4 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key4.Name = "Key4";
        Key5 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key5.Name = "Key5";
        Key6 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key6.Name = "Key6";
        Key7 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key7.Name = "Key7";
        Key8 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key8.Name = "Key8";
        Key9 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key9.Name = "Key9";
        Key0 = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        Key0.Name = "Key0";
        Row2Keys = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        Row2Keys.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (Row2Keys.ElementSave != null) Row2Keys.AddStatesAndCategoriesRecursivelyToGue(Row2Keys.ElementSave);
        if (Row2Keys.ElementSave != null) Row2Keys.SetInitialState();
        Row2Keys.Name = "Row2Keys";
        Row3Keys = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        Row3Keys.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (Row3Keys.ElementSave != null) Row3Keys.AddStatesAndCategoriesRecursivelyToGue(Row3Keys.ElementSave);
        if (Row3Keys.ElementSave != null) Row3Keys.SetInitialState();
        Row3Keys.Name = "Row3Keys";
        Row4Keys = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        Row4Keys.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (Row4Keys.ElementSave != null) Row4Keys.AddStatesAndCategoriesRecursivelyToGue(Row4Keys.ElementSave);
        if (Row4Keys.ElementSave != null) Row4Keys.SetInitialState();
        Row4Keys.Name = "Row4Keys";
        Row5Keys = new global::MonoGameGum.GueDeriving.ContainerRuntime();
        Row5Keys.ElementSave = ObjectFinder.Self.GetStandardElement("Container");
        if (Row5Keys.ElementSave != null) Row5Keys.AddStatesAndCategoriesRecursivelyToGue(Row5Keys.ElementSave);
        if (Row5Keys.ElementSave != null) Row5Keys.SetInitialState();
        Row5Keys.Name = "Row5Keys";
        KeyBackspace = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyBackspace.Name = "KeyBackspace";
        KeyReturn = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyReturn.Name = "KeyReturn";
        KeyLeft = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyLeft.Name = "KeyLeft";
        KeyRight = new CodeGen_MonoGameForms_FullCodegen.Components.Controls.KeyboardKey();
        KeyRight.Name = "KeyRight";
        HighlightRectangle = new global::MonoGameGum.GueDeriving.RectangleRuntime();
        HighlightRectangle.ElementSave = ObjectFinder.Self.GetStandardElement("Rectangle");
        if (HighlightRectangle.ElementSave != null) HighlightRectangle.AddStatesAndCategoriesRecursivelyToGue(HighlightRectangle.ElementSave);
        if (HighlightRectangle.ElementSave != null) HighlightRectangle.SetInitialState();
        HighlightRectangle.Name = "HighlightRectangle";
        IconInstance = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        IconInstance.Name = "IconInstance";
        IconInstance1 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        IconInstance1.Name = "IconInstance1";
        IconInstance2 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        IconInstance2.Name = "IconInstance2";
        IconInstance3 = new CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon();
        IconInstance3.Name = "IconInstance3";
        base.RefreshInternalVisualReferences();
    }
    protected virtual void AssignParents()
    {
        AllRows.AddChild(Row1Keys);
        this.AddChild(AllRows);
        Row1Keys.AddChild(Key1);
        Row2Keys.AddChild(KeyQ);
        Row3Keys.AddChild(KeyA);
        Row4Keys.AddChild(KeyZ);
        Row5Keys.AddChild(KeyParenLeft);
        Row2Keys.AddChild(KeyW);
        Row3Keys.AddChild(KeyS);
        Row4Keys.AddChild(KeyX);
        Row5Keys.AddChild(KeyParenRight);
        Row2Keys.AddChild(KeyE);
        Row3Keys.AddChild(KeyD);
        Row4Keys.AddChild(KeyC);
        Row5Keys.AddChild(KeySpace);
        Row2Keys.AddChild(KeyR);
        Row3Keys.AddChild(KeyF);
        Row4Keys.AddChild(KeyV);
        Row2Keys.AddChild(KeyT);
        Row3Keys.AddChild(KeyG);
        Row4Keys.AddChild(KeyB);
        Row2Keys.AddChild(KeyY);
        Row3Keys.AddChild(KeyH);
        Row4Keys.AddChild(KeyN);
        Row2Keys.AddChild(KeyU);
        Row3Keys.AddChild(KeyJ);
        Row4Keys.AddChild(KeyM);
        Row2Keys.AddChild(KeyI);
        Row3Keys.AddChild(KeyK);
        Row4Keys.AddChild(KeyComma);
        Row5Keys.AddChild(KeyQuestion);
        Row2Keys.AddChild(KeyO);
        Row3Keys.AddChild(KeyL);
        Row4Keys.AddChild(KeyPeriod);
        Row5Keys.AddChild(KeyBang);
        Row2Keys.AddChild(KeyP);
        Row3Keys.AddChild(KeyUnderscore);
        Row4Keys.AddChild(KeyHyphen);
        Row5Keys.AddChild(KeyAmpersand);
        Row1Keys.AddChild(Key2);
        Row1Keys.AddChild(Key3);
        Row1Keys.AddChild(Key4);
        Row1Keys.AddChild(Key5);
        Row1Keys.AddChild(Key6);
        Row1Keys.AddChild(Key7);
        Row1Keys.AddChild(Key8);
        Row1Keys.AddChild(Key9);
        Row1Keys.AddChild(Key0);
        AllRows.AddChild(Row2Keys);
        AllRows.AddChild(Row3Keys);
        AllRows.AddChild(Row4Keys);
        AllRows.AddChild(Row5Keys);
        this.AddChild(KeyBackspace);
        this.AddChild(KeyReturn);
        this.AddChild(KeyLeft);
        this.AddChild(KeyRight);
        this.AddChild(HighlightRectangle);
        KeyBackspace.AddChild(IconInstance);
        KeyReturn.AddChild(IconInstance1);
        KeyLeft.AddChild(IconInstance2);
        KeyRight.AddChild(IconInstance3);
    }
    private void ApplyDefaultVariables()
    {
        this.Row1Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.Row1Keys.Height = 20f;
        this.Row1Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Row1Keys.Width = 0f;
        this.Row1Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Row1Keys.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.Row1Keys.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.Row1Keys.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
        this.Row1Keys.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;

        this.AllRows.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
        this.AllRows.Height = 0f;
        this.AllRows.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.AllRows.Width = 90f;
        this.AllRows.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.AllRows.X = 0f;
        this.AllRows.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        this.AllRows.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        this.AllRows.Y = 0f;
        this.AllRows.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.AllRows.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key1.Visual.Height = 0f;
        this.Key1.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key1.Text = @"1";
        this.Key1.Visual.Width = 10f;
        this.Key1.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key1.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key1.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyQ.Visual.Height = 0f;
        this.KeyQ.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyQ.Text = @"Q";
        this.KeyQ.Visual.Width = 10f;
        this.KeyQ.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyQ.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyQ.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyA.Visual.Height = 0f;
        this.KeyA.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyA.Text = @"A";
        this.KeyA.Visual.Width = 10f;
        this.KeyA.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyA.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyA.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyZ.Visual.Height = 0f;
        this.KeyZ.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyZ.Text = @"Z";
        this.KeyZ.Visual.Width = 10f;
        this.KeyZ.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyZ.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyZ.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyParenLeft.Visual.Height = 0f;
        this.KeyParenLeft.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyParenLeft.Text = @"(";
        this.KeyParenLeft.Visual.Width = 10f;
        this.KeyParenLeft.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyParenLeft.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyParenLeft.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyW.Visual.Height = 0f;
        this.KeyW.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyW.Text = @"W";
        this.KeyW.Visual.Width = 10f;
        this.KeyW.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyW.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyW.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyS.Visual.Height = 0f;
        this.KeyS.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyS.Text = @"S";
        this.KeyS.Visual.Width = 10f;
        this.KeyS.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyS.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyS.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyX.Visual.Height = 0f;
        this.KeyX.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyX.Text = @"X";
        this.KeyX.Visual.Width = 10f;
        this.KeyX.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyX.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyX.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyParenRight.Visual.Height = 0f;
        this.KeyParenRight.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyParenRight.Text = @")";
        this.KeyParenRight.Visual.Width = 10f;
        this.KeyParenRight.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyParenRight.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyParenRight.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyE.Visual.Height = 0f;
        this.KeyE.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyE.Text = @"E";
        this.KeyE.Visual.Width = 10f;
        this.KeyE.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyE.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyE.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyD.Visual.Height = 0f;
        this.KeyD.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyD.Text = @"D";
        this.KeyD.Visual.Width = 10f;
        this.KeyD.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyD.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyD.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyC.Visual.Height = 0f;
        this.KeyC.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyC.Text = @"C";
        this.KeyC.Visual.Width = 10f;
        this.KeyC.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyC.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyC.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeySpace.Visual.Height = 0f;
        this.KeySpace.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeySpace.Text = @"SPACE";
        this.KeySpace.Visual.Width = 50f;
        this.KeySpace.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeySpace.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeySpace.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyR.Visual.Height = 0f;
        this.KeyR.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyR.Text = @"R";
        this.KeyR.Visual.Width = 10f;
        this.KeyR.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyR.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyR.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyF.Visual.Height = 0f;
        this.KeyF.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyF.Text = @"F";
        this.KeyF.Visual.Width = 10f;
        this.KeyF.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyF.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyF.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyV.Visual.Height = 0f;
        this.KeyV.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyV.Text = @"V";
        this.KeyV.Visual.Width = 10f;
        this.KeyV.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyV.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyV.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyT.Visual.Height = 0f;
        this.KeyT.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyT.Text = @"T";
        this.KeyT.Visual.Width = 10f;
        this.KeyT.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyT.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyT.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyG.Visual.Height = 0f;
        this.KeyG.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyG.Text = @"G";
        this.KeyG.Visual.Width = 10f;
        this.KeyG.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyG.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyG.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyB.Visual.Height = 0f;
        this.KeyB.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyB.Text = @"B";
        this.KeyB.Visual.Width = 10f;
        this.KeyB.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyB.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyB.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyY.Visual.Height = 0f;
        this.KeyY.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyY.Text = @"Y";
        this.KeyY.Visual.Width = 10f;
        this.KeyY.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyY.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyY.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyH.Visual.Height = 0f;
        this.KeyH.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyH.Text = @"H";
        this.KeyH.Visual.Width = 10f;
        this.KeyH.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyH.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyH.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyN.Visual.Height = 0f;
        this.KeyN.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyN.Text = @"N";
        this.KeyN.Visual.Width = 10f;
        this.KeyN.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyN.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyN.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyU.Visual.Height = 0f;
        this.KeyU.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyU.Text = @"U";
        this.KeyU.Visual.Width = 10f;
        this.KeyU.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyU.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyU.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyJ.Visual.Height = 0f;
        this.KeyJ.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyJ.Text = @"J";
        this.KeyJ.Visual.Width = 10f;
        this.KeyJ.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyJ.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyJ.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyM.Visual.Height = 0f;
        this.KeyM.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyM.Text = @"M";
        this.KeyM.Visual.Width = 10f;
        this.KeyM.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyM.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyM.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyI.Visual.Height = 0f;
        this.KeyI.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyI.Text = @"I";
        this.KeyI.Visual.Width = 10f;
        this.KeyI.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyI.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyI.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyK.Visual.Height = 0f;
        this.KeyK.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyK.Text = @"K";
        this.KeyK.Visual.Width = 10f;
        this.KeyK.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyK.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyK.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyComma.Visual.Height = 0f;
        this.KeyComma.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyComma.Text = @",";
        this.KeyComma.Visual.Width = 10f;
        this.KeyComma.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyComma.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyComma.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyQuestion.Visual.Height = 0f;
        this.KeyQuestion.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyQuestion.Text = @"?";
        this.KeyQuestion.Visual.Width = 10f;
        this.KeyQuestion.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyQuestion.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyQuestion.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyO.Visual.Height = 0f;
        this.KeyO.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyO.Text = @"O";
        this.KeyO.Visual.Width = 10f;
        this.KeyO.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyO.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyO.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyL.Visual.Height = 0f;
        this.KeyL.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyL.Text = @"L";
        this.KeyL.Visual.Width = 10f;
        this.KeyL.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyL.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyL.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyPeriod.Visual.Height = 0f;
        this.KeyPeriod.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyPeriod.Text = @".";
        this.KeyPeriod.Visual.Width = 10f;
        this.KeyPeriod.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyPeriod.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyPeriod.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyBang.Visual.Height = 0f;
        this.KeyBang.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyBang.Text = @"!";
        this.KeyBang.Visual.Width = 10f;
        this.KeyBang.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyBang.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyBang.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyP.Visual.Height = 0f;
        this.KeyP.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyP.Text = @"P";
        this.KeyP.Visual.Width = 10f;
        this.KeyP.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyP.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyP.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyUnderscore.Visual.Height = 0f;
        this.KeyUnderscore.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyUnderscore.Text = @"_";
        this.KeyUnderscore.Visual.Width = 10f;
        this.KeyUnderscore.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyUnderscore.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyUnderscore.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyHyphen.Visual.Height = 0f;
        this.KeyHyphen.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyHyphen.Text = @"-";
        this.KeyHyphen.Visual.Width = 10f;
        this.KeyHyphen.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyHyphen.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyHyphen.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.KeyAmpersand.Visual.Height = 0f;
        this.KeyAmpersand.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.KeyAmpersand.Text = @"&";
        this.KeyAmpersand.Visual.Width = 10f;
        this.KeyAmpersand.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyAmpersand.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.KeyAmpersand.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key2.Visual.Height = 0f;
        this.Key2.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key2.Text = @"2";
        this.Key2.Visual.Width = 10f;
        this.Key2.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key2.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key2.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key3.Visual.Height = 0f;
        this.Key3.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key3.Text = @"3";
        this.Key3.Visual.Width = 10f;
        this.Key3.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key3.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key3.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key4.Visual.Height = 0f;
        this.Key4.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key4.Text = @"4";
        this.Key4.Visual.Width = 10f;
        this.Key4.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key4.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key4.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key5.Visual.Height = 0f;
        this.Key5.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key5.Text = @"5";
        this.Key5.Visual.Width = 10f;
        this.Key5.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key5.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key5.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key6.Visual.Height = 0f;
        this.Key6.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key6.Text = @"6";
        this.Key6.Visual.Width = 10f;
        this.Key6.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key6.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key6.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key7.Visual.Height = 0f;
        this.Key7.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key7.Text = @"7";
        this.Key7.Visual.Width = 10f;
        this.Key7.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key7.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key7.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key8.Visual.Height = 0f;
        this.Key8.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key8.Text = @"8";
        this.Key8.Visual.Width = 10f;
        this.Key8.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key8.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key8.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key9.Visual.Height = 0f;
        this.Key9.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key9.Text = @"9";
        this.Key9.Visual.Width = 10f;
        this.Key9.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key9.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key9.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Key0.Visual.Height = 0f;
        this.Key0.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.Key0.Text = @"0";
        this.Key0.Visual.Width = 10f;
        this.Key0.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Key0.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.Key0.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.Row2Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.Row2Keys.Height = 20f;
        this.Row2Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Row2Keys.Width = 0f;
        this.Row2Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.Row3Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.Row3Keys.Height = 20f;
        this.Row3Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Row3Keys.Width = 0f;
        this.Row3Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.Row4Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.Row4Keys.Height = 20f;
        this.Row4Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Row4Keys.Width = 0f;
        this.Row4Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.Row5Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
        this.Row5Keys.Height = 20f;
        this.Row5Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.Row5Keys.Width = 0f;
        this.Row5Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;

        this.KeyBackspace.Visual.Height = 20f;
        this.KeyBackspace.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyBackspace.Text = @"";
        this.KeyBackspace.Visual.Width = 10f;
        this.KeyBackspace.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyBackspace.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.KeyBackspace.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.KeyReturn.Visual.Height = 40f;
        this.KeyReturn.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyReturn.Text = @"";
        this.KeyReturn.Visual.Width = 10f;
        this.KeyReturn.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyReturn.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.KeyReturn.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.KeyReturn.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
        this.KeyReturn.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;

        this.KeyLeft.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyLeft.Text = @"";
        this.KeyLeft.Visual.Width = 10f;
        this.KeyLeft.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyLeft.Visual.X = 0f;
        this.KeyLeft.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.KeyLeft.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.KeyLeft.Visual.Y = 20f;
        this.KeyLeft.Visual.YUnits = global::Gum.Converters.GeneralUnitType.Percentage;

        this.KeyRight.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyRight.Text = @"";
        this.KeyRight.Visual.Width = 10f;
        this.KeyRight.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.PercentageOfParent;
        this.KeyRight.Visual.X = 0f;
        this.KeyRight.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
        this.KeyRight.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromLarge;
        this.KeyRight.Visual.Y = 40f;
        this.KeyRight.Visual.YUnits = global::Gum.Converters.GeneralUnitType.Percentage;

        this.HighlightRectangle.Blue = 0;
        this.HighlightRectangle.Green = 128;
        this.HighlightRectangle.Height = -6f;
        this.HighlightRectangle.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.HighlightRectangle.Red = 0;
        this.HighlightRectangle.Visible = false;
        this.HighlightRectangle.Width = -6f;
        this.HighlightRectangle.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        this.HighlightRectangle.X = 0f;
        this.HighlightRectangle.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.HighlightRectangle.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.HighlightRectangle.Y = -1f;
        this.HighlightRectangle.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.HighlightRectangle.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.IconInstance.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Delete;
        this.IconInstance.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.IconInstance.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.IconInstance.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.IconInstance.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.IconInstance1.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Enter;
        this.IconInstance1.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.IconInstance1.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.IconInstance1.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.IconInstance1.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.IconInstance2.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Arrow1;
        this.IconInstance2.Visual.FlipHorizontal = true;
        this.IconInstance2.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.IconInstance2.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.IconInstance2.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.IconInstance2.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

        this.IconInstance3.IconCategoryState = CodeGen_MonoGameForms_FullCodegen.Components.Elements.Icon.IconCategory.Arrow1;
        this.IconInstance3.Visual.FlipHorizontal = false;
        this.IconInstance3.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
        this.IconInstance3.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;
        this.IconInstance3.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
        this.IconInstance3.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromMiddle;

    }
    partial void CustomInitialize();
}
