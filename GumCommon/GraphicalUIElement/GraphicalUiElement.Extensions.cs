using Gum.StateAnimation.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{

    #region GraphicalUiElementExtensions
    public static class GraphicalUiElementExtensions
    {
#if !FRB

        /// <summary>
        /// Sets variables on the argument GraphicalUiElement from the animation at the specified index based on the given time.
        /// </summary>
        /// <param name="graphicalUiElement">The GraphicalUiElement on which to apply the animation.</param>
        /// <param name="index">The index of the animation to apply.</param>
        /// <param name="timeInSeconds">The elapsed time since the animation started, in seconds.</param>
        public static void ApplyAnimation(this GraphicalUiElement graphicalUiElement, int index, double timeInSeconds)
        {
            var animation = graphicalUiElement.GetAnimation(index);
            if (animation == null)
            {
                throw new ArgumentException(nameof(index), $"Could not find an animation at index {index}");
            }
            graphicalUiElement.ApplyAnimation(animation, timeInSeconds);
        }

        /// <summary>
        /// Sets variables on the argument GraphicalUiElement from the animation with the specified name based on the given time.
        /// </summary>
        /// <param name="graphicalUiElement">The GraphicalUiElement on which to apply the animation.</param>
        /// <param name="name">The name of the animation to apply.</param>
        /// <param name="timeInSeconds">The elapsed time since the animation started, in seconds.</param>
        public static void ApplyAnimation(this GraphicalUiElement graphicalUiElement, string name, double timeInSeconds)
        {
            var animation = graphicalUiElement.GetAnimation(name);
            if (animation == null)
            {
                throw new ArgumentException(nameof(name), $"Could not find an animation with the name {name}");
            }
            graphicalUiElement.ApplyAnimation(animation, timeInSeconds);
        }

        /// <summary>
        /// Sets variables on the argument GraphicalUiElement from the specified AnimationRuntime based on the given time.
        /// </summary>
        /// <param name="graphicalUiElement">The GraphicalUiElement on which to apply the animation</param>
        /// <param name="animation">The AnimationRuntime object to apply</param>
        /// <param name="timeInSeconds">The elapesd time since the animation started, in seconds.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public static void ApplyAnimation(this GraphicalUiElement graphicalUiElement, AnimationRuntime animation, double timeInSeconds)
        {
            if (animation != null)
            {
                animation.ApplyAtTimeTo(timeInSeconds, graphicalUiElement);
            }
            else
            {
                throw new ArgumentNullException(nameof(animation), "the AnimationRuntime cannot be null");
            }
        }

        /// <summary>
        /// Starts playing the animation at the specified index.
        /// </summary>
        /// <param name="graphicalUiElement">The GraphicalUiElement on which to play the animation.</param>
        /// <param name="index">The index of the animation to play.</param>
        public static void PlayAnimation(this GraphicalUiElement graphicalUiElement, int index)
        {
            var animation = graphicalUiElement.GetAnimation(index);
            if (animation == null)
            {
                throw new ArgumentException(nameof(index), $"Could not find an animation at the index {index}");
            }
            graphicalUiElement.PlayAnimation(animation);
        }

        /// <summary>
        /// Starts playing the animation with the specified name.
        /// </summary>
        /// <param name="graphicalUiElement">The GraphicalUiElement on which to play the animation.</param>
        /// <param name="name">The name of the animation to play.</param>
        public static void PlayAnimation(this GraphicalUiElement graphicalUiElement, string name)
        {
            var animation = graphicalUiElement.GetAnimation(name);
            if (animation == null)
            {
                throw new ArgumentException(nameof(name), $"Could not find an animation with the name {name}");
            }
            graphicalUiElement.PlayAnimation(animation);
        }


        /// <summary>
        /// Gets the animation at the specified index.
        /// </summary>
        /// <param name="graphicalUiElement">the GraphicalUiElement to get the animation from</param>
        /// <param name="index">the index of the animation to get</param>
        /// <returns>The animation if found, otherwise returns null.</returns>
        public static AnimationRuntime? GetAnimation(this GraphicalUiElement graphicalUiElement, int index)
        {
            if (graphicalUiElement.Animations != null && index >= 0 && index < graphicalUiElement.Animations.Count)
            {
                return graphicalUiElement.Animations[index];
            }

            return null;
        }

        /// <summary>
        /// Get the animation at the specified name.
        /// </summary>
        /// <param name="graphicalUiElement">The GraphicalUiElement to get the animation from</param>
        /// <param name="animationName">The name of the animation to get</param>
        /// <returns>The animation if found, otherwise returns null.</returns>
        public static AnimationRuntime? GetAnimation(this GraphicalUiElement graphicalUiElement, string animationName)
        {
            return graphicalUiElement.Animations?.FirstOrDefault(item => item.Name == animationName);
        }


#endif

        #endregion
    }
}
