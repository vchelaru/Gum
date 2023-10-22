using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public interface ITexturable
    {
        System.Drawing.Rectangle? SourceRectangle { get; set; }
        bool Wrap { get; set; }
    }
}
