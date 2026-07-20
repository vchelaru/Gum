namespace Gum.Managers;

/// <summary>
/// ImageList index constants for the element tree view's icons (Screens/Components/Standard/
/// Behaviors panel). Relocated from <see cref="ElementTreeViewManager"/> (ADR-0005 Phase 3) so
/// <see cref="TreeNodeImageLogic"/> and other non-WinForms consumers can reference them without
/// depending on the WinForms-coupled ElementTreeViewManager. Values map to the insertion order of
/// the <c>TryInjectIcon</c> calls in <c>ElementTreeViewCreator.InjectDynamicIcons</c> — renumbering
/// requires reordering those calls to match (see the gum-tool-tree-view skill).
/// </summary>
public static class TreeNodeImageIndices
{
    public const int TransparentImageIndex = 0;
    public const int FolderImageIndex = 1;
    public const int ComponentImageIndex = 2;
    public const int InstanceImageIndex = 3;
    public const int ScreenImageIndex = 4;
    public const int StandardElementImageIndex = 5;
    public const int ExclamationIndex = 6;
    public const int StateImageIndex = 7;
    public const int BehaviorImageIndex = 8;
    public const int DerivedInstanceImageIndex = 9;
    public const int LockedInstanceImageIndex = 10;

    // Per-type standard element icons. Indices must match the TryInjectIcon
    // call order in ElementTreeViewCreator.InjectDynamicIcons.
    public const int ContainerImageIndex = 11;
    public const int SpriteImageIndex = 12;
    public const int NineSliceImageIndex = 13;
    public const int TextImageIndex = 14;
    public const int RectangleImageIndex = 15;
    public const int ColoredRectangleImageIndex = 16;
    public const int CircleImageIndex = 17;
    public const int ColoredCircleImageIndex = 18;
    public const int RoundedRectangleImageIndex = 19;
    public const int PolygonImageIndex = 20;
    public const int ArcImageIndex = 21;
    public const int LineImageIndex = 22;
    public const int CanvasImageIndex = 23;
    public const int LottieAnimationImageIndex = 24;
    public const int SvgImageIndex = 25;

    // Per-type instance icons (same shapes, blue tint instead of purple).
    // Indices must match the TryInjectIcon call order in
    // ElementTreeViewCreator.InjectDynamicIcons.
    public const int ContainerInstanceImageIndex = 26;
    public const int SpriteInstanceImageIndex = 27;
    public const int NineSliceInstanceImageIndex = 28;
    public const int TextInstanceImageIndex = 29;
    public const int RectangleInstanceImageIndex = 30;
    public const int ColoredRectangleInstanceImageIndex = 31;
    public const int CircleInstanceImageIndex = 32;
    public const int ColoredCircleInstanceImageIndex = 33;
    public const int RoundedRectangleInstanceImageIndex = 34;
    public const int PolygonInstanceImageIndex = 35;
    public const int ArcInstanceImageIndex = 36;
    public const int LineInstanceImageIndex = 37;
    public const int CanvasInstanceImageIndex = 38;
    public const int LottieAnimationInstanceImageIndex = 39;
    public const int SvgInstanceImageIndex = 40;
}
