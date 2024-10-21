using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms
{
    public class FormsUtilities
    {
        static Cursor cursor;

        public static Cursor Cursor => cursor;

        static MonoGameGum.Input.Keyboard keyboard;

        public static Keyboard Keyboard => keyboard;

        /// <summary>
        /// Initializes defaults to enable FlatRedBall Forms. This method should be called before using Forms.
        /// </summary>
        /// <remarks>
        /// Projects can make further customization to Forms such as by modifying the FrameworkElement.Root or the DefaultFormsComponents.
        /// </remarks>
        public static void InitializeDefaults()
        {
            FrameworkElement.DefaultFormsComponents[typeof(Button)] = typeof(DefaultButtonRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(CheckBox)] = typeof(DefaultCheckboxRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(ComboBox)] = typeof(DefaultComboBoxRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(ListBox)] = typeof(DefaultListBoxRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(ListBoxItem)] = typeof(DefaultListBoxItemRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(ScrollBar)] = typeof(DefaultScrollBarRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(ScrollViewer)] = typeof(DefaultScrollViewerRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(TextBox)] = typeof(DefaultTextBoxRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(PasswordBox)] = typeof(DefaultTextBoxRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(Slider)] = typeof(DefaultSliderRuntime);

            cursor = new Cursor();
            keyboard = new MonoGameGum.Input.Keyboard();

            FrameworkElement.MainCursor = cursor;

            var root = new ContainerRuntime();

            root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;

            if (SystemManagers.Default == null)
            {
                throw new InvalidOperationException("You must call this method after initializing SystemManagers.Default");
            }

            root.AddToManagers();
            FrameworkElement.PopupRoot = root;
        }

        static List<GraphicalUiElement> innerList = new List<GraphicalUiElement>();

        public static void Update(GameTime gameTime, GraphicalUiElement rootGue)
        {
            cursor.Activity(gameTime.TotalGameTime.TotalSeconds);
            keyboard.Activity(gameTime.TotalGameTime.TotalSeconds);

            innerList.Clear();
            innerList.Add(rootGue);
            if(rootGue != FrameworkElement.PopupRoot && FrameworkElement.PopupRoot != null)
            {
                // make sure this is the last:
                foreach(var layer in SystemManagers.Default.Renderer.Layers)
                {
                    if(layer.Renderables.Contains(FrameworkElement.PopupRoot.RenderableComponent) && layer.Renderables.Last() != FrameworkElement.PopupRoot.RenderableComponent)
                    {
                        layer.Remove(FrameworkElement.PopupRoot.RenderableComponent as IRenderableIpso);
                        layer.Add(FrameworkElement.PopupRoot.RenderableComponent as IRenderableIpso);
                    }
                }

                foreach (var item in FrameworkElement.PopupRoot.Children)
                {
                    if(item is GraphicalUiElement itemAsGue)
                    {
                        innerList.Add(itemAsGue);
                    }
                }
            }

            //FrameworkElement.Root.DoUiActivityRecursively(cursor, keyboard, gameTime.TotalGameTime.TotalSeconds);
            GueInteractiveExtensionMethods.DoUiActivityRecursively(innerList, cursor, keyboard, gameTime.TotalGameTime.TotalSeconds);
        }
    }
}
