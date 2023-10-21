namespace Gum
{
    public class BlendState
    {
        //
        // Summary:
        //     A built-in state object with settings for opaque blend that is overwriting the
        //     source with the destination data.
        public static readonly BlendState Opaque = new BlendState();
        //
        // Summary:
        //     A built-in state object with settings for alpha blend that is blending the source
        //     and destination data using alpha.
        public static readonly BlendState AlphaBlend = new BlendState();
        //
        // Summary:
        //     A built-in state object with settings for additive blend that is adding the destination
        //     data to the source data without using alpha.
        public static readonly BlendState Additive = new BlendState();
        //
        // Summary:
        //     A built-in state object with settings for blending with non-premultipled alpha
        //     that is blending source and destination data by using alpha while assuming the
        //     color data contains no alpha information.
        public static readonly BlendState NonPremultiplied = new BlendState();
    }
}
