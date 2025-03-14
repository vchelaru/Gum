//Code for Controls/Keyboard (Container)
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
    public partial class KeyboardRuntime:ContainerRuntime
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

        CursorMoveCategory mCursorMoveCategoryState;
        public CursorMoveCategory CursorMoveCategoryState
        {
            get => mCursorMoveCategoryState;
            set
            {
                mCursorMoveCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case CursorMoveCategory.LeftRightMoveSupported:
                            this.KeyLeft.Visible = true;
                            this.KeyRight.Visible = true;
                            break;
                        case CursorMoveCategory.NoMovement:
                            this.KeyLeft.Visible = false;
                            this.KeyRight.Visible = false;
                            break;
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
            }

            this.Height = 0f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
             
             
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;

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
            Row1Keys = new ContainerRuntime();
            Row1Keys.Name = "Row1Keys";
            AllRows = new ContainerRuntime();
            AllRows.Name = "AllRows";
            Key1 = new KeyboardKeyRuntime();
            Key1.Name = "Key1";
            KeyQ = new KeyboardKeyRuntime();
            KeyQ.Name = "KeyQ";
            KeyA = new KeyboardKeyRuntime();
            KeyA.Name = "KeyA";
            KeyZ = new KeyboardKeyRuntime();
            KeyZ.Name = "KeyZ";
            KeyParenLeft = new KeyboardKeyRuntime();
            KeyParenLeft.Name = "KeyParenLeft";
            KeyW = new KeyboardKeyRuntime();
            KeyW.Name = "KeyW";
            KeyS = new KeyboardKeyRuntime();
            KeyS.Name = "KeyS";
            KeyX = new KeyboardKeyRuntime();
            KeyX.Name = "KeyX";
            KeyParenRight = new KeyboardKeyRuntime();
            KeyParenRight.Name = "KeyParenRight";
            KeyE = new KeyboardKeyRuntime();
            KeyE.Name = "KeyE";
            KeyD = new KeyboardKeyRuntime();
            KeyD.Name = "KeyD";
            KeyC = new KeyboardKeyRuntime();
            KeyC.Name = "KeyC";
            KeySpace = new KeyboardKeyRuntime();
            KeySpace.Name = "KeySpace";
            KeyR = new KeyboardKeyRuntime();
            KeyR.Name = "KeyR";
            KeyF = new KeyboardKeyRuntime();
            KeyF.Name = "KeyF";
            KeyV = new KeyboardKeyRuntime();
            KeyV.Name = "KeyV";
            KeyT = new KeyboardKeyRuntime();
            KeyT.Name = "KeyT";
            KeyG = new KeyboardKeyRuntime();
            KeyG.Name = "KeyG";
            KeyB = new KeyboardKeyRuntime();
            KeyB.Name = "KeyB";
            KeyY = new KeyboardKeyRuntime();
            KeyY.Name = "KeyY";
            KeyH = new KeyboardKeyRuntime();
            KeyH.Name = "KeyH";
            KeyN = new KeyboardKeyRuntime();
            KeyN.Name = "KeyN";
            KeyU = new KeyboardKeyRuntime();
            KeyU.Name = "KeyU";
            KeyJ = new KeyboardKeyRuntime();
            KeyJ.Name = "KeyJ";
            KeyM = new KeyboardKeyRuntime();
            KeyM.Name = "KeyM";
            KeyI = new KeyboardKeyRuntime();
            KeyI.Name = "KeyI";
            KeyK = new KeyboardKeyRuntime();
            KeyK.Name = "KeyK";
            KeyComma = new KeyboardKeyRuntime();
            KeyComma.Name = "KeyComma";
            KeyQuestion = new KeyboardKeyRuntime();
            KeyQuestion.Name = "KeyQuestion";
            KeyO = new KeyboardKeyRuntime();
            KeyO.Name = "KeyO";
            KeyL = new KeyboardKeyRuntime();
            KeyL.Name = "KeyL";
            KeyPeriod = new KeyboardKeyRuntime();
            KeyPeriod.Name = "KeyPeriod";
            KeyBang = new KeyboardKeyRuntime();
            KeyBang.Name = "KeyBang";
            KeyP = new KeyboardKeyRuntime();
            KeyP.Name = "KeyP";
            KeyUnderscore = new KeyboardKeyRuntime();
            KeyUnderscore.Name = "KeyUnderscore";
            KeyHyphen = new KeyboardKeyRuntime();
            KeyHyphen.Name = "KeyHyphen";
            KeyAmpersand = new KeyboardKeyRuntime();
            KeyAmpersand.Name = "KeyAmpersand";
            Key2 = new KeyboardKeyRuntime();
            Key2.Name = "Key2";
            Key3 = new KeyboardKeyRuntime();
            Key3.Name = "Key3";
            Key4 = new KeyboardKeyRuntime();
            Key4.Name = "Key4";
            Key5 = new KeyboardKeyRuntime();
            Key5.Name = "Key5";
            Key6 = new KeyboardKeyRuntime();
            Key6.Name = "Key6";
            Key7 = new KeyboardKeyRuntime();
            Key7.Name = "Key7";
            Key8 = new KeyboardKeyRuntime();
            Key8.Name = "Key8";
            Key9 = new KeyboardKeyRuntime();
            Key9.Name = "Key9";
            Key0 = new KeyboardKeyRuntime();
            Key0.Name = "Key0";
            Row2Keys = new ContainerRuntime();
            Row2Keys.Name = "Row2Keys";
            Row3Keys = new ContainerRuntime();
            Row3Keys.Name = "Row3Keys";
            Row4Keys = new ContainerRuntime();
            Row4Keys.Name = "Row4Keys";
            Row5Keys = new ContainerRuntime();
            Row5Keys.Name = "Row5Keys";
            KeyBackspace = new KeyboardKeyRuntime();
            KeyBackspace.Name = "KeyBackspace";
            KeyReturn = new KeyboardKeyRuntime();
            KeyReturn.Name = "KeyReturn";
            KeyLeft = new KeyboardKeyRuntime();
            KeyLeft.Name = "KeyLeft";
            KeyRight = new KeyboardKeyRuntime();
            KeyRight.Name = "KeyRight";
            HighlightRectangle = new RectangleRuntime();
            HighlightRectangle.Name = "HighlightRectangle";
            IconInstance = new IconRuntime();
            IconInstance.Name = "IconInstance";
            IconInstance1 = new IconRuntime();
            IconInstance1.Name = "IconInstance1";
            IconInstance2 = new IconRuntime();
            IconInstance2.Name = "IconInstance2";
            IconInstance3 = new IconRuntime();
            IconInstance3.Name = "IconInstance3";
        }
        protected virtual void AssignParents()
        {
            AllRows.Children.Add(Row1Keys);
            this.Children.Add(AllRows);
            Row1Keys.Children.Add(Key1);
            Row2Keys.Children.Add(KeyQ);
            Row3Keys.Children.Add(KeyA);
            Row4Keys.Children.Add(KeyZ);
            Row5Keys.Children.Add(KeyParenLeft);
            Row2Keys.Children.Add(KeyW);
            Row3Keys.Children.Add(KeyS);
            Row4Keys.Children.Add(KeyX);
            Row5Keys.Children.Add(KeyParenRight);
            Row2Keys.Children.Add(KeyE);
            Row3Keys.Children.Add(KeyD);
            Row4Keys.Children.Add(KeyC);
            Row5Keys.Children.Add(KeySpace);
            Row2Keys.Children.Add(KeyR);
            Row3Keys.Children.Add(KeyF);
            Row4Keys.Children.Add(KeyV);
            Row2Keys.Children.Add(KeyT);
            Row3Keys.Children.Add(KeyG);
            Row4Keys.Children.Add(KeyB);
            Row2Keys.Children.Add(KeyY);
            Row3Keys.Children.Add(KeyH);
            Row4Keys.Children.Add(KeyN);
            Row2Keys.Children.Add(KeyU);
            Row3Keys.Children.Add(KeyJ);
            Row4Keys.Children.Add(KeyM);
            Row2Keys.Children.Add(KeyI);
            Row3Keys.Children.Add(KeyK);
            Row4Keys.Children.Add(KeyComma);
            Row5Keys.Children.Add(KeyQuestion);
            Row2Keys.Children.Add(KeyO);
            Row3Keys.Children.Add(KeyL);
            Row4Keys.Children.Add(KeyPeriod);
            Row5Keys.Children.Add(KeyBang);
            Row2Keys.Children.Add(KeyP);
            Row3Keys.Children.Add(KeyUnderscore);
            Row4Keys.Children.Add(KeyHyphen);
            Row5Keys.Children.Add(KeyAmpersand);
            Row1Keys.Children.Add(Key2);
            Row1Keys.Children.Add(Key3);
            Row1Keys.Children.Add(Key4);
            Row1Keys.Children.Add(Key5);
            Row1Keys.Children.Add(Key6);
            Row1Keys.Children.Add(Key7);
            Row1Keys.Children.Add(Key8);
            Row1Keys.Children.Add(Key9);
            Row1Keys.Children.Add(Key0);
            AllRows.Children.Add(Row2Keys);
            AllRows.Children.Add(Row3Keys);
            AllRows.Children.Add(Row4Keys);
            AllRows.Children.Add(Row5Keys);
            this.Children.Add(KeyBackspace);
            this.Children.Add(KeyReturn);
            this.Children.Add(KeyLeft);
            this.Children.Add(KeyRight);
            this.Children.Add(HighlightRectangle);
            KeyBackspace.Children.Add(IconInstance);
            KeyReturn.Children.Add(IconInstance1);
            KeyLeft.Children.Add(IconInstance2);
            KeyRight.Children.Add(IconInstance3);
        }
        private void ApplyDefaultVariables()
        {
            this.Row1Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.Row1Keys.Height = 20f;
            this.Row1Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Row1Keys.Width = 0f;
            this.Row1Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Row1Keys.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.Row1Keys.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Row1Keys.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.Row1Keys.YUnits = GeneralUnitType.PixelsFromSmall;

            this.AllRows.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.AllRows.Height = 0f;
            this.AllRows.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.AllRows.Width = 90f;
            this.AllRows.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.AllRows.X = 0f;
            this.AllRows.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.AllRows.XUnits = GeneralUnitType.PixelsFromSmall;
            this.AllRows.Y = 0f;
            this.AllRows.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.AllRows.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key1.Height = 0f;
            this.Key1.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key1.Text = @"1";
            this.Key1.Width = 10f;
            this.Key1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key1.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyQ.Height = 0f;
            this.KeyQ.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyQ.Text = @"Q";
            this.KeyQ.Width = 10f;
            this.KeyQ.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyQ.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyQ.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyA.Height = 0f;
            this.KeyA.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyA.Text = @"A";
            this.KeyA.Width = 10f;
            this.KeyA.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyA.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyA.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyZ.Height = 0f;
            this.KeyZ.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyZ.Text = @"Z";
            this.KeyZ.Width = 10f;
            this.KeyZ.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyZ.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyZ.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyParenLeft.Height = 0f;
            this.KeyParenLeft.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyParenLeft.Text = @"(";
            this.KeyParenLeft.Width = 10f;
            this.KeyParenLeft.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyParenLeft.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyParenLeft.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyW.Height = 0f;
            this.KeyW.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyW.Text = @"W";
            this.KeyW.Width = 10f;
            this.KeyW.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyW.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyW.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyS.Height = 0f;
            this.KeyS.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyS.Text = @"S";
            this.KeyS.Width = 10f;
            this.KeyS.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyS.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyS.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyX.Height = 0f;
            this.KeyX.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyX.Text = @"X";
            this.KeyX.Width = 10f;
            this.KeyX.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyX.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyX.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyParenRight.Height = 0f;
            this.KeyParenRight.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyParenRight.Text = @")";
            this.KeyParenRight.Width = 10f;
            this.KeyParenRight.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyParenRight.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyParenRight.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyE.Height = 0f;
            this.KeyE.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyE.Text = @"E";
            this.KeyE.Width = 10f;
            this.KeyE.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyE.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyE.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyD.Height = 0f;
            this.KeyD.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyD.Text = @"D";
            this.KeyD.Width = 10f;
            this.KeyD.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyD.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyD.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyC.Height = 0f;
            this.KeyC.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyC.Text = @"C";
            this.KeyC.Width = 10f;
            this.KeyC.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyC.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyC.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeySpace.Height = 0f;
            this.KeySpace.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeySpace.Text = @"SPACE";
            this.KeySpace.Width = 50f;
            this.KeySpace.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeySpace.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeySpace.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyR.Height = 0f;
            this.KeyR.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyR.Text = @"R";
            this.KeyR.Width = 10f;
            this.KeyR.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyR.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyR.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyF.Height = 0f;
            this.KeyF.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyF.Text = @"F";
            this.KeyF.Width = 10f;
            this.KeyF.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyF.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyF.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyV.Height = 0f;
            this.KeyV.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyV.Text = @"V";
            this.KeyV.Width = 10f;
            this.KeyV.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyV.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyV.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyT.Height = 0f;
            this.KeyT.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyT.Text = @"T";
            this.KeyT.Width = 10f;
            this.KeyT.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyT.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyT.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyG.Height = 0f;
            this.KeyG.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyG.Text = @"G";
            this.KeyG.Width = 10f;
            this.KeyG.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyG.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyG.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyB.Height = 0f;
            this.KeyB.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyB.Text = @"B";
            this.KeyB.Width = 10f;
            this.KeyB.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyB.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyB.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyY.Height = 0f;
            this.KeyY.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyY.Text = @"Y";
            this.KeyY.Width = 10f;
            this.KeyY.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyY.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyY.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyH.Height = 0f;
            this.KeyH.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyH.Text = @"H";
            this.KeyH.Width = 10f;
            this.KeyH.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyH.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyH.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyN.Height = 0f;
            this.KeyN.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyN.Text = @"N";
            this.KeyN.Width = 10f;
            this.KeyN.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyN.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyN.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyU.Height = 0f;
            this.KeyU.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyU.Text = @"U";
            this.KeyU.Width = 10f;
            this.KeyU.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyU.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyU.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyJ.Height = 0f;
            this.KeyJ.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyJ.Text = @"J";
            this.KeyJ.Width = 10f;
            this.KeyJ.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyJ.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyJ.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyM.Height = 0f;
            this.KeyM.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyM.Text = @"M";
            this.KeyM.Width = 10f;
            this.KeyM.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyM.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyM.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyI.Height = 0f;
            this.KeyI.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyI.Text = @"I";
            this.KeyI.Width = 10f;
            this.KeyI.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyI.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyI.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyK.Height = 0f;
            this.KeyK.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyK.Text = @"K";
            this.KeyK.Width = 10f;
            this.KeyK.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyK.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyK.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyComma.Height = 0f;
            this.KeyComma.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyComma.Text = @",";
            this.KeyComma.Width = 10f;
            this.KeyComma.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyComma.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyComma.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyQuestion.Height = 0f;
            this.KeyQuestion.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyQuestion.Text = @"?";
            this.KeyQuestion.Width = 10f;
            this.KeyQuestion.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyQuestion.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyQuestion.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyO.Height = 0f;
            this.KeyO.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyO.Text = @"O";
            this.KeyO.Width = 10f;
            this.KeyO.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyO.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyO.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyL.Height = 0f;
            this.KeyL.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyL.Text = @"L";
            this.KeyL.Width = 10f;
            this.KeyL.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyL.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyL.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyPeriod.Height = 0f;
            this.KeyPeriod.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyPeriod.Text = @".";
            this.KeyPeriod.Width = 10f;
            this.KeyPeriod.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyPeriod.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyPeriod.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyBang.Height = 0f;
            this.KeyBang.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyBang.Text = @"!";
            this.KeyBang.Width = 10f;
            this.KeyBang.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyBang.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyBang.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyP.Height = 0f;
            this.KeyP.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyP.Text = @"P";
            this.KeyP.Width = 10f;
            this.KeyP.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyP.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyP.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyUnderscore.Height = 0f;
            this.KeyUnderscore.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyUnderscore.Text = @"_";
            this.KeyUnderscore.Width = 10f;
            this.KeyUnderscore.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyUnderscore.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyUnderscore.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyHyphen.Height = 0f;
            this.KeyHyphen.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyHyphen.Text = @"-";
            this.KeyHyphen.Width = 10f;
            this.KeyHyphen.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyHyphen.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyHyphen.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.KeyAmpersand.Height = 0f;
            this.KeyAmpersand.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.KeyAmpersand.Text = @"&";
            this.KeyAmpersand.Width = 10f;
            this.KeyAmpersand.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyAmpersand.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.KeyAmpersand.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key2.Height = 0f;
            this.Key2.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key2.Text = @"2";
            this.Key2.Width = 10f;
            this.Key2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key2.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key2.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key3.Height = 0f;
            this.Key3.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key3.Text = @"3";
            this.Key3.Width = 10f;
            this.Key3.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key3.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key3.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key4.Height = 0f;
            this.Key4.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key4.Text = @"4";
            this.Key4.Width = 10f;
            this.Key4.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key4.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key4.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key5.Height = 0f;
            this.Key5.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key5.Text = @"5";
            this.Key5.Width = 10f;
            this.Key5.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key5.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key5.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key6.Height = 0f;
            this.Key6.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key6.Text = @"6";
            this.Key6.Width = 10f;
            this.Key6.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key6.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key6.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key7.Height = 0f;
            this.Key7.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key7.Text = @"7";
            this.Key7.Width = 10f;
            this.Key7.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key7.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key7.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key8.Height = 0f;
            this.Key8.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key8.Text = @"8";
            this.Key8.Width = 10f;
            this.Key8.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key8.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key8.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key9.Height = 0f;
            this.Key9.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key9.Text = @"9";
            this.Key9.Width = 10f;
            this.Key9.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key9.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key9.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Key0.Height = 0f;
            this.Key0.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Key0.Text = @"0";
            this.Key0.Width = 10f;
            this.Key0.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Key0.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Key0.YUnits = GeneralUnitType.PixelsFromMiddle;

            this.Row2Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.Row2Keys.Height = 20f;
            this.Row2Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Row2Keys.Width = 0f;
            this.Row2Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.Row3Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.Row3Keys.Height = 20f;
            this.Row3Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Row3Keys.Width = 0f;
            this.Row3Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.Row4Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.Row4Keys.Height = 20f;
            this.Row4Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Row4Keys.Width = 0f;
            this.Row4Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.Row5Keys.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.Row5Keys.Height = 20f;
            this.Row5Keys.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.Row5Keys.Width = 0f;
            this.Row5Keys.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            this.KeyBackspace.Height = 20f;
            this.KeyBackspace.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyBackspace.Text = @"";
            this.KeyBackspace.Width = 10f;
            this.KeyBackspace.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyBackspace.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.KeyBackspace.XUnits = GeneralUnitType.PixelsFromLarge;

            this.KeyReturn.Height = 40f;
            this.KeyReturn.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyReturn.Text = @"";
            this.KeyReturn.Width = 10f;
            this.KeyReturn.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyReturn.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.KeyReturn.XUnits = GeneralUnitType.PixelsFromLarge;
            this.KeyReturn.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.KeyReturn.YUnits = GeneralUnitType.PixelsFromLarge;

            this.KeyLeft.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyLeft.Text = @"";
            this.KeyLeft.Width = 10f;
            this.KeyLeft.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyLeft.X = 0f;
            this.KeyLeft.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.KeyLeft.XUnits = GeneralUnitType.PixelsFromLarge;
            this.KeyLeft.Y = 20f;
            this.KeyLeft.YUnits = GeneralUnitType.Percentage;

            this.KeyRight.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyRight.Text = @"";
            this.KeyRight.Width = 10f;
            this.KeyRight.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Percentage;
            this.KeyRight.X = 0f;
            this.KeyRight.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.KeyRight.XUnits = GeneralUnitType.PixelsFromLarge;
            this.KeyRight.Y = 40f;
            this.KeyRight.YUnits = GeneralUnitType.Percentage;

            this.HighlightRectangle.Blue = 0;
            this.HighlightRectangle.Green = 128;
            this.HighlightRectangle.Height = -6f;
            this.HighlightRectangle.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.HighlightRectangle.Red = 0;
            this.HighlightRectangle.Visible = false;
            this.HighlightRectangle.Width = -6f;
            this.HighlightRectangle.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.HighlightRectangle.X = 0f;
            this.HighlightRectangle.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.HighlightRectangle.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.HighlightRectangle.Y = -1f;
            this.HighlightRectangle.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.HighlightRectangle.YUnits = GeneralUnitType.PixelsFromMiddle;

this.IconInstance.IconCategoryState = IconRuntime.IconCategory.Delete;
            this.IconInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.IconInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.IconInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.IconInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

this.IconInstance1.IconCategoryState = IconRuntime.IconCategory.Enter;
            this.IconInstance1.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.IconInstance1.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.IconInstance1.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.IconInstance1.YUnits = GeneralUnitType.PixelsFromMiddle;

this.IconInstance2.IconCategoryState = IconRuntime.IconCategory.Arrow1;
            this.IconInstance2.FlipHorizontal = true;
            this.IconInstance2.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.IconInstance2.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.IconInstance2.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.IconInstance2.YUnits = GeneralUnitType.PixelsFromMiddle;

this.IconInstance3.IconCategoryState = IconRuntime.IconCategory.Arrow1;
            this.IconInstance3.FlipHorizontal = false;
            this.IconInstance3.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.IconInstance3.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.IconInstance3.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.IconInstance3.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
