using Microsoft.Xna.Framework.Input;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms.Controls
{
    public enum ScrollIntoViewStyle
    {
        /// <summary>
        /// Scrolls only if the item is not in view. Scrolls the minimum amount necessary to bring the item into view.
        /// In other words, if the item is above the visible area, then the scrolling brings the item to the top.
        /// If the item is below the visible area, then the scrolling brings the item to the bottom.
        /// If the item is already into view, no scrolling is performed.
        /// </summary>
        BringIntoView,

        Top,
        Center,
        Bottom
    }

    public class ListBox : ItemsControl
    {

    }
}
