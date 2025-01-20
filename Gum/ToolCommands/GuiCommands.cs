namespace Gum.ToolCommands
{
    public class GuiCommands_Old
    {
        Wireframe.WireframeControl mWireframeControl;

        static GuiCommands_Old mSelf = new GuiCommands_Old();


        public static GuiCommands_Old Self
        {
            get { return mSelf; }
            set { mSelf = value; }
        }

        public void Initialize(Wireframe.WireframeControl wireframeControl)
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
