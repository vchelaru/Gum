using System;
using System.Drawing;

namespace Gum
{
    public enum BlendFunction
    {
        Add,
        Subtract,
        ReverseSubtract,
        Min,
        Max
    }

    public enum Blend
    {
        One,
        Zero,
        SourceColor,
        InverseSourceColor,
        SourceAlpha,
        InverseSourceAlpha,
        DestinationColor,
        InverseDestinationColor,
        DestinationAlpha,
        InverseDestinationAlpha,
        BlendFactor,
        InverseBlendFactor,
        SourceAlphaSaturation
    }

    [Flags]
    public enum ColorWriteChannels
    {
        None = 0,
        Red = 1,
        Green = 2,
        Blue = 4,
        Alpha = 8,
        All = 0xF
    }

    public class BlendState
    {
        string Name;

        // these are not used. They originally existed
        // because MonoGame had them, so maybe we'll need
        // them later?
        //private Color _blendFactor;
        //private int _multiSampleMask;
        //private bool _independentBlendEnable;

        public BlendFunction AlphaBlendFunction { get; set; }

        public Blend AlphaDestinationBlend { get; set; }

        public Blend AlphaSourceBlend { get; set; }

        public BlendFunction ColorBlendFunction { get; set; }

        public Blend ColorDestinationBlend { get; set; }

        public Blend ColorSourceBlend { get; set; }

        public ColorWriteChannels ColorWriteChannels { get; set; }

        public ColorWriteChannels ColorWriteChannels1 { get; set; }

        public ColorWriteChannels ColorWriteChannels2 { get; set; }

        public ColorWriteChannels ColorWriteChannels3 { get; set; }

        public Color BlendFactor { get; set; }

        public int MultiSampleMask { get; set; }

        public bool IndependentBlendEnable { get; set; }

        //
        // Summary:
        //     A built-in state object with settings for opaque blend that is overwriting the
        //     source with the destination data.
        public static readonly BlendState Opaque;
        //
        // Summary:
        //     A built-in state object with settings for alpha blend that is blending the source
        //     and destination data using alpha.
        public static readonly BlendState AlphaBlend;
        //
        // Summary:
        //     A built-in state object with settings for additive blend that is adding the destination
        //     data to the source data without using alpha.
        public static readonly BlendState Additive;
        //
        // Summary:
        //     A built-in state object with settings for blending with non-premultipled alpha
        //     that is blending source and destination data by using alpha while assuming the
        //     color data contains no alpha information.
        public static readonly BlendState NonPremultiplied;

        public static readonly BlendState NonPremultipliedAddAlpha;

        public static readonly BlendState SubtractAlpha;
        public static readonly BlendState ReplaceAlpha;
        public static readonly BlendState MinAlpha;

        static BlendState()
        {
            Additive = new BlendState("BlendState.Additive", Blend.SourceAlpha, Blend.One);
            AlphaBlend = new BlendState("BlendState.AlphaBlend", Blend.One, Blend.InverseSourceAlpha);
            NonPremultiplied = new BlendState("BlendState.NonPremultiplied", Blend.SourceAlpha, Blend.InverseSourceAlpha);
            Opaque = new BlendState("BlendState.Opaque", Blend.One, Blend.Zero);


            {

                // Vic 2/6/2025, pulled from 12/19/2020
                // This took me a while to figure out, so I'll document what I learned.
                // For alpha, the operation is:
                // ResultAlpha = (SourceAlpha * Blend.AlphaSourceBlend) {BlendFunc} (DestinationAlpha * Blend.AlphaDestblend)
                // where:
                // ResultAlpha is the resulting pixel alpha after the operation occurs
                // SourceAlpha is the alpha of the pixel on the sprite (or current item) that is being drawn
                // DestinationAlpha is the alpha of the pixel on the surface before the pixel is drawn, which is the result alpha from a previous operation
                // In this case we want to subtract the sprite being drawn.
                // To subtract the sprite that is being drawn, which is the SourceSprite, we need to do a ReverseSubtract
                // so that the Source is being subtracted.
                // We want to use Blend.One on both so that the values being used are the pixel values on source and dest.
                // Keep in mind that since we're making a texture, we need this texture to be premultiplied, so we
                // need to multiply the destination color by the inverse source alpha, so that if alpha is 0, we preserve the color, otherwise we
                // darken it to premult
                SubtractAlpha = new BlendState();
                SubtractAlpha.ColorSourceBlend = Blend.Zero;
                SubtractAlpha.ColorBlendFunction = BlendFunction.Add;
                SubtractAlpha.ColorDestinationBlend = Blend.One;

                SubtractAlpha.AlphaSourceBlend = Blend.One;
                SubtractAlpha.AlphaBlendFunction = BlendFunction.ReverseSubtract;
                SubtractAlpha.AlphaDestinationBlend = Blend.One;

                // 2/6/2025
                // I don't yet
                // understand why
                // these properties
                // are needed, but if
                // I don't assign them,
                // the blend doesn't work
                // correctly.
                SubtractAlpha.BlendFactor = Color.White;
                SubtractAlpha.ColorWriteChannels = ColorWriteChannels.All;
                SubtractAlpha.ColorWriteChannels1 = ColorWriteChannels.All;
                SubtractAlpha.ColorWriteChannels2 = ColorWriteChannels.All;
                SubtractAlpha.ColorWriteChannels3 = ColorWriteChannels.All;
            }


            ReplaceAlpha = new BlendState();
            ReplaceAlpha.ColorSourceBlend = Blend.Zero;
            ReplaceAlpha.ColorBlendFunction = BlendFunction.Add;
            ReplaceAlpha.ColorDestinationBlend = Blend.One;

            ReplaceAlpha.AlphaSourceBlend = Blend.One;
            ReplaceAlpha.AlphaBlendFunction = BlendFunction.Add;
            ReplaceAlpha.AlphaDestinationBlend = Blend.Zero;

            ReplaceAlpha.BlendFactor = Color.White;
            ReplaceAlpha.ColorWriteChannels = ColorWriteChannels.All;
            ReplaceAlpha.ColorWriteChannels1 = ColorWriteChannels.All;
            ReplaceAlpha.ColorWriteChannels2 = ColorWriteChannels.All;
            ReplaceAlpha.ColorWriteChannels3 = ColorWriteChannels.All;


            MinAlpha = new BlendState();
            MinAlpha.ColorSourceBlend = Blend.Zero;
            MinAlpha.ColorBlendFunction = BlendFunction.Add;
            MinAlpha.ColorDestinationBlend = Blend.One;

            MinAlpha.AlphaSourceBlend = Blend.One;
            MinAlpha.AlphaBlendFunction = BlendFunction.Min;
            MinAlpha.AlphaDestinationBlend = Blend.One;

            MinAlpha.BlendFactor = Color.White;
            MinAlpha.ColorWriteChannels = ColorWriteChannels.All;
            MinAlpha.ColorWriteChannels1 = ColorWriteChannels.All;
            MinAlpha.ColorWriteChannels2 = ColorWriteChannels.All;
            MinAlpha.ColorWriteChannels3 = ColorWriteChannels.All;







            NonPremultipliedAddAlpha = new BlendState();

            NonPremultipliedAddAlpha.ColorSourceBlend = BlendState.NonPremultiplied.ColorSourceBlend;
            NonPremultipliedAddAlpha.ColorDestinationBlend = BlendState.NonPremultiplied.ColorDestinationBlend;
            NonPremultipliedAddAlpha.ColorBlendFunction = BlendState.NonPremultiplied.ColorBlendFunction;

            NonPremultipliedAddAlpha.AlphaSourceBlend = Blend.SourceAlpha;
            NonPremultipliedAddAlpha.AlphaDestinationBlend = Blend.DestinationAlpha;
            NonPremultipliedAddAlpha.AlphaBlendFunction = BlendFunction.Add;
        }

        public BlendState()
        {
            // not sure what to do here...
        }



        private BlendState(string name, Blend sourceBlend, Blend destinationBlend)
        {
            Name = name;
            ColorSourceBlend = sourceBlend;
            AlphaSourceBlend = sourceBlend;
            ColorDestinationBlend = destinationBlend;
            AlphaDestinationBlend = destinationBlend;
            //_defaultStateObject = true;
        }




        public override string ToString() => Name;
    }
}
namespace RenderingLibrary.Graphics
{
    public enum ColorOperation
    {
        //Texture,
        //Add,
        //Subtract,
        Modulate = 3,
        //InverseTexture,
        //Color,
        ColorTextureAlpha = 6,
        //Modulate2X,
        //Modulate4X,
        //InterpolateColor

    }

}