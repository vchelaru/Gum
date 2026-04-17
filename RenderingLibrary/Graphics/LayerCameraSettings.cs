using System.Numerics;

namespace RenderingLibrary.Graphics
{
    /// <summary>
    /// Per-layer camera overrides that let a <see cref="Layer"/> ignore, replace, or offset
    /// the main <see cref="Camera"/>'s position and zoom.
    /// </summary>
    /// <remarks>
    /// Assigning a <see cref="LayerCameraSettings"/> to a <see cref="Layer"/> changes how both
    /// rendering and cursor hit-testing are performed for objects on that layer. When this value
    /// is <see langword="null"/> (the default), the layer uses the main camera unchanged.
    /// </remarks>
    public class LayerCameraSettings
    {
        /// <summary>
        /// Whether the layer is rendered in screen space, ignoring the main camera's position.
        /// </summary>
        /// <remarks>
        /// When <see langword="true"/>, objects on this layer are positioned relative to the
        /// screen rather than the world: moving the main <see cref="Camera"/> has no effect on
        /// where they appear. This is typically used for HUDs, overlays, and other UI that
        /// should stay fixed on the screen while a world camera scrolls.
        /// <para>
        /// <see cref="Position"/> and <see cref="Zoom"/> still apply when this is
        /// <see langword="true"/>; they are simply applied on top of the screen origin
        /// instead of the main camera's world position.
        /// </para>
        /// </remarks>
        public bool IsInScreenSpace { get; set; }

        /// <summary>
        /// Overrides the main camera's zoom for this layer. If <see langword="null"/>, the main
        /// camera's zoom is used.
        /// </summary>
        /// <remarks>
        /// A value of 1 means no zoom, 2 means rendered objects appear twice as large, and 0.5
        /// means half size. This is applied independently of the main camera's zoom, so a UI
        /// layer can remain at 1:1 while a world layer zooms.
        /// </remarks>
        public float? Zoom { get; set; }

        /// <summary>
        /// An additional camera position applied to this layer, or <see langword="null"/> to use
        /// the main camera's position unchanged.
        /// </summary>
        /// <remarks>
        /// This value represents a <em>camera</em> position, not a layer offset. Because moving
        /// a camera in one direction makes the world appear to move in the opposite direction,
        /// the sign convention is the reverse of what you might expect if you are thinking of
        /// it as "shift the layer by this amount":
        /// <list type="bullet">
        /// <item><description>
        /// A positive <see cref="Vector2.X"/> moves the camera right, which makes the layer's
        /// content appear to shift <em>left</em> on screen.
        /// </description></item>
        /// <item><description>
        /// A positive <see cref="Vector2.Y"/> moves the camera down (Gum uses a Y-down screen
        /// convention for the main camera), which makes the layer's content appear to shift
        /// <em>up</em> on screen. A negative Y makes the content shift down.
        /// </description></item>
        /// </list>
        /// When <see cref="IsInScreenSpace"/> is <see langword="false"/>, this value is
        /// <em>added</em> to the main camera's position for this layer. When
        /// <see cref="IsInScreenSpace"/> is <see langword="true"/>, the main camera's position
        /// is ignored and this value is used on its own.
        /// <para>
        /// Rendering and cursor hit-testing both respect this value, so a cursor's world
        /// position on this layer is consistent with where its objects draw.
        /// </para>
        /// </remarks>
        public Vector2? Position;
    }
}
