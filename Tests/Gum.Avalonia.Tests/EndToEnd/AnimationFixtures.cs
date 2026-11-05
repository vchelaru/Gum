using Gum.StateAnimation.SaveClasses;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Writes a <c>.ganx</c> fixture through the tool's own serializer. A hand-written file differs
/// from what the tool saves (line endings, encoding), so a scenario that snapshots the project
/// and later compares it with a tool-saved file would report a difference the user never made.
/// </summary>
internal static class AnimationFixtures
{
    /// <summary>Writes an animation file at <paramref name="path"/> with one animation holding one keyframe on <paramref name="stateName"/>.</summary>
    public static void Write(string path, string animationName, string stateName)
    {
        ElementAnimationsSave animations = new ElementAnimationsSave();
        AnimationSave animation = new AnimationSave { Name = animationName };
        animation.States.Add(new AnimatedStateSave { StateName = stateName, Time = 0 });
        animations.Animations.Add(animation);
        animations.Save(path);
    }

    /// <summary>Writes an animation file at <paramref name="path"/> with one animation that plays <paramref name="referencedAnimation"/> (<c>Instance.Animation</c>) at time 0.</summary>
    public static void WriteSubAnimation(string path, string animationName, string referencedAnimation)
    {
        ElementAnimationsSave animations = new ElementAnimationsSave();
        AnimationSave animation = new AnimationSave { Name = animationName };
        animation.Animations.Add(new AnimationReferenceSave { Name = referencedAnimation, Time = 0 });
        animations.Animations.Add(animation);
        animations.Save(path);
    }
}
