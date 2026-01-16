using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region Events

        // It's possible that a size change could result in a layout which 
        // results in a further size change. This recursive call of size changes
        // could happen indefinitely so we only want to do this one time.
        // This prevents the size change from happening over and over:
        bool isInSizeChange;
        /// <summary>
        /// Event raised whenever this instance's absolute size changes. This size change can occur by a direct value being
        /// set (such as Width or WidthUnits), or by an indirect value changing, such as if a Parent is resized and if
        /// this uses a WidthUnits depending on the parent.
        /// </summary>
        public event EventHandler SizeChanged;
        public event EventHandler PositionChanged;
        public event EventHandler VisibleChanged;
        public event EventHandler<ParentChangedEventArgs> ParentChanged;

        public class ParentChangedEventArgs
        {
            public IRenderableIpso? OldValue { get; set; }
            public IRenderableIpso? NewValue { get; set; }
        };

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
            {
                var args = new PropertyChangedEventArgs(propertyName);
                PropertyChanged(this, args);
            }
        }

        public static Action<IText, GraphicalUiElement>? UpdateFontFromProperties;
        public static Action<GraphicalUiElement>? ThrowExceptionsForMissingFiles;
        public static Action<IRenderableIpso, ISystemManagers>? RemoveRenderableFromManagers;
        public static Action<IRenderableIpso, ISystemManagers, Layer>? AddRenderableToManagers;
        public static Action<string, GraphicalUiElement>? ApplyMarkup;

        public static Action<IRenderableIpso, GraphicalUiElement, string, object> SetPropertyOnRenderable =
            // This is the default fallback to make Gum work. Specific rendering libraries can change this to provide 
            // better performance.
            SetPropertyThroughReflection;

        public static Func<IRenderable, IRenderable>? CloneRenderableFunction;


        #endregion
    }
}