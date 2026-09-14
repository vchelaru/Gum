using System;
using System.Collections;
using System.Collections.Generic;
using WpfDataUi.DataTypes;

namespace WpfDataUi;

/// <summary>
/// Chooses the editor for a row. <see cref="SelectDisplayerKey"/> is the shared decision (preferred
/// displayer, then custom options, then the member's type, then text); each head registers which of
/// its controls stands for each neutral key and resolves through <see cref="SelectControlType"/>.
/// </summary>
public class DisplayerRegistry
{
    private readonly Dictionary<Type, Type> _controlTypesByKey;
    private readonly List<KeyValuePair<Func<Type, bool>, Type>> _typeAssociations;

    /// <summary>Creates a registry with no controls registered and the standard type rules.</summary>
    public DisplayerRegistry()
    {
        _controlTypesByKey = new Dictionary<Type, Type>();
        _typeAssociations = new List<KeyValuePair<Func<Type, bool>, Type>>
        {
            new(type => type == typeof(bool), typeof(StandardDisplayers.CheckBox)),
            new(type => type == typeof(bool?), typeof(StandardDisplayers.NullableBool)),
            new(type => type != null && type.IsEnum, typeof(StandardDisplayers.ComboBox)),
            // Nullable enums render as the same drop-down; the combo box adds a "no value" entry.
            new(type => type != null && Nullable.GetUnderlyingType(type)?.IsEnum == true, typeof(StandardDisplayers.ComboBox)),
            new(type => type != null && typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string), typeof(StandardDisplayers.ListBox)),
        };
    }

    /// <summary>Registers the control a head uses for a neutral key (or any other key type).</summary>
    public void Register(Type displayerKey, Type controlType)
    {
        _controlTypesByKey[displayerKey] = controlType;
    }

    /// <summary>
    /// The control type for <paramref name="displayer"/>: its registered control when it is a key,
    /// otherwise the type itself (a concrete control assigned directly).
    /// </summary>
    public Type ResolveControlType(Type displayer)
    {
        return _controlTypesByKey.TryGetValue(displayer, out Type? controlType) ? controlType : displayer;
    }

    /// <summary>Whether a control is registered for <paramref name="displayerKey"/>.</summary>
    public bool IsRegistered(Type displayerKey) => _controlTypesByKey.ContainsKey(displayerKey);

    /// <summary>
    /// The displayer <paramref name="member"/> should use, as a key or a control type: its
    /// <see cref="InstanceMember.PreferredDisplayer"/>; otherwise a combo box when it has custom
    /// options; otherwise the editor for its property type; otherwise a text box.
    /// </summary>
    public Type SelectDisplayerKey(InstanceMember member)
    {
        if (member.PreferredDisplayer != null)
        {
            return member.PreferredDisplayer;
        }

        // Custom options take precedence over the type, so a converter-reduced enum shows only its options.
        if (member.CustomOptions != null && member.CustomOptions.Count != 0)
        {
            return typeof(StandardDisplayers.ComboBox);
        }

        Type? selected = null;
        Type propertyType = member.PropertyType;
        foreach (KeyValuePair<Func<Type, bool>, Type> association in _typeAssociations)
        {
            // Every matching rule overrides the previous one, so the last match wins.
            if (association.Key(propertyType))
            {
                selected = association.Value;
            }
        }

        return selected ?? typeof(StandardDisplayers.TextBox);
    }

    /// <summary>The control type to create for <paramref name="member"/> in this head.</summary>
    public Type SelectControlType(InstanceMember member) => ResolveControlType(SelectDisplayerKey(member));
}
