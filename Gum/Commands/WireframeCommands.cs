﻿using Gum.Plugins;
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

        bool isBackgroundGridVisible = true;
        public bool IsBackgroundGridVisible
        {
            get => isBackgroundGridVisible;
            set
            {
                isBackgroundGridVisible = value;
                PluginManager.Self.WireframePropertyChanged(nameof(IsBackgroundGridVisible));
            }
        }

        bool areHighlightsVisible;
        public bool AreHighlightsVisible
        {
            get => areHighlightsVisible;
            set
            {
                areHighlightsVisible = value;
                PluginManager.Self.WireframePropertyChanged(nameof(AreHighlightsVisible));
            }
        }
    }
}
