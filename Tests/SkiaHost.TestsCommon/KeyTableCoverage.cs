using Shouldly;
using GumKeys = Gum.Forms.Input.Keys;

namespace SkiaHost.TestsCommon;

/// <summary>
/// Guards a host's Gum-key-to-engine-key table against silently missing keys. Every named
/// <see cref="GumKeys"/> value must be mapped or listed in the host's explicit
/// "intentionally unmapped" set, so a new host cannot skip a key without saying so.
/// </summary>
public static class KeyTableCoverage
{
    /// <summary>
    /// The keys a desktop game keyboard table normally leaves out: <c>None</c>, IME keys, the
    /// Select/Print/Execute/Help/Sleep/Separator legacy keys, F13-F24, browser, volume, media and launch
    /// keys, ChatPad keys, and the rare OEM and Attn/Crsel/Exsel/Pa1/Zoom keys. A host starts from this
    /// list and removes whatever its engine does expose.
    /// </summary>
    public static IReadOnlyCollection<GumKeys> MediaImeAndLegacyKeys { get; } = new[]
    {
        GumKeys.None,
        GumKeys.Kana, GumKeys.Kanji, GumKeys.ImeConvert, GumKeys.ImeNoConvert, GumKeys.ProcessKey,
        GumKeys.Select, GumKeys.Print, GumKeys.Execute, GumKeys.Help, GumKeys.Sleep, GumKeys.Separator,
        GumKeys.F13, GumKeys.F14, GumKeys.F15, GumKeys.F16, GumKeys.F17, GumKeys.F18,
        GumKeys.F19, GumKeys.F20, GumKeys.F21, GumKeys.F22, GumKeys.F23, GumKeys.F24,
        GumKeys.BrowserBack, GumKeys.BrowserForward, GumKeys.BrowserRefresh, GumKeys.BrowserStop,
        GumKeys.BrowserSearch, GumKeys.BrowserFavorites, GumKeys.BrowserHome,
        GumKeys.VolumeMute, GumKeys.VolumeDown, GumKeys.VolumeUp,
        GumKeys.MediaNextTrack, GumKeys.MediaPreviousTrack, GumKeys.MediaStop, GumKeys.MediaPlayPause,
        GumKeys.LaunchMail, GumKeys.SelectMedia, GumKeys.LaunchApplication1, GumKeys.LaunchApplication2,
        GumKeys.ChatPadGreen, GumKeys.ChatPadOrange,
        GumKeys.Oem8, GumKeys.OemCopy, GumKeys.OemAuto, GumKeys.OemEnlW, GumKeys.OemClear,
        GumKeys.Attn, GumKeys.Crsel, GumKeys.Exsel, GumKeys.EraseEof, GumKeys.Play, GumKeys.Zoom, GumKeys.Pa1,
    };

    /// <summary>
    /// Asserts that every named <see cref="GumKeys"/> value is mapped in <paramref name="table"/> or
    /// listed in <paramref name="intentionallyUnmapped"/>, and that no key is in both. Failures name the keys.
    /// </summary>
    public static void AssertEveryKeyAccountedFor<TDeviceKey>(
        IReadOnlyDictionary<GumKeys, TDeviceKey> table,
        IEnumerable<GumKeys> intentionallyUnmapped)
    {
        HashSet<GumKeys> unmapped = new HashSet<GumKeys>(intentionallyUnmapped);

        List<string> missing = Enum.GetValues<GumKeys>()
            .Where(key => !table.ContainsKey(key) && !unmapped.Contains(key))
            .Select(key => key.ToString())
            .Distinct()
            .OrderBy(name => name)
            .ToList();
        missing.ShouldBeEmpty("Keys neither mapped nor listed as intentionally unmapped");

        List<string> contradictory = unmapped
            .Where(table.ContainsKey)
            .Select(key => key.ToString())
            .OrderBy(name => name)
            .ToList();
        contradictory.ShouldBeEmpty("Keys listed as intentionally unmapped but present in the table");
    }
}
