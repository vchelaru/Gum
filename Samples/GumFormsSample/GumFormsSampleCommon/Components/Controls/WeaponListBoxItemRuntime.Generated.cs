//Code for Controls/WeaponListBoxItem (Container)
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
    public partial class WeaponListBoxItemRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/WeaponListBoxItem", typeof(WeaponListBoxItemRuntime));
        }
        public MonoGameGum.Forms.Controls.ListBoxItem FormsControl => FormsControlAsObject as MonoGameGum.Forms.Controls.ListBoxItem;
        public enum ListBoxItemCategory
        {
            Enabled,
            Highlighted,
            Selected,
            Focused,
        }

        ListBoxItemCategory mListBoxItemCategoryState;
        public ListBoxItemCategory ListBoxItemCategoryState
        {
            get => mListBoxItemCategoryState;
            set
            {
                mListBoxItemCategoryState = value;
                var appliedDynamically = false;
                if(!appliedDynamically)
                {
                    switch (value)
                    {
                        case ListBoxItemCategory.Enabled:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.Background.Visible = false;
                            this.FocusedIndicator.Visible = false;
                            break;
                        case ListBoxItemCategory.Highlighted:
                            Background.SetProperty("ColorCategoryState", "PrimaryLight");
                            this.Background.Visible = true;
                            this.FocusedIndicator.Visible = false;
                            break;
                        case ListBoxItemCategory.Selected:
                            Background.SetProperty("ColorCategoryState", "Accent");
                            this.Background.Visible = true;
                            this.FocusedIndicator.Visible = false;
                            break;
                        case ListBoxItemCategory.Focused:
                            Background.SetProperty("ColorCategoryState", "DarkGray");
                            this.Background.Visible = false;
                            this.FocusedIndicator.Visible = true;
                            break;
                    }
                }
            }
        }
        public NineSliceRuntime Background { get; protected set; }
        public TextRuntime NameTextInstance { get; protected set; }
        public TextRuntime DamageTextInstance { get; protected set; }
        public TextRuntime DurabilityTextInstance { get; protected set; }
        public TextRuntime LevelTextInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public string ListItemDisplayText
        {
            get => NameTextInstance.Text;
            set => NameTextInstance.Text = value;
        }

        public WeaponListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        {
            if(fullInstantiation)
            {
            }

            this.Height = 72f;
            this.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
             
            this.Width = 0f;
            this.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            InitializeInstances();

            ApplyDefaultVariables();
            AssignParents();
            if(tryCreateFormsObject)
            {
                if (FormsControl == null)
                {
                    FormsControlAsObject = new MonoGameGum.Forms.Controls.ListBoxItem(this);
                }
            }
            CustomInitialize();
        }
        protected virtual void InitializeInstances()
        {
            Background = new NineSliceRuntime();
            Background.Name = "Background";
            NameTextInstance = new TextRuntime();
            NameTextInstance.Name = "NameTextInstance";
            DamageTextInstance = new TextRuntime();
            DamageTextInstance.Name = "DamageTextInstance";
            DurabilityTextInstance = new TextRuntime();
            DurabilityTextInstance.Name = "DurabilityTextInstance";
            LevelTextInstance = new TextRuntime();
            LevelTextInstance.Name = "LevelTextInstance";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(NameTextInstance);
            this.Children.Add(DamageTextInstance);
            this.Children.Add(DurabilityTextInstance);
            this.Children.Add(LevelTextInstance);
            this.Children.Add(FocusedIndicator);
        }
        private void ApplyDefaultVariables()
        {
Background.SetProperty("ColorCategoryState", "DarkGray");
Background.SetProperty("StyleCategoryState", "Solid");
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

NameTextInstance.SetProperty("ColorCategoryState", "White");
NameTextInstance.SetProperty("StyleCategoryState", "Normal");
            this.NameTextInstance.FontSize = 24;
            this.NameTextInstance.Height = 0f;
            this.NameTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.NameTextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.NameTextInstance.Text = @"<Weapon Name>";
            this.NameTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.NameTextInstance.Width = -8f;
            this.NameTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.NameTextInstance.X = 0f;
            this.NameTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.NameTextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.NameTextInstance.Y = 0f;
            this.NameTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.NameTextInstance.YUnits = GeneralUnitType.PixelsFromSmall;

DamageTextInstance.SetProperty("ColorCategoryState", "White");
DamageTextInstance.SetProperty("StyleCategoryState", "Normal");
            this.DamageTextInstance.FontSize = 14;
            this.DamageTextInstance.Height = 0f;
            this.DamageTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.DamageTextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.DamageTextInstance.Text = @"<Weapon Damage>";
            this.DamageTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.DamageTextInstance.Width = -8f;
            this.DamageTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DamageTextInstance.X = 0f;
            this.DamageTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.DamageTextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.DamageTextInstance.Y = 27f;
            this.DamageTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.DamageTextInstance.YUnits = GeneralUnitType.PixelsFromSmall;

DurabilityTextInstance.SetProperty("ColorCategoryState", "White");
DurabilityTextInstance.SetProperty("StyleCategoryState", "Normal");
            this.DurabilityTextInstance.FontSize = 14;
            this.DurabilityTextInstance.Height = 0f;
            this.DurabilityTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.DurabilityTextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.DurabilityTextInstance.Text = @"<Weapon Durability>";
            this.DurabilityTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.DurabilityTextInstance.Width = -8f;
            this.DurabilityTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.DurabilityTextInstance.X = 0f;
            this.DurabilityTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.DurabilityTextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
            this.DurabilityTextInstance.Y = 46f;
            this.DurabilityTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.DurabilityTextInstance.YUnits = GeneralUnitType.PixelsFromSmall;

LevelTextInstance.SetProperty("ColorCategoryState", "White");
LevelTextInstance.SetProperty("StyleCategoryState", "Normal");
            this.LevelTextInstance.FontSize = 14;
            this.LevelTextInstance.Height = 0f;
            this.LevelTextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.LevelTextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.LevelTextInstance.Text = @"<Weapon Durability>";
            this.LevelTextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.LevelTextInstance.Width = 0f;
            this.LevelTextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.LevelTextInstance.X = 0f;
            this.LevelTextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Right;
            this.LevelTextInstance.XUnits = GeneralUnitType.PixelsFromLarge;
            this.LevelTextInstance.Y = 0f;
            this.LevelTextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.LevelTextInstance.YUnits = GeneralUnitType.PixelsFromSmall;

FocusedIndicator.SetProperty("ColorCategoryState", "Warning");
FocusedIndicator.SetProperty("StyleCategoryState", "Solid");
            this.FocusedIndicator.Height = 2f;
            this.FocusedIndicator.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            this.FocusedIndicator.Visible = false;
            this.FocusedIndicator.Y = -2f;
            this.FocusedIndicator.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            this.FocusedIndicator.YUnits = GeneralUnitType.PixelsFromLarge;

        }
        partial void CustomInitialize();
    }
}
