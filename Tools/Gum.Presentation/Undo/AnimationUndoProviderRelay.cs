using Gum.DataTypes;
using Gum.StateAnimation.SaveClasses;

namespace Gum.Undo;

/// <summary>
/// Late-bound bridge between the headless undo strategy and the animation plugin. <see cref="UndoManager"/>
/// and its <see cref="ElementUndoStrategy"/> are constructed by DI at startup, before any plugin exists,
/// so they receive this relay as their <see cref="IAnimationUndoProvider"/>. The animation plugin then
/// registers itself as the real provider in its StartUp via <see cref="IAnimationUndoProviderRegistrar"/>.
/// Until a provider is registered (e.g. in headless tests or before the plugin loads) every call is a
/// no-op returning null, which the strategy treats as "this element has no animations".
/// </summary>
public class AnimationUndoProviderRelay : IAnimationUndoProvider, IAnimationUndoProviderRegistrar
{
    private IAnimationUndoProvider? _provider;

    public void Register(IAnimationUndoProvider provider) => _provider = provider;

    public ElementAnimationsSave? GetCurrentAnimations(ElementSave element)
        => _provider?.GetCurrentAnimations(element);

    public void ApplyAnimations(ElementSave element, ElementAnimationsSave animations)
        => _provider?.ApplyAnimations(element, animations);
}
