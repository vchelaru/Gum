//Code for Controls/TreeViewItem (Container)
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
    public partial class TreeViewItemRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/TreeViewItem", typeof(TreeViewItemRuntime));
        }
        public TreeViewToggleRuntime ToggleButtonInstance { get; protected set; }
        public ListBoxItemRuntime ListBoxItemInstance { get; protected set; }
        public ContainerRuntime InnerPanelInstance { get; protected set; }

        public TreeViewItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 0f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
             
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

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
            ToggleButtonInstance = new TreeViewToggleRuntime();
            ToggleButtonInstance.Name = "ToggleButtonInstance";
            ListBoxItemInstance = new ListBoxItemRuntime();
            ListBoxItemInstance.Name = "ListBoxItemInstance";
            InnerPanelInstance = new ContainerRuntime();
            InnerPanelInstance.Name = "InnerPanelInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(ToggleButtonInstance);
            this.Children.Add(ListBoxItemInstance);
            this.Children.Add(InnerPanelInstance);
        }
        private void ApplyDefaultVariables()
        {

            this.ListBoxItemInstance.Height = 24f;
            this.ListBoxItemInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.ListBoxItemInstance.Width = -24f;
            this.ListBoxItemInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.ListBoxItemInstance.XUnits = GeneralUnitType.PixelsFromLarge;

            this.InnerPanelInstance.ChildrenLayout = global::Gum.Managers.ChildrenLayout.TopToBottomStack;
            this.InnerPanelInstance.Height = 0f;
            this.InnerPanelInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.InnerPanelInstance.Width = -24f;
            this.InnerPanelInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.InnerPanelInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.InnerPanelInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            this.InnerPanelInstance.Y = 24f;

        }
        partial void CustomInitialize();
    }
}
