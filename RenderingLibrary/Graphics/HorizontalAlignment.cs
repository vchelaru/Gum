namespace RenderingLibrary.Graphics;

public enum HorizontalAlignment
{
    Left,
    Center,
    Right
}

public static class HorizontalAlignmentExtensionMethods
{
    public static HorizontalAlignment Flip(this HorizontalAlignment alignment)
    {
        switch(alignment)
        {
            case HorizontalAlignment.Left: return HorizontalAlignment.Right;
            case HorizontalAlignment.Right: return HorizontalAlignment.Left;
        }
        return alignment;
    }
}
