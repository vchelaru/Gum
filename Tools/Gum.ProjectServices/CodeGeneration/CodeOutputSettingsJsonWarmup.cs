using Newtonsoft.Json;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Runs Newtonsoft.Json's write and read paths for <see cref="CodeOutputProjectSettings"/> ahead of the
/// first project load. The Code Output plugin reads ProjectCodeSettings.codsj inside the load, and the
/// first use of the serializer otherwise costs a couple hundred milliseconds of setup (JIT, contract
/// resolution) on the UI thread while the tree waits. Safe on any thread.
/// </summary>
public class CodeOutputSettingsJsonWarmup
{
    /// <summary>Writes <paramref name="settings"/> to JSON and reads it back; returns the copy read back.</summary>
    public CodeOutputProjectSettings WarmUp(CodeOutputProjectSettings settings) =>
        JsonConvert.DeserializeObject<CodeOutputProjectSettings>(JsonConvert.SerializeObject(settings))!;
}
