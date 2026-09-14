using RenderingLibrary;
using RenderingLibrary.Graphics;
using System.Linq;

namespace Gum.Managers
{
    /// <summary>
    /// Owns the top-most editor overlay layer used by the wireframe editor tab. Plugin-scoped:
    /// instantiated by the editor plugin (<c>MainEditorTabPlugin</c>), not registered in Builder.cs.
    /// </summary>
    public interface IToolLayerService
    {
        /// <summary>
        /// The layer kept above all others, used for editor overlay visuals. Null until
        /// <see cref="Initialize"/> has been called.
        /// </summary>
        Layer TopLayer { get; }

        /// <summary>
        /// Creates the top layer. Must be called after <see cref="SystemManagers.Default"/> is initialized.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Re-adds the top layer if another plugin pushed a layer above it. Call once per frame.
        /// </summary>
        void Activity();
    }

    /// <inheritdoc cref="IToolLayerService"/>
    public class ToolLayerService : IToolLayerService
    {
        public Layer TopLayer { get; private set; }

        public void Initialize()
        {
            TopLayer = SystemManagers.Default.Renderer.AddLayer();
        }

        public void Activity()
        {
            // just in case another plugin adds more layers, keep this one on top:
            if (SystemManagers.Default.Renderer.Layers.Last() != TopLayer)
            {
                SystemManagers.Default.Renderer.RemoveLayer(TopLayer);
                SystemManagers.Default.Renderer.AddLayer(TopLayer);
            }
        }
    }
}
