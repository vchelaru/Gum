using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace WpfDataUi.DataTypes;

/// <summary>
/// An <see cref="InstanceMember"/> that presents several underlying "channel" members as a single
/// composite value (for example Red/Green/Blue channels shown as one color). Reading composes the
/// channel values into the composite; writing decomposes the composite and forwards one value to each
/// channel. This is the domain-agnostic sibling of <see cref="MultiSelectInstanceMember"/>: it knows
/// nothing about what the composite represents, only how to compose/decompose via the supplied delegates.
/// </summary>
public class CompositeInstanceMember : InstanceMember
{
    private readonly Func<IReadOnlyList<object?>, object> _compose;
    private readonly Func<object, object?[]> _decompose;
    private readonly Type _compositeType;

    /// <summary>
    /// Raised before the channel members are written during a set. Allows a consumer to prepare for the
    /// multi-channel write, such as requesting a single undo lock so all channel writes coalesce.
    /// </summary>
    public event Action<SetPropertyArgs>? BeforeComposite;

    /// <summary>
    /// Raised after all channel members have been written during a set. Allows a consumer to clean up,
    /// such as disposing the undo lock taken in <see cref="BeforeComposite"/>.
    /// </summary>
    public event Action<SetPropertyArgs>? AfterComposite;

    /// <summary>
    /// The underlying members composed into this single row, in the order expected by the compose and
    /// decompose delegates.
    /// </summary>
    public IReadOnlyList<InstanceMember> ChannelMembers { get; }

    /// <inheritdoc/>
    public override bool IsDefault
    {
        // The getter is read by the grid to show default vs. set state. The setter forwards to each channel
        // (mirroring MultiSelectInstanceMember). It is currently only reachable if SupportsMakeDefault is
        // re-enabled on the composite — the consumer (CompositeMemberLogic) sets SupportsMakeDefault = false
        // and drives "Make Default" through each channel's IsDefault directly. Do NOT delete this override:
        // without it the base InstanceMember.IsDefault setter runs, which wipes the value to its type default
        // on ANY assignment (true or false) rather than deferring to the channels.
        get => ChannelMembers.All(channel => channel.IsDefault);
        set
        {
            foreach (InstanceMember channel in ChannelMembers)
            {
                channel.IsDefault = value;
            }
        }
    }

    /// <inheritdoc/>
    public override bool IsReadOnly
    {
        get => ChannelMembers.Any(channel => channel.IsReadOnly);
        set => base.IsReadOnly = value;
    }

    /// <summary>
    /// Creates a composite member over the supplied channel members.
    /// </summary>
    /// <param name="name">The composite member's name.</param>
    /// <param name="channelMembers">The underlying members, in compose/decompose order.</param>
    /// <param name="compositeType">The type of the composed value (used to select the displayer).</param>
    /// <param name="compose">Builds the composite value from the channel values.</param>
    /// <param name="decompose">Splits the composite value back into one value per channel.</param>
    public CompositeInstanceMember(
        string name,
        IReadOnlyList<InstanceMember> channelMembers,
        Type compositeType,
        Func<IReadOnlyList<object?>, object> compose,
        Func<object, object?[]> decompose) : base(name, null)
    {
        ChannelMembers = channelMembers;
        _compositeType = compositeType;
        _compose = compose;
        _decompose = decompose;

        CustomGetEvent += HandleCustomGet;
        CustomGetTypeEvent += HandleCustomGetType;
        CustomSetPropertyEvent += HandleCustomSet;

        foreach (InstanceMember channel in ChannelMembers)
        {
            channel.PropertyChanged += HandleChannelPropertyChanged;
        }
    }

    private void HandleChannelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        SimulateValueChanged();
    }

    private object HandleCustomGet(object owner)
    {
        List<object?> channelValues = ChannelMembers.Select(channel => channel.Value).ToList();
        return _compose(channelValues);
    }

    private Type HandleCustomGetType(object owner)
    {
        return _compositeType;
    }

    private void HandleCustomSet(object owner, SetPropertyArgs args)
    {
        // BeforeComposite typically takes a single undo lock (see CompositeMemberLogic) that AfterComposite
        // disposes. The channel writes must run inside try/finally so AfterComposite ALWAYS fires even if a
        // channel's SetValue throws — otherwise the undo lock would leak and silently suppress all further
        // undo recording for the rest of the session.
        BeforeComposite?.Invoke(args);

        try
        {
            if (args.Value != null)
            {
                object?[] decomposed = _decompose(args.Value);
                if (decomposed.Length != ChannelMembers.Count)
                {
                    throw new InvalidOperationException(
                        $"Decompose returned {decomposed.Length} value(s) but there are {ChannelMembers.Count} " +
                        "channel(s); a composite descriptor's Decompose must return exactly one value per channel.");
                }
                for (int i = 0; i < ChannelMembers.Count; i++)
                {
                    // Skip channels whose decomposed value already matches the channel's current
                    // value. Some composites (e.g. corner radius) have channels that carry an
                    // inherit-vs-explicit distinction via nullability; force-writing an unchanged
                    // channel on every commit would silently flip it from inherited to explicit.
                    if (!Equals(ChannelMembers[i].Value, decomposed[i]))
                    {
                        ChannelMembers[i].SetValue(decomposed[i], args.CommitType);
                    }
                }
            }
        }
        finally
        {
            AfterComposite?.Invoke(args);
        }
    }
}
