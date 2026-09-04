using System;
using System.Collections.Generic;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes;
using Gum.DataTypes.Serialization.Json;
using ToolsUtilities;

namespace Gum.StateAnimation.SaveClasses;

public class ElementAnimationsSave
{
    /// <summary>XML animation sidecar extension, as in <c>MyComponentAnimations.ganx</c>.</summary>
    public const string FileExtension = "ganx";

    /// <summary>JSON counterpart of <see cref="FileExtension"/>.</summary>
    public const string JsonFileExtension = "ganj";

    /// <summary>
    /// The <c>Animations.ganx</c> / <c>Animations.ganj</c> suffix appended to an element's file name
    /// (minus its own extension) to reach that element's animation sidecar.
    /// </summary>
    public static string GetFileNameSuffix(bool isJsonFormat) =>
        "Animations." + (isJsonFormat ? JsonFileExtension : FileExtension);

    public List<AnimationSave> Animations
    {
        get;
        set;
    } = new List<AnimationSave>();

    public string ElementName
    {
        get; set;
    }

    public ElementAnimationsSave()
    {
        Animations = new List<AnimationSave>();
    }

    /// <summary>
    /// Writes this to <paramref name="fileName"/> as XML or JSON. As with
    /// <see cref="ElementSave.Save"/> there is no content-sniffing: the target file's own extension
    /// picks the serializer, so a <c>.ganj</c> path can never receive XML.
    /// </summary>
    // Gated because this file also compiles into GumDataTypesNet6.csproj (netstandard2.0),
    // which has no trim attributes.
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Serializes ElementAnimationsSave, which GumCommon's ILLink.Descriptors.xml preserves in full (Gum.StateAnimation.SaveClasses.*, preserve=\"all\").")]
#endif
    public void Save(string fileName)
    {
        if (IsJsonFormat(fileName))
        {
            GumJsonFileSerializer.WriteToFile(fileName,
                GumAnimationJsonFileSerializer.SerializeElementAnimations(this));
        }
        else
        {
            FileManager.XmlSerialize(this, fileName);
        }
    }

    /// <summary>
    /// Reads an <see cref="ElementAnimationsSave"/> from <paramref name="fileName"/>, picking the
    /// deserializer off the file's own extension. Symmetric with <see cref="Save"/>.
    /// </summary>
#if NET5_0_OR_GREATER
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Deserializes ElementAnimationsSave, which GumCommon's ILLink.Descriptors.xml preserves in full (Gum.StateAnimation.SaveClasses.*, preserve=\"all\").")]
#endif
    public static ElementAnimationsSave Load(string fileName)
    {
        return IsJsonFormat(fileName)
            ? GumAnimationJsonFileSerializer.DeserializeElementAnimations(File.ReadAllText(fileName))
            : FileManager.XmlDeserialize<ElementAnimationsSave>(fileName);
    }

    /// <summary>
    /// True when <paramref name="fileName"/> is a <c>.ganj</c>. The single source of truth for
    /// which serializer an animation file uses.
    /// </summary>
    public static bool IsJsonFormat(string fileName) =>
        string.Equals(FileManager.GetExtension(fileName), JsonFileExtension, StringComparison.OrdinalIgnoreCase);

    public override string ToString()
    {
        return $"{ElementName} ({Animations.Count} animations)";
    }
}
