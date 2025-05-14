using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.GueDeriving
{
    public class RectangleRuntime : BindableGue
    {
        RenderingLibrary.Math.Geometry.LineRectangle containedLineRectangle;
        RenderingLibrary.Math.Geometry.LineRectangle ContainedLineRectangle
        {
            get
            {
                if (containedLineRectangle == null)
                {
                    containedLineRectangle = this.RenderableComponent as RenderingLibrary.Math.Geometry.LineRectangle;
                }
                return containedLineRectangle;
            }
        }

        public float LineWidth
        {
            get => ContainedLineRectangle.LinePixelWidth;
            set
            {
                ContainedLineRectangle.LinePixelWidth = value;
            }
        }

        public bool IsDotted
        {
            get => ContainedLineRectangle.IsDotted;
            set
            {
                ContainedLineRectangle.IsDotted = value;
                NotifyPropertyChanged();
            }
        }

        public int Alpha
        {
            get
            {
                return ContainedLineRectangle.Color.A;
            }
            set
            {
                // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
                // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
                var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithAlpha(ContainedLineRectangle.Color, (byte)value);
                ContainedLineRectangle.Color = color;
            }
        }
        public int Blue
        {
            get
            {
                return ContainedLineRectangle.Color.B;
            }
            set
            {
                // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
                // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
                var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithBlue(ContainedLineRectangle.Color, (byte)value);
                ContainedLineRectangle.Color = color;
            }
        }
        public int Green
        {
            get
            {
                return ContainedLineRectangle.Color.G;
            }
            set
            {
                // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
                // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
                var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithGreen(ContainedLineRectangle.Color, (byte)value);
                ContainedLineRectangle.Color = color;
            }
        }

        public int Red
        {
            get
            {
                return ContainedLineRectangle.Color.R;
            }
            set
            {
                // The new version of Glue is moving away from XNA color values. This code converts color values. If this doesn't run, you need to upgrade your GLUX version.
                // More info here: https://flatredball.com/documentation/tools/glue-reference/glujglux/
                var color = ToolsUtilitiesStandard.Helpers.ColorExtensions.WithRed(ContainedLineRectangle.Color, (byte)value);
                ContainedLineRectangle.Color = color;
            }
        }

        public Microsoft.Xna.Framework.Color Color
        {
            get
            {
                return RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedLineRectangle.Color);
            }
            set
            {
                ContainedLineRectangle.Color = RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value);
                NotifyPropertyChanged();
            }
        }

        public void AddToManagers() => base.AddToManagers(SystemManagers.Default, layer: null);

        public RectangleRuntime(bool fullInstantiation = true, SystemManagers systemManagers = null)
        {
            if (fullInstantiation)
            {
                var rectangle = new RenderingLibrary.Math.Geometry.LineRectangle(systemManagers);
                SetContainedObject(rectangle);
                containedLineRectangle = rectangle;

                rectangle.Color = System.Drawing.Color.White;
                Width = 50;
                Height = 50;
            }
        }
    }
}
