using Gum.Plugins.InternalPlugins.EditorTab.Views;

namespace Gum.ToolCommands
{
    public class GuiCommands_Old
    {
        WireframeControl mWireframeControl;

        static GuiCommands_Old mSelf = new GuiCommands_Old();


        public static GuiCommands_Old Self
        {
            get { return mSelf; }
            set { mSelf = value; }
        }

        public void Initialize(WireframeControl wireframeControl)
        {
            mWireframeControl = wireframeControl;
        }

        public void UpdateWireframeToProject()
        {
            mWireframeControl.UpdateCanvasBoundsToProject();
        }

        public void RefreshWireframe()
        {
            mWireframeControl.RefreshDisplay();
        }
    }
}
