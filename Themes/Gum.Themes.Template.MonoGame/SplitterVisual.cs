using Gum.GueDeriving;
using BaseSplitterVisual = Gum.Forms.DefaultVisuals.V3.SplitterVisual;

namespace Gum.Themes.Template;

/// <summary>
/// Template-styled Splitter visual. A thin Border-colored fill — same hairline
/// look as the rest of Template's container borders, so a splitter between two
/// surfaces reads as a continuation of the chrome rather than a new control.
/// <para>
/// V3.SplitterVisual has no state category and no hover/press feedback;
/// matching that here keeps the visual minimal. If we later want drag
/// feedback (VS Code-style brighten-on-hover), we'd have to wire mouse events
/// on the InteractiveGue directly since there's no built-in state plumbing.
/// </para>
/// </summary>
public class SplitterVisual : BaseSplitterVisual
{
    private readonly RectangleRuntime _fill;

    public SplitterVisual(bool fullInstantiation = true, bool tryCreateFormsObject = true)
        : base(fullInstantiation, tryCreateFormsObject)
    {
        Background.Parent = null;

        _fill = TemplateShapes.Fill(TemplateStyling.ActiveStyle.Colors.Border, cornerRadius: 0f, "SplitterFill");
        AddChild(_fill);
    }
}
