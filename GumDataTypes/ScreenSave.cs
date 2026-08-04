#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
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

        // Gated because this file also compiles into GumDataTypes.csproj (net472) and is shared
        // with FlatRedBall via GumCoreShared.projitems, neither of which has trim attributes.
#if NET5_0_OR_GREATER
        [UnconditionalSuppressMessage("Trimming", "IL2026",
            Justification = "Clones this ScreenSave instance, which GumCommon's ILLink.Descriptors.xml preserves in full (preserve=\"all\").")]
#endif
        public ScreenSave Clone()
        {
            var cloned = FileManager.CloneSaveObject(this);
            return cloned;

        }
    }
}
