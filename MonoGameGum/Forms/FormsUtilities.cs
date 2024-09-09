using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.Input;
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

        public Keyboard Keyboard => keyboard;

        public static void InitializeDefaults()
        {
            FrameworkElement.DefaultFormsComponents[typeof(Button)] = typeof(DefaultButtonRuntime);
            FrameworkElement.DefaultFormsComponents[typeof(CheckBox)] = typeof(DefaultCheckboxRuntime);
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

        }

        public static void Update(GameTime gameTime, GraphicalUiElement rootElement)
        {
            cursor.Activity(gameTime.TotalGameTime.TotalSeconds);
            keyboard.Activity(gameTime.TotalGameTime.TotalSeconds);

            rootElement.DoUiActivityRecursively(cursor, keyboard, gameTime.TotalGameTime.TotalSeconds);

        }
    }
}
