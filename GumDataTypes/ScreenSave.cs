using ToolsUtilities;

namespace Gum.DataTypes
{
    public class ScreenSave : ElementSave
    {
        public override string FileExtension
        {
            get { return GumProjectSave.ScreenExtension; }
        }

        public override string Subfolder
        {
            get { return ElementReference.ScreenSubfolder; }
        }

        public ScreenSave Clone()
        {
            var cloned = FileManager.CloneSaveObject(this);
            return cloned;

        }
    }
}
