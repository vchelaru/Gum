namespace RenderingLibrary.Graphics
{
    /// <summary>
    /// How a renderable's <c>Color</c> combines with its texture when drawing. Values match
    /// FlatRedBall's ColorOperation numbering, so the commented-out members are reserved slots
    /// rather than gaps.
    /// </summary>
    public enum ColorOperation
    {
        //Texture,
        /// <summary>
        /// Draws the normal modulated pass, then adds the renderable's <c>Color</c> on top as a
        /// second additive pass. Black is the identity, so a black Color renders unchanged.
        /// </summary>
        Add = 1,
        //Subtract,
        /// <summary>
        /// Multiplies the texture by the renderable's <c>Color</c>. White is the identity.
        /// </summary>
        Modulate = 3,
        //InverseTexture,
        //Color,
        /// <summary>
        /// Uses the renderable's <c>Color</c> for RGB and the texture's alpha for transparency.
        /// </summary>
        ColorTextureAlpha = 6,
        //Modulate2X,
        //Modulate4X,
        //InterpolateColor

    }
}
