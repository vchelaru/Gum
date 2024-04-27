namespace RenderingLibrary.Graphics
{
    public static class XNAExtensions
    {
        public static Microsoft.Xna.Framework.Color ToXNA(this System.Drawing.Color value)
        {
            return new Microsoft.Xna.Framework.Color(value.R, value.G, value.B, value.A);
        }

        public static System.Drawing.Color ToSystemDrawing(this Microsoft.Xna.Framework.Color value)
        {
            return System.Drawing.Color.FromArgb(value.A, value.R, value.G, value.B);
        }

        public static Microsoft.Xna.Framework.Point ToXNA(this System.Drawing.Point value)
        {
            return new Microsoft.Xna.Framework.Point(value.X, value.Y);
        }

        public static Microsoft.Xna.Framework.Rectangle ToXNA(this System.Drawing.Rectangle value)
        {
            return new Microsoft.Xna.Framework.Rectangle(value.X, value.Y, value.Width, value.Height);
        }

        public static System.Drawing.Rectangle ToSystemDrawing(this Microsoft.Xna.Framework.Rectangle value)
        {
            return new System.Drawing.Rectangle(value.X, value.Y, value.Width, value.Height);
        }

        public static Microsoft.Xna.Framework.Vector2 ToXNA(this System.Numerics.Vector2 value)
        {
            return new Microsoft.Xna.Framework.Vector2(value.X, value.Y);
        }

        public static System.Numerics.Vector2 ToSystemNumerics(this Microsoft.Xna.Framework.Vector2 value)
        {
            return new System.Numerics.Vector2(value.X, value.Y);
        }

        public static Microsoft.Xna.Framework.Vector3 ToXNA(this System.Numerics.Vector3 value)
        {
            return new Microsoft.Xna.Framework.Vector3(value.X, value.Y, value.Z);
        }

        public static Microsoft.Xna.Framework.Matrix ToXNA(this System.Numerics.Matrix4x4 value)
        {
            return new Microsoft.Xna.Framework.Matrix(
                value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44
            );
        }

        public static Microsoft.Xna.Framework.Graphics.BlendState ToXNA(this Gum.BlendState value)
        {
            if (value == null) return null;
            else if (value == Gum.BlendState.Opaque) return Microsoft.Xna.Framework.Graphics.BlendState.Opaque;
            else if (value == Gum.BlendState.AlphaBlend) return Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend;
            else if (value == Gum.BlendState.Additive) return Microsoft.Xna.Framework.Graphics.BlendState.Additive;
            else if (value == Gum.BlendState.NonPremultiplied) return Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied;
            else
            {
                var toReturn = new Microsoft.Xna.Framework.Graphics.BlendState();

                toReturn.AlphaBlendFunction = value.AlphaBlendFunction.ToXna();
                toReturn.AlphaDestinationBlend = value.AlphaDestinationBlend.ToXna();
                toReturn.AlphaSourceBlend = value.AlphaSourceBlend.ToXna();
                toReturn.ColorBlendFunction = value.ColorBlendFunction.ToXna();
                toReturn.ColorDestinationBlend = value.ColorDestinationBlend.ToXna();
                toReturn.ColorSourceBlend = value.ColorSourceBlend.ToXna();
                toReturn.ColorWriteChannels = value.ColorWriteChannels.ToXna();
                toReturn.ColorWriteChannels1 = value.ColorWriteChannels1.ToXna();
                toReturn.ColorWriteChannels2 = value.ColorWriteChannels2.ToXna();
                toReturn.ColorWriteChannels3 = value.ColorWriteChannels3.ToXna();
                toReturn.BlendFactor = value.BlendFactor.ToXNA();
                toReturn.MultiSampleMask = value.MultiSampleMask;
                // not supported in XNA, only MonoGame. Do we care?
                //toReturn.IndependentBlendEnable = value.IndependentBlendEnable;

                return toReturn;
            }
        }

        public static Microsoft.Xna.Framework.Graphics.BlendFunction ToXna(this Gum.BlendFunction value) =>
            (Microsoft.Xna.Framework.Graphics.BlendFunction)value;
        public static Microsoft.Xna.Framework.Graphics.Blend ToXna(this Gum.Blend value) =>
            (Microsoft.Xna.Framework.Graphics.Blend)value;
        public static Microsoft.Xna.Framework.Graphics.ColorWriteChannels ToXna(this Gum.ColorWriteChannels value) =>
            (Microsoft.Xna.Framework.Graphics.ColorWriteChannels)value;

        public static Gum.BlendFunction ToGum(this Microsoft.Xna.Framework.Graphics.BlendFunction value) =>
            (Gum.BlendFunction)value;
        public static Gum.Blend ToGum(this Microsoft.Xna.Framework.Graphics.Blend value) =>
            (Gum.Blend)value;
        public static Gum.ColorWriteChannels ToGum(this Microsoft.Xna.Framework.Graphics.ColorWriteChannels value) =>
            (Gum.ColorWriteChannels)value;



        public static Gum.BlendState ToGum(this Microsoft.Xna.Framework.Graphics.BlendState value)
        {
            if (value == null) return null;
            else if(value == Microsoft.Xna.Framework.Graphics.BlendState.Opaque) return Gum.BlendState.Opaque;
            else if (value == Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend) return Gum.BlendState.AlphaBlend;
            else if (value == Microsoft.Xna.Framework.Graphics.BlendState.Additive) return Gum.BlendState.Additive;
            else if (value == Microsoft.Xna.Framework.Graphics.BlendState.NonPremultiplied) return Gum.BlendState.NonPremultiplied;
            else
            {
                var toReturn = new Gum.BlendState();

                toReturn.AlphaBlendFunction = value.AlphaBlendFunction.ToGum();
                toReturn.AlphaDestinationBlend = value.AlphaDestinationBlend.ToGum();
                toReturn.AlphaSourceBlend = value.AlphaSourceBlend.ToGum();
                toReturn.ColorBlendFunction = value.ColorBlendFunction.ToGum();
                toReturn.ColorDestinationBlend = value.ColorDestinationBlend.ToGum();
                toReturn.ColorSourceBlend = value.ColorSourceBlend.ToGum();
                toReturn.ColorWriteChannels = value.ColorWriteChannels.ToGum();
                toReturn.ColorWriteChannels1 = value.ColorWriteChannels1.ToGum();
                toReturn.ColorWriteChannels2 = value.ColorWriteChannels2.ToGum();
                toReturn.ColorWriteChannels3 = value.ColorWriteChannels3.ToGum();
                toReturn.BlendFactor = value.BlendFactor.ToSystemDrawing();
                toReturn.MultiSampleMask = value.MultiSampleMask;

                // Not supported in XNA, only MonoGame. Do we care?
                //toReturn.IndependentBlendEnable = value.IndependentBlendEnable;

                return toReturn;
            }
        }

    }
}