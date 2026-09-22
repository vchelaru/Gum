using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.StateAnimation.SaveClasses;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ProjectServices;

/// <summary>
/// Reports animation keyframes that reference a state which no longer exists on the element. Runs as
/// part of the headless per-element error pass (via <see cref="IAdditionalErrorSource"/>), so the
/// error surfaces on project open and on edits — not only when the State Animation plugin's view
/// model happens to hold the element. See issue #3293.
/// </summary>
/// <remarks>
/// Currently validates state keyframes (<c>AnimationSave.States</c>). Sub-animation references
/// (<c>AnimationSave.Animations</c>) are not yet validated here.
/// </remarks>
public class AnimationKeyframeErrorSource : IAdditionalErrorSource
{
    private readonly IElementAnimationsProvider _animationsProvider;

    public AnimationKeyframeErrorSource(IElementAnimationsProvider animationsProvider)
    {
        _animationsProvider = animationsProvider;
    }

    /// <inheritdoc/>
    public IEnumerable<ErrorResult> GetErrors(ElementSave element, GumProjectSave project)
    {
        ElementAnimationsSave? animations = _animationsProvider.GetAnimationsFor(element, project);
        if (animations == null)
        {
            yield break;
        }

        foreach (AnimationSave animation in animations.Animations)
        {
            foreach (AnimatedStateSave keyframe in animation.States)
            {
                if (!string.IsNullOrEmpty(keyframe.StateName) && !StateExists(keyframe.StateName, element))
                {
                    yield return new ErrorResult
                    {
                        ElementName = element.Name,
                        Message = $"Animation '{animation.Name}' keyframe at time {keyframe.Time} " +
                                  $"references state '{keyframe.StateName}' which does not exist.",
                        Severity = ErrorSeverity.Error
                    };
                }
            }

            foreach (AnimationReferenceSave keyframe in animation.Animations)
            {
                if (!string.IsNullOrEmpty(keyframe.Name) && !SubAnimationExists(keyframe, element, animations, project))
                {
                    yield return new ErrorResult
                    {
                        ElementName = element.Name,
                        Message = $"Animation '{animation.Name}' keyframe at time {keyframe.Time} " +
                                  $"plays animation '{keyframe.Name}' which does not exist.",
                        Severity = ErrorSeverity.Error
                    };
                }
            }
        }
    }

    /// <summary>
    /// Mirrors the State Animation plugin's reference resolution: "Animation" is one of the element's
    /// own animations; "Instance.Animation" is one of the instance's element's animations.
    /// </summary>
    private bool SubAnimationExists(AnimationReferenceSave keyframe, ElementSave element, ElementAnimationsSave ownAnimations, GumProjectSave project)
    {
        if (string.IsNullOrEmpty(keyframe.SourceObject))
        {
            return ownAnimations.Animations.Any(item => item.Name == keyframe.RootName);
        }

        InstanceSave? instance = element.Instances.FirstOrDefault(item => item.Name == keyframe.SourceObject);
        ElementSave? instanceElement = instance == null ? null : project.AllElements.FirstOrDefault(item => item.Name == instance.BaseType);
        if (instanceElement == null)
        {
            return false;
        }
        ElementAnimationsSave? instanceAnimations = _animationsProvider.GetAnimationsFor(instanceElement, project);
        return instanceAnimations?.Animations.Any(item => item.Name == keyframe.RootName) == true;
    }

    /// <summary>
    /// Mirrors the State Animation plugin's categorized-name lookup: a keyframe's
    /// <c>StateName</c> is either "Category/State" or a top-level state name.
    /// </summary>
    private static bool StateExists(string categorizedStateName, ElementSave element)
    {
        int slashIndex = categorizedStateName.IndexOf('/');
        if (slashIndex >= 0)
        {
            string categoryName = categorizedStateName.Substring(0, slashIndex);
            string stateName = categorizedStateName.Substring(slashIndex + 1);
            StateSaveCategory? category = element.Categories.FirstOrDefault(item => item.Name == categoryName);
            return category != null && category.States.Any(item => item.Name == stateName);
        }

        return element.States.Any(item => item.Name == categorizedStateName);
    }
}
