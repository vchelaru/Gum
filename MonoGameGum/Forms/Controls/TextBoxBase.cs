using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls
{
    public class TextCompositionEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The new text value.
        /// </summary>
        public string Text { get; }
        public TextCompositionEventArgs(string text) { Text = text; }
    }

    public class TextBoxBase : FrameworkElement //, IInputReceiver
    {
    }
}
