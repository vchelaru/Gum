using Gum.Wireframe;

namespace Gum.StateAnimation.Runtime
{
    public class RunningStateAnimation
    {
        public AnimationRuntime Animation { get; }
        public GraphicalUiElement Target { get; }
        public double StartTime { get; }

        public RunningStateAnimation(AnimationRuntime animation, GraphicalUiElement target, double startTime)
        {
            Animation = animation;
            Target = target;
            StartTime = startTime;
        }

        public bool Update(double currentTime)
        {
            var elapsed = currentTime - StartTime;
            if (Animation.Loops && Animation.Length > 0)
            {
                elapsed %= Animation.Length;
            }
            if (!Animation.Loops && elapsed > Animation.Length)
            {
                elapsed = Animation.Length;
            }
            Animation.ApplyAtTimeTo(elapsed, Target);
            return !Animation.Loops && elapsed >= Animation.Length;
        }
    }
}
