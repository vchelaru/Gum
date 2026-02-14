namespace Gum.Commands;

public interface IWireframeCommands
{
    void Refresh(bool forceLayout = true, bool forceReloadContent = false);
    bool AreRulersVisible { get; set; }
    bool AreCanvasBoundsVisible { get; set; }
    bool IsBackgroundGridVisible { get; set; }
    bool AreHighlightsVisible { get; set; }
}
