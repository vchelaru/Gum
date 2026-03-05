using RenderingLibrary.Graphics;
using System;

#if FRB
using BindableGue = Gum.Wireframe.GraphicalUiElement;
#endif

namespace Gum.Wireframe;

#if !FRB
/// <summary>
/// Deprecated. Use <see cref="GraphicalUiElement"/> directly — binding support has been moved to the base class.
/// </summary>
[Obsolete("BindableGue is deprecated. Use GraphicalUiElement instead.")]
public class BindableGue : GraphicalUiElement
{
    public BindableGue() { }

    public BindableGue(IRenderable renderable) : base(renderable) { }

    /// <summary>
    /// Deprecated. Use <see cref="GraphicalUiElement.ConvertValue"/> instead.
    /// </summary>
    [Obsolete("Use GraphicalUiElement.ConvertValue instead.")]
    public static new object ConvertValue(object value, Type desiredType, string format)
        => GraphicalUiElement.ConvertValue(value, desiredType, format);
}
#endif
