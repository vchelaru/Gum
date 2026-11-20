using System;
using System.Linq;
using System.Text.Json;
using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The animation operations of the operations file (<c>addAnimation</c>, <c>removeAnimation</c>,
/// <c>setAnimationLoops</c>, <c>addKeyframe</c>, <c>removeKeyframe</c>). Each reads its fields, asks
/// <see cref="IAnimationEditLogic"/> whether the change is allowed, and changes the element's animations held by
/// <see cref="PendingAnimationEdits"/>; nothing is written until the edit run saves.
/// </summary>
public class AnimationEditOperations
{
    private readonly IAnimationEditLogic _logic;
    private readonly PendingAnimationEdits _pending;

    public AnimationEditOperations(IAnimationEditLogic logic, PendingAnimationEdits pending)
    {
        _logic = logic;
        _pending = pending;
    }

    /// <summary>The <c>addAnimation</c> operation.</summary>
    public void AddAnimation(GumProjectSave project, JsonElement operation)
    {
        (ElementSave element, ElementAnimationsSave animations) = GetAnimations(project, operation);
        string name = ProjectEditService.GetString(operation, "name");
        if (_logic.GetWhyCannotAddAnimation(element, animations, name) is { } why)
        {
            throw new EditException(why);
        }
        _logic.AddAnimation(animations, name, GetOptionalBool(operation, "loops") ?? false);
    }

    /// <summary>The <c>removeAnimation</c> operation.</summary>
    public void RemoveAnimation(GumProjectSave project, JsonElement operation)
    {
        (ElementSave element, ElementAnimationsSave animations) = GetAnimations(project, operation);
        string name = ProjectEditService.GetString(operation, "name");
        if (!_logic.RemoveAnimation(animations, name))
        {
            throw new EditException($"{element.Name} has no animation named {name}.");
        }
    }

    /// <summary>The <c>setAnimationLoops</c> operation.</summary>
    public void SetAnimationLoops(GumProjectSave project, JsonElement operation)
    {
        (ElementSave element, ElementAnimationsSave animations) = GetAnimations(project, operation);
        GetAnimation(element, animations, operation).Loops = GetOptionalBool(operation, "loops")!.Value;
    }

    /// <summary>The <c>addKeyframe</c> operation.</summary>
    public void AddKeyframe(GumProjectSave project, JsonElement operation)
    {
        (ElementSave element, ElementAnimationsSave animations) = GetAnimations(project, operation);
        AnimationSave animation = GetAnimation(element, animations, operation);
        string state = ProjectEditService.GetString(operation, "state");
        float time = GetTime(operation);
        InterpolationType interpolation = GetEnum(operation, "interpolation", new AnimatedStateSave().InterpolationType);
        Easing easing = GetEnum(operation, "easing", new AnimatedStateSave().Easing);
        if (_logic.GetWhyCannotAddKeyframe(element, animation, state, time) is { } why)
        {
            throw new EditException(why);
        }
        _logic.AddKeyframe(animation, state, time, interpolation, easing);
    }

    /// <summary>The <c>removeKeyframe</c> operation.</summary>
    public void RemoveKeyframe(GumProjectSave project, JsonElement operation)
    {
        (ElementSave element, ElementAnimationsSave animations) = GetAnimations(project, operation);
        AnimationSave animation = GetAnimation(element, animations, operation);
        string state = ProjectEditService.GetString(operation, "state");
        float time = GetTime(operation);
        if (!_logic.RemoveKeyframe(animation, state, time))
        {
            throw new EditException($"{animation.Name} has no keyframe for {state} at time {time}.");
        }
    }

    private (ElementSave Element, ElementAnimationsSave Animations) GetAnimations(GumProjectSave project, JsonElement operation)
    {
        ElementSave element = ProjectEditService.GetElement(project, operation);
        return (element, _pending.GetForEdit(element, project));
    }

    private static AnimationSave GetAnimation(ElementSave element, ElementAnimationsSave animations, JsonElement operation)
    {
        string name = ProjectEditService.GetString(operation, "animation");
        return animations.Animations.FirstOrDefault(item => item.Name == name)
            ?? throw new EditException($"{element.Name} has no animation named {name}.");
    }

    private static bool? GetOptionalBool(JsonElement operation, string field) =>
        operation.TryGetProperty(field, out JsonElement value) ? value.GetBoolean() : null;

    private static float GetTime(JsonElement operation)
    {
        JsonElement value = operation.GetProperty("time");
        return value.ValueKind == JsonValueKind.Number && value.TryGetSingle(out float time)
            ? time
            : throw new EditException($"time must be a number of seconds, not {value.GetRawText()}.");
    }

    private static T GetEnum<T>(JsonElement operation, string field, T defaultValue) where T : struct, Enum
    {
        if (!operation.TryGetProperty(field, out JsonElement value))
        {
            return defaultValue;
        }
        string? text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return text != null && Enum.TryParse(text, ignoreCase: false, out T parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new EditException($"{value.GetRawText()} is not a valid {field}: use {string.Join(", ", Enum.GetNames<T>())}.");
    }
}
