using Gum.DataTypes;
using static Gum.Managers.TreeNodeImageIndices;

namespace Gum.Managers;

/// <summary>
/// Resolves the tree-view ImageList index for element, behavior, and instance nodes.
/// Extracted from ElementTreeViewManager (Gum.csproj, WinForms-coupled) so the (pure,
/// WinForms-free) icon-decision logic can be unit-tested directly and referenced from headless
/// code (ADR-0005 Phase 3). The ImageIndex constants live alongside it on
/// <see cref="TreeNodeImageIndices"/>, relocated off ElementTreeViewManager for the same reason.
/// </summary>
public class TreeNodeImageLogic
{
    /// <summary>
    /// Returns the per-type (blue-tinted) icon for an instance whose BaseType is a standard element.
    /// Falls back to the generic <see cref="InstanceImageIndex"/> for component-typed instances or
    /// unrecognized types.
    /// </summary>
    public int GetImageIndexForInstance(string? baseType) => baseType switch
    {
        "Container" => ContainerInstanceImageIndex,
        "Sprite" => SpriteInstanceImageIndex,
        "NineSlice" => NineSliceInstanceImageIndex,
        "Text" => TextInstanceImageIndex,
        "Rectangle" => RectangleInstanceImageIndex,
        "ColoredRectangle" => ColoredRectangleInstanceImageIndex,
        "Circle" => CircleInstanceImageIndex,
        "ColoredCircle" => ColoredCircleInstanceImageIndex,
        "RoundedRectangle" => RoundedRectangleInstanceImageIndex,
        "Polygon" => PolygonInstanceImageIndex,
        "Arc" => ArcInstanceImageIndex,
        "Line" => LineInstanceImageIndex,
        "Canvas" => CanvasInstanceImageIndex,
        "LottieAnimation" => LottieAnimationInstanceImageIndex,
        "Svg" => SvgInstanceImageIndex,
        _ => InstanceImageIndex,
    };

    /// <summary>
    /// Returns the per-type (purple-tinted) icon for a standard element by name, falling back to
    /// <see cref="StandardElementImageIndex"/> for unrecognized names.
    /// </summary>
    public int GetImageIndexForStandardElement(string? name) => name switch
    {
        "Container" => ContainerImageIndex,
        "Sprite" => SpriteImageIndex,
        "NineSlice" => NineSliceImageIndex,
        "Text" => TextImageIndex,
        "Rectangle" => RectangleImageIndex,
        "ColoredRectangle" => ColoredRectangleImageIndex,
        "Circle" => CircleImageIndex,
        "ColoredCircle" => ColoredCircleImageIndex,
        "RoundedRectangle" => RoundedRectangleImageIndex,
        "Polygon" => PolygonImageIndex,
        "Arc" => ArcImageIndex,
        "Line" => LineImageIndex,
        "Canvas" => CanvasImageIndex,
        "LottieAnimation" => LottieAnimationImageIndex,
        "Svg" => SvgImageIndex,
        _ => StandardElementImageIndex,
    };

    /// <summary>
    /// The icon for an existing element node being refreshed: an exclamation when the source file is
    /// missing or the element has errors, otherwise the per-type element icon.
    /// </summary>
    public int GetElementRefreshImageIndex(ElementSave element, bool hasErrors)
    {
        bool showExclamation = element.IsSourceFileMissing || hasErrors;
        int normalIndex = element is ScreenSave ? ScreenImageIndex
                        : element is ComponentSave ? ComponentImageIndex
                        : GetImageIndexForStandardElement(element.Name);
        return showExclamation ? ExclamationIndex : normalIndex;
    }

    /// <summary>
    /// The icon for a freshly-created element or behavior node: an exclamation when the source file is
    /// missing, otherwise the supplied default (type) icon.
    /// </summary>
    public int GetCreateImageIndex(bool isSourceFileMissing, int defaultImageIndex)
        => isSourceFileMissing ? ExclamationIndex : defaultImageIndex;

    /// <summary>
    /// The icon for a freshly-created instance node. A locked instance shows the lock icon; an instance
    /// whose base type is missing (and missing types are not tolerated) shows an exclamation.
    /// </summary>
    public int GetInstanceCreateImageIndex(InstanceSave instance, bool baseTypeValid, bool tolerateMissingTypes)
    {
        if (baseTypeValid || tolerateMissingTypes)
            return instance.Locked ? LockedInstanceImageIndex : GetImageIndexForInstance(instance.BaseType);
        else
            return ExclamationIndex;
    }

    /// <summary>
    /// The icon for an existing instance node being refreshed. Exclamation wins when the base element is
    /// missing or its source file is missing; otherwise a locked instance shows the lock icon, and
    /// everything else shows the per-type instance icon.
    /// </summary>
    public int GetInstanceRefreshImageIndex(InstanceSave instance, ElementSave? baseElement)
    {
        int desiredImageIndex = GetImageIndexForInstance(instance.BaseType);
        if (baseElement == null || baseElement.IsSourceFileMissing)
            desiredImageIndex = ExclamationIndex;
        else if (instance.Locked)
            desiredImageIndex = LockedInstanceImageIndex;
        return desiredImageIndex;
    }
}
