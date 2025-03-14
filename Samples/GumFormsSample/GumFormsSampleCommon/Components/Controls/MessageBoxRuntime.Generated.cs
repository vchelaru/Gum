//Code for Controls/MessageBox (Controls/UserControl)
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
    public partial class MessageBoxRuntime:UserControlRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/MessageBox", typeof(MessageBoxRuntime));
        }
        public LabelRuntime LabelInstance { get; protected set; }
        public ContainerRuntime ContainerInstance { get; protected set; }
        public ButtonConfirmRuntime OkButton { get; protected set; }
        public ButtonDenyRuntime CancelButton { get; protected set; }

        public MessageBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.AutoGridHorizontalCells = 4;
            this.AutoGridVerticalCells = 4;
            this.ChildrenLayout = global::Gum.Managers.ChildrenLayout.Regular;
            this.ClipsChildren = false;
             
            this.FlipHorizontal = false;
            this.HasEvents = true;
            this.Height = 214f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.IgnoredByParentSize = false;
             
            this.Rotation = 0f;
            this.StackSpacing = 0f;
            this.Visible = true;
            this.Width = 430f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.WrapsChildren = false;
            this.X = 0f;
            this.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Y = 0f;
            this.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.YUnits = GeneralUnitType.PixelsFromMiddle;


            ApplyDefaultVariables();
            if(tryCreateFormsObject)
            {
            }
            CustomInitialize();
        }
        protected override void InitializeInstances()
        {
            base.InitializeInstances();
            LabelInstance = new LabelRuntime();
            LabelInstance.Name = "LabelInstance";
            ContainerInstance = new ContainerRuntime();
            ContainerInstance.Name = "ContainerInstance";
            OkButton = new ButtonConfirmRuntime();
            OkButton.Name = "OkButton";
            CancelButton = new ButtonDenyRuntime();
            CancelButton.Name = "CancelButton";
        }
        protected override void AssignParents()
        {
            // Intentionally do not call base.AssignParents so that this class can determine the addition of order
            this.Children.Add(Background);
            this.Children.Add(LabelInstance);
            this.Children.Add(ContainerInstance);
            ContainerInstance.Children.Add(OkButton);
            ContainerInstance.Children.Add(CancelButton);
        }
        private void ApplyDefaultVariables()
        {

            this.LabelInstance.Height = 48f;
            this.LabelInstance.LabelText = @"This is a popup. You can change this text if you'd like...";
            this.LabelInstance.Width = -20f;
            this.LabelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.LabelInstance.X = 0f;
            this.LabelInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.LabelInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.LabelInstance.Y = 10f;
            this.LabelInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.LabelInstance.YUnits = GeneralUnitType.PixelsFromSmall;

            this.ContainerInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            this.ContainerInstance.Height = 29f;
            this.ContainerInstance.StackSpacing = 5f;
            this.ContainerInstance.Width = 0f;
            this.ContainerInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.ContainerInstance.X = -10f;
            this.ContainerInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.ContainerInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            this.ContainerInstance.Y = -10f;
            this.ContainerInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Bottom;
            this.ContainerInstance.YUnits = GeneralUnitType.PixelsFromLarge;



        }
        partial void CustomInitialize();
    }
}
