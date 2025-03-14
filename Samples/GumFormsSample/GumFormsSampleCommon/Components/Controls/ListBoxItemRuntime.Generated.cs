//Code for Controls/ListBoxItem (Container)
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
    public partial class ListBoxItemRuntime:ContainerRuntime
    {
        [System.Runtime.CompilerServices.ModuleInitializer]
        public static void RegisterRuntimeType()
        {
            GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType("Controls/ListBoxItem", typeof(ListBoxItemRuntime));
            MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof(MonoGameGum.Forms.Controls.ListBoxItem)] = typeof(ListBoxItemRuntime);
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
        public TextRuntime TextInstance { get; protected set; }
        public NineSliceRuntime FocusedIndicator { get; protected set; }

        public string ListItemDisplayText
        {
            get => TextInstance.Text;
            set => TextInstance.Text = value;
        }

        public ListBoxItemRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true)
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
            TextInstance = new TextRuntime();
            TextInstance.Name = "TextInstance";
            FocusedIndicator = new NineSliceRuntime();
            FocusedIndicator.Name = "FocusedIndicator";
        }
        protected virtual void AssignParents()
        {
            this.Children.Add(Background);
            this.Children.Add(TextInstance);
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

TextInstance.SetProperty("ColorCategoryState", "White");
TextInstance.SetProperty("StyleCategoryState", "Normal");
            this.TextInstance.Height = 0f;
            this.TextInstance.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            this.TextInstance.HorizontalAlignment = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            this.TextInstance.Text = @"ListBox Item";
            this.TextInstance.VerticalAlignment = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.Width = -8f;
            this.TextInstance.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            this.TextInstance.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Center;
            this.TextInstance.XUnits = GeneralUnitType.PixelsFromMiddle;
            this.TextInstance.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Center;
            this.TextInstance.YUnits = GeneralUnitType.PixelsFromMiddle;

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
