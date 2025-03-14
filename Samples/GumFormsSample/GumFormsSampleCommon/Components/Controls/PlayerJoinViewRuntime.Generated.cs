//Code for Controls/PlayerJoinView (Container)
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
    public partial class PlayerJoinViewRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/PlayerJoinView", typeof(PlayerJoinViewRuntime));
        }
        public ContainerRuntime InnerPanelInstance { get; protected set; }
        public PlayerJoinViewItemRuntime PlayerJoinViewItem1 { get; protected set; }
        public PlayerJoinViewItemRuntime PlayerJoinViewItem2 { get; protected set; }
        public PlayerJoinViewItemRuntime PlayerJoinViewItem3 { get; protected set; }
        public PlayerJoinViewItemRuntime PlayerJoinViewItem4 { get; protected set; }

        public PlayerJoinViewRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 463f;
             
            this.Width = 144f;

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
            InnerPanelInstance = new ContainerRuntime();
            InnerPanelInstance.Name = "InnerPanelInstance";
            PlayerJoinViewItem1 = new PlayerJoinViewItemRuntime();
            PlayerJoinViewItem1.Name = "PlayerJoinViewItem1";
            PlayerJoinViewItem2 = new PlayerJoinViewItemRuntime();
            PlayerJoinViewItem2.Name = "PlayerJoinViewItem2";
            PlayerJoinViewItem3 = new PlayerJoinViewItemRuntime();
            PlayerJoinViewItem3.Name = "PlayerJoinViewItem3";
            PlayerJoinViewItem4 = new PlayerJoinViewItemRuntime();
            PlayerJoinViewItem4.Name = "PlayerJoinViewItem4";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(InnerPanelInstance);
            InnerPanelInstance.Children.Add(PlayerJoinViewItem1);
            InnerPanelInstance.Children.Add(PlayerJoinViewItem2);
            InnerPanelInstance.Children.Add(PlayerJoinViewItem3);
            InnerPanelInstance.Children.Add(PlayerJoinViewItem4);
        }
        private void ApplyDefaultVariables()
        {
            this.InnerPanelInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.InnerPanelInstance.Height = 0f;
            this.InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.InnerPanelInstance.StackSpacing = 32f;
            this.InnerPanelInstance.Width = 0f;
            this.InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.InnerPanelInstance.X = 0f;
            this.InnerPanelInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.InnerPanelInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.InnerPanelInstance.Y = 0f;
            this.InnerPanelInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.InnerPanelInstance.YUnits = GeneralUnitType.PixelsFromMiddle;





        }
        partial void CustomInitialize();
    }
}
