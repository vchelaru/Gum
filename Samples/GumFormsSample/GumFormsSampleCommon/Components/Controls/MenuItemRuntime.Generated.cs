//Code for Controls/MenuItem (Container)
using GumRuntime;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
namespace GumFormsSample.Components
{
    public partial class MenuItemRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/MenuItem", typeof(MenuItemRuntime));
        }
        public MonoGameGum.Forms.Controls.MenuItem FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.MenuItem;
        public enum MenuItemCategory
        {
            Enabled,
            Highlighted,
            Selected,
            Focused,
        }

        MenuItemCategory mMenuItemCategoryState;
        public MenuItemCategory MenuItemCategoryState
        {
            get => mMenuItemCategoryState;
            set
            {
                mMenuItemCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case MenuItemCategory.Enabled:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            break;
                        case MenuItemCategory.Highlighted:
                            Background.SetProperty("ColorCategoryState", "LightGray");
                            break;
                        case MenuItemCategory.Selected:
                            Background.SetProperty("ColorCategoryState", "Primary");
                            break;
                        case MenuItemCategory.Focused:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime TextInstance { get; protected set; }

        public string Header
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public MenuItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 0f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
             
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.X = 0f;
            this.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.XUnits = GeneralUnitType.PixelsFromSmall;
            this.Y = 0f;
            this.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.YUnits = GeneralUnitType.PixelsFromSmall;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
                if (FormsControl == null)
                {
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.MenuItem(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(TextInstance);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "DarkGray");
            this.Background.Height = 0f;
            this.Background.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.Width = 0f;
            this.Background.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.Background.X = 0f;
            this.Background.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.Background.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.Background.Y = 0f;
            this.Background.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.Background.YUnits = GeneralUnitType.PixelsFromMiddle;

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Normal");
            this.TextInstance.Height = 0f;
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance.Text = @"Menu Item";
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.Width = 2f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance.X = 2f;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

        }
        partial void CustomInitialize();
    }
}
