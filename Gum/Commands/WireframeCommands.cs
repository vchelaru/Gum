using Gum.Wireframe;

namespace Gum.Commands
{
    public class WireframeCommands
    {
        public void Refresh(bool forceLayout = true, bool forceReloadContent = false)
        {
            WireframeObjectManager.Self.RefreshAll(forceLayout, forceReloadContent);
        }

        public void RefreshGuides()
        {
            WireframeObjectManager.Self.RefreshGuides();
        }

        public bool AreRulersVisible
        {
            get => WireframeObjectManager.Self.WireframeControl.RulersVisible;
            set => WireframeObjectManager.Self.WireframeControl.RulersVisible = value;
        }

        public bool AreCanvasBoundsVisible
        {
            get => WireframeObjectManager.Self.WireframeControl.CanvasBoundsVisible;
            set => WireframeObjectManager.Self.WireframeControl.CanvasBoundsVisible = value;
        }

        public bool IsBackgroundGridVisible
        {
            get => WireframeObjectManager.Self.BackgroundSprite.Visible;
            set => WireframeObjectManager.Self.BackgroundSprite.Visible = value;
        }

        public bool AreHighlightsVisible
        {
            get => SelectionManager.Self.AreHighlightsVisible;
            set => SelectionManager.Self.AreHighlightsVisible = value;
        }
    }
}
