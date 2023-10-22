using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public interface ITextureCoordinate
    {
        Rectangle? SourceRectangle { get; set; }
        bool Wrap { get; set; }
    }
}
