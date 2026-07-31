using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.StateInterpolation;
using Gum.StateAnimation.SaveClasses;

namespace Gum.DataTypes.Serialization.Json;

/// <summary>
/// JSON-serializable shapes of the <c>.ganx</c> animation save classes (<see cref="ElementAnimationsSave"/>
/// and its nested graph). Not part of the <c>GumCoreShared.projitems</c>-shared source — Gum's animation
/// system is not used by FlatRedBall (FRB1 generates its own runtimes), matching how the underlying
/// <c>Gum.StateAnimation.SaveClasses</c> types themselves are excluded from that shared surface.
/// </summary>
internal sealed class ElementAnimationsSaveJson
{
    public string ElementName { get; set; } = "";
    public List<AnimationSaveJson> Animations { get; set; } = new List<AnimationSaveJson>();
}

internal sealed class AnimationSaveJson
{
    public string Name { get; set; } = "";
    public bool Loops { get; set; }
    public List<AnimatedStateSaveJson> States { get; set; } = new List<AnimatedStateSaveJson>();
    public List<AnimationReferenceSaveJson> Animations { get; set; } = new List<AnimationReferenceSaveJson>();
    public List<NamedEventSaveJson> Events { get; set; } = new List<NamedEventSaveJson>();
}

internal sealed class AnimatedStateSaveJson
{
    public string StateName { get; set; } = "";
    public float Time { get; set; }
    public InterpolationType InterpolationType { get; set; }
    public Easing Easing { get; set; }
}

internal sealed class AnimationReferenceSaveJson
{
    public string Name { get; set; } = "";
    public float Time { get; set; }
}

internal sealed class NamedEventSaveJson
{
    public string? Name { get; set; }
    public float Time { get; set; }
}

internal static class ElementAnimationsSaveJsonMapper
{
    public static ElementAnimationsSaveJson ToJson(ElementAnimationsSave source)
    {
        return new ElementAnimationsSaveJson
        {
            ElementName = source.ElementName,
            Animations = source.Animations.Select(ToJson).ToList(),
        };
    }

    public static ElementAnimationsSave FromJson(ElementAnimationsSaveJson dto)
    {
        ElementAnimationsSave result = new ElementAnimationsSave { ElementName = dto.ElementName };
        result.Animations = dto.Animations.Select(FromJson).ToList();
        return result;
    }

    private static AnimationSaveJson ToJson(AnimationSave source)
    {
        return new AnimationSaveJson
        {
            Name = source.Name,
            Loops = source.Loops,
            States = source.States.Select(ToJson).ToList(),
            Animations = source.Animations.Select(ToJson).ToList(),
            Events = source.Events.Select(ToJson).ToList(),
        };
    }

    private static AnimationSave FromJson(AnimationSaveJson dto)
    {
        AnimationSave result = new AnimationSave { Name = dto.Name, Loops = dto.Loops };
        result.States = dto.States.Select(FromJson).ToList();
        result.Animations = dto.Animations.Select(FromJson).ToList();
        result.Events = dto.Events.Select(FromJson).ToList();
        return result;
    }

    private static AnimatedStateSaveJson ToJson(AnimatedStateSave source)
    {
        return new AnimatedStateSaveJson
        {
            StateName = source.StateName,
            Time = source.Time,
            InterpolationType = source.InterpolationType,
            Easing = source.Easing,
        };
    }

    private static AnimatedStateSave FromJson(AnimatedStateSaveJson dto)
    {
        return new AnimatedStateSave
        {
            StateName = dto.StateName,
            Time = dto.Time,
            InterpolationType = dto.InterpolationType,
            Easing = dto.Easing,
        };
    }

    private static AnimationReferenceSaveJson ToJson(AnimationReferenceSave source)
    {
        return new AnimationReferenceSaveJson { Name = source.Name, Time = source.Time };
    }

    private static AnimationReferenceSave FromJson(AnimationReferenceSaveJson dto)
    {
        return new AnimationReferenceSave { Name = dto.Name, Time = dto.Time };
    }

    private static NamedEventSaveJson ToJson(NamedEventSave source)
    {
        return new NamedEventSaveJson { Name = source.Name, Time = source.Time };
    }

    private static NamedEventSave FromJson(NamedEventSaveJson dto)
    {
        return new NamedEventSave { Name = dto.Name, Time = dto.Time };
    }
}
