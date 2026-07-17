using System;
using Newtonsoft.Json;
using System.Xml.Serialization;
using ToolsUtilities;

namespace Gum.Settings;

/// <summary>
/// A single entry in <c>GeneralSettingsFile.RecentProjects</c>. Split out of
/// <c>GeneralSettingsFile.cs</c> (ADR-0005 Phase 3) so <see cref="Gum.Managers.IProjectManager"/> can
/// expose <c>RecentProjects</c> without leaking the whole WinForms-entangled GeneralSettingsFile —
/// this class itself has no such dependency, so it moved into the headless Gum.Presentation assembly.
/// </summary>
public class RecentProjectReference
{
    public DateTime LastTimeOpened;
    public string AbsoluteFileName = "";

    [XmlIgnore]
    [JsonIgnore]
    public FilePath FilePath => AbsoluteFileName;

    public bool IsFavorite;
}
