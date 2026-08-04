using Gum.DataTypes.Behaviors;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using ToolsUtilities;

namespace Gum.DataTypes;

public class ComponentSave : ElementSave
{
    // should this be part of ElementSave? Not sure...
    //public string? DefaultChildContainer { get; set; }

    public override string FileExtension
    {
        get { return GumProjectSave.ComponentExtension; }
    }


    public override string Subfolder
    {
        get { return ElementReference.ComponentSubfolder; }
    }

    // Gated because this file also compiles into GumDataTypesNet6.csproj (netstandard2.0),
    // which has no trim attributes.
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Clones this ComponentSave instance, which GumCommon's ILLink.Descriptors.xml preserves in full (preserve=\"all\").")]
#endif
    public ComponentSave Clone()
    {
        var cloned = FileManager.CloneSaveObject(this);
        return cloned;

    }

}
