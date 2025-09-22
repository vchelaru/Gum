using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenderingLibrary.Graphics
{
    public interface IText
    {
        void SetNeedsRefreshToTrue();

        void UpdatePreRenderDimensions();

        float DescenderHeight { get; }
        float FontScale { get; }

        float WrappedTextWidth { get; }
        float WrappedTextHeight { get; }

        // The text that was assgined on this Text instance prior to any wrapping.
        string? RawText { get; set; }

        float? Width { get; set; }

        TextOverflowVerticalMode TextOverflowVerticalMode { get; set; }
    }
}
