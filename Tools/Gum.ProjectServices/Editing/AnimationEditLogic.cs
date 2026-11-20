using System.Linq;
using FlatRedBall.Glue.StateInterpolation;
using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// The data changes to an element's animations (<see cref="ElementAnimationsSave"/>) that need no tool state: add
/// and remove an animation, add and remove a state keyframe. Each <c>GetWhyCannot</c> method gives the reason a
/// change would be refused, or null.
/// </summary>
public interface IAnimationEditLogic
{
    /// <summary>
    /// Why an animation named <paramref name="name"/> cannot be added to <paramref name="element"/>, or null.
    /// </summary>
    string? GetWhyCannotAddAnimation(ElementSave element, ElementAnimationsSave animations, string? name);

    /// <summary>
    /// Adds an animation with no keyframes and returns it.
    /// </summary>
    AnimationSave AddAnimation(ElementAnimationsSave animations, string name, bool loops);

    /// <summary>
    /// Removes the animation named <paramref name="name"/>. False when there is none.
    /// </summary>
    bool RemoveAnimation(ElementAnimationsSave animations, string name);

    /// <summary>
    /// Why a state keyframe for <paramref name="categorizedStateName"/> at <paramref name="time"/> cannot be added to
    /// <paramref name="animation"/>, or null.
    /// </summary>
    string? GetWhyCannotAddKeyframe(ElementSave element, AnimationSave animation, string categorizedStateName, float time);

    /// <summary>
    /// Adds a state keyframe in time order, after any keyframe at the same time, and returns it.
    /// </summary>
    AnimatedStateSave AddKeyframe(AnimationSave animation, string categorizedStateName, float time,
        InterpolationType interpolation, Easing easing);

    /// <summary>
    /// Removes the state keyframe for <paramref name="categorizedStateName"/> at <paramref name="time"/>. False when
    /// there is none.
    /// </summary>
    bool RemoveKeyframe(AnimationSave animation, string categorizedStateName, float time);
}

/// <inheritdoc/>
public class AnimationEditLogic : IAnimationEditLogic
{
    private readonly IElementNameValidator _nameValidator;
    private readonly NameRules _rules = new NameRules();

    /// <param name="nameValidator">Checks an animation's name against the element's other top-level names.</param>
    public AnimationEditLogic(IElementNameValidator nameValidator)
    {
        _nameValidator = nameValidator;
    }

    /// <inheritdoc/>
    public string? GetWhyCannotAddAnimation(ElementSave element, ElementAnimationsSave animations, string? name)
    {
        if (AnimationNameRules.GetWhyInvalid(name, animations.Animations.Select(item => item.Name)) is { } why)
        {
            return why;
        }
        if (!_rules.IsNameValidCommon(name, out string commonWhyNotValid, out _))
        {
            return commonWhyNotValid;
        }
        if (!_nameValidator.IsNameValidTopLevel(name, element, objectToIgnore: null, out string? topLevelWhyNotValid))
        {
            return topLevelWhyNotValid;
        }
        return null;
    }

    /// <inheritdoc/>
    public AnimationSave AddAnimation(ElementAnimationsSave animations, string name, bool loops)
    {
        AnimationSave animation = new AnimationSave { Name = name, Loops = loops };
        animations.Animations.Add(animation);
        return animation;
    }

    /// <inheritdoc/>
    public bool RemoveAnimation(ElementAnimationsSave animations, string name) =>
        animations.Animations.RemoveAll(item => item.Name == name) > 0;

    /// <inheritdoc/>
    public string? GetWhyCannotAddKeyframe(ElementSave element, AnimationSave animation, string categorizedStateName, float time)
    {
        if (float.IsNaN(time) || float.IsInfinity(time) || time < 0)
        {
            return $"time {time} is not valid: use a number of seconds that is 0 or more.";
        }
        if (!AnimationStateLookup.StateExists(categorizedStateName, element))
        {
            return $"{element.Name} has no state named {categorizedStateName}; a state in a category is written Category/State. " +
                "Add the state first.";
        }
        if (animation.States.Any(item => item.StateName == categorizedStateName && item.Time == time))
        {
            return $"{animation.Name} already has a keyframe for {categorizedStateName} at time {time}.";
        }
        return null;
    }

    /// <inheritdoc/>
    public AnimatedStateSave AddKeyframe(AnimationSave animation, string categorizedStateName, float time,
        InterpolationType interpolation, Easing easing)
    {
        AnimatedStateSave keyframe = new AnimatedStateSave
        {
            StateName = categorizedStateName,
            Time = time,
            InterpolationType = interpolation,
            Easing = easing
        };
        animation.States.Insert(AnimationKeyframeOrder.GetInsertIndex(animation.States, item => item.Time, time), keyframe);
        return keyframe;
    }

    /// <inheritdoc/>
    public bool RemoveKeyframe(AnimationSave animation, string categorizedStateName, float time) =>
        animation.States.RemoveAll(item => item.StateName == categorizedStateName && item.Time == time) > 0;
}
