using ToolsUtilities;

namespace Gum.Managers
{
    public class FileLocations
    {
        static FileLocations mSelf;

        public static FileLocations Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new FileLocations();
                }
                return mSelf;
            }
        }

        public string ScreensFolder => ProjectFolder + "Screens/";

        public string ComponentsFolder => ProjectFolder + "Components/";

        public string StandardsFolder => ProjectFolder + "Standards/";

        public string BehaviorsFolder => ProjectFolder + "Behaviors/";

        public string ProjectFolder  => FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName);
    }
}
