using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using GumKeys = Gum.Forms.Input.Keys;

namespace Gum.Input;

/// <summary>
/// A <see cref="PolledKeyboard"/> whose key map is a table from <see cref="GumKeys"/> to the host
/// engine's own key type. A host supplies the table to the constructor and implements one device
/// query, <see cref="IsDeviceKeyPressed"/>. The table's keys are the keys the keyboard polls; any
/// <see cref="GumKeys"/> value left out of it reads as not pressed.
/// </summary>
/// <typeparam name="TDeviceKey">The host engine's key type.</typeparam>
public abstract class TablePolledKeyboard<TDeviceKey> : PolledKeyboard
{
    private readonly IReadOnlyDictionary<GumKeys, TDeviceKey> _gumToDevice;

    /// <summary>
    /// Creates a keyboard that polls the keys in <paramref name="gumToDevice"/>.
    /// </summary>
    protected TablePolledKeyboard(IReadOnlyDictionary<GumKeys, TDeviceKey> gumToDevice)
    {
        _gumToDevice = gumToDevice;
    }

    /// <inheritdoc/>
    protected sealed override IEnumerable<GumKeys> SupportedKeys => _gumToDevice.Keys;

    /// <inheritdoc/>
    protected sealed override bool IsDeviceKeyDown(GumKeys key) =>
        _gumToDevice.TryGetValue(key, out TDeviceKey? deviceKey) && IsDeviceKeyPressed(deviceKey);

    /// <summary>
    /// Returns whether the host's device reports <paramref name="deviceKey"/> as held.
    /// </summary>
    protected abstract bool IsDeviceKeyPressed(TDeviceKey deviceKey);

    /// <summary>
    /// Looks up the host key for <paramref name="key"/>. Returns false for a key left out of the table.
    /// For a host that answers push and release from the device too and overrides those queries.
    /// </summary>
    protected bool TryGetDeviceKey(GumKeys key, [MaybeNullWhen(false)] out TDeviceKey deviceKey) =>
        _gumToDevice.TryGetValue(key, out deviceKey);
}
