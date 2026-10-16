using Gum;
using RenderingLibrary.Graphics;

namespace RenderingLibrary
{
    public interface ISurfaceInvalidatable
    {
        void InvalidateSurface();
    }

    public interface ISystemManagers : ISurfaceInvalidatable
    {
        bool EnableTouchEvents { get; set; }
        IRenderer Renderer { get; }

        /// <summary>
        /// The <see cref="GumContext"/> of the service these managers belong to, or null when they do not
        /// belong to a service. Building an element from these managers activates it.
        /// </summary>
        GumContext? Context => null;

        private static ISystemManagers _processWideDefault = null!;

        /// <summary>
        /// Set by Gum initialization (e.g. GumService.Initialize); not usable before that. With several
        /// services this is the current <see cref="GumContext"/>'s managers, falling back to the
        /// process-wide value when that context has none.
        /// </summary>
        public static ISystemManagers Default
        {
            get
            {
                GumContext? context = GumContext.Current;
                if (context == null || context.AdoptsProcessStatics)
                {
                    return _processWideDefault;
                }
                return context.Managers ?? _processWideDefault;
            }
            set
            {
                GumContext? context = GumContext.Current;
                if (context == null || context.AdoptsProcessStatics)
                {
                    _processWideDefault = value;
                }
                else
                {
                    context.Managers = value;
                }
            }
        }
    }
}
