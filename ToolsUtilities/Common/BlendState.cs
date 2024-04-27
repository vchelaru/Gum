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

        private Color _blendFactor;

        private int _multiSampleMask;

        private bool _independentBlendEnable;


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

        static BlendState()
        {
            Additive = new BlendState("BlendState.Additive", Blend.SourceAlpha, Blend.One);
            AlphaBlend = new BlendState("BlendState.AlphaBlend", Blend.One, Blend.InverseSourceAlpha);
            NonPremultiplied = new BlendState("BlendState.NonPremultiplied", Blend.SourceAlpha, Blend.InverseSourceAlpha);
            Opaque = new BlendState("BlendState.Opaque", Blend.One, Blend.Zero);
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