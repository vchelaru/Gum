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

            if (SystemManagers.Default == null)
            {
                throw new InvalidOperationException("You must call this method after initializing SystemManagers.Default");
            }

            FrameworkElement.PopupRoot = CreateFullscreenContainer();
            FrameworkElement.ModalRoot = CreateFullscreenContainer();
        }

        static ContainerRuntime CreateFullscreenContainer()
        {
            var container = new ContainerRuntime();

            container.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            container.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            container.Width = GraphicalUiElement.CanvasWidth;
            container.Height = GraphicalUiElement.CanvasHeight;

            container.AddToManagers();

            return container;
        }

        static List<GraphicalUiElement> innerList = new List<GraphicalUiElement>();

        public static void Update(GameTime gameTime, GraphicalUiElement rootGue)
        {
            cursor.Activity(gameTime.TotalGameTime.TotalSeconds);
            keyboard.Activity(gameTime.TotalGameTime.TotalSeconds);
            innerList.Clear();

            if (FrameworkElement.ModalRoot.Children.Count > 0)
            {
                SetDimensionsToCanvas(FrameworkElement.ModalRoot);

                // make sure this is the last:
                foreach (var layer in SystemManagers.Default.Renderer.Layers)
                {
                    if (layer.Renderables.Contains(FrameworkElement.ModalRoot.RenderableComponent) && layer.Renderables.Last() != FrameworkElement.ModalRoot.RenderableComponent)
                    {
                        layer.Remove(FrameworkElement.ModalRoot.RenderableComponent as IRenderableIpso);
                        layer.Add(FrameworkElement.ModalRoot.RenderableComponent as IRenderableIpso);
                    }
                }

                foreach (var item in FrameworkElement.ModalRoot.Children)
                {
                    if (item is GraphicalUiElement itemAsGue)
                    {
                        innerList.Add(itemAsGue);
                    }
                }
            }
            else
            {
                innerList.Add(rootGue);
                if (rootGue != FrameworkElement.PopupRoot && FrameworkElement.PopupRoot != null && FrameworkElement.PopupRoot.Children.Count > 0)
                {
                    SetDimensionsToCanvas(FrameworkElement.PopupRoot);
                    // make sure this is the last:
                    foreach (var layer in SystemManagers.Default.Renderer.Layers)
                    {
                        if (layer.Renderables.Contains(FrameworkElement.PopupRoot.RenderableComponent) && layer.Renderables.Last() != FrameworkElement.PopupRoot.RenderableComponent)
                        {
                            layer.Remove(FrameworkElement.PopupRoot.RenderableComponent as IRenderableIpso);
                            layer.Add(FrameworkElement.PopupRoot.RenderableComponent as IRenderableIpso);
                        }
                    }

                    foreach (var item in FrameworkElement.PopupRoot.Children)
                    {
                        if (item is GraphicalUiElement itemAsGue)
                        {
                            innerList.Add(itemAsGue);
                        }
                    }
                }
            }


            //FrameworkElement.Root.DoUiActivityRecursively(cursor, keyboard, gameTime.TotalGameTime.TotalSeconds);
            GueInteractiveExtensionMethods.DoUiActivityRecursively(innerList, cursor, keyboard, gameTime.TotalGameTime.TotalSeconds);
        }

        static void SetDimensionsToCanvas(InteractiveGue container)
        {

            container.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            container.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            container.Width = GraphicalUiElement.CanvasWidth;
            container.Height = GraphicalUiElement.CanvasHeight;
        }
    }
}
