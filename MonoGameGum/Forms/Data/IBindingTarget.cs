using System;
using System.ComponentModel;
using Gum.Wireframe;

#if FRB
namespace FlatRedBall.Forms.Data;
#endif

#if !FRB
namespace Gum.Forms.Data;
#endif

/// <summary>
/// The element a binding writes to: a Forms control, or (outside FRB) a visual. Bindings read the target's
/// properties by reflection on its runtime type and follow its BindingContext through these events.
/// </summary>
internal interface IBindingTarget : INotifyPropertyChanged
{
    /// <summary>The object the target's bindings read from, its own or inherited from its parent.</summary>
    object? BindingContext { get; }

    /// <summary>Raised when the effective <see cref="BindingContext"/> changes.</summary>
    event EventHandler<BindingContextChangedEventArgs>? BindingContextChanged;

    /// <summary>Raised when the context the target would inherit from its parent changes.</summary>
    event EventHandler<BindingContextChangedEventArgs>? InheritedBindingContextChanged;

    /// <summary>
    /// The context the target would inherit from its parent. A binding of <see cref="BindingContext"/> reads its
    /// path from it.
    /// </summary>
    object? InheritedBindingContext { get; }

    /// <summary>
    /// Set while a binding sets <see cref="BindingContext"/>. The value it resolves, null included, is then the
    /// target's context, as WPF does for a bound DataContext; once the binding is cleared, the target inherits again.
    /// </summary>
    void SetIsBindingContextBound(bool isBound);

    /// <summary>Raised when the target loses focus, for <see cref="UpdateSourceTrigger.LostFocus"/>.</summary>
    event EventHandler? LostFocus;

    /// <summary>
    /// The mode of a binding whose <see cref="Binding.Mode"/> is not set. A Forms control is TwoWay, since the
    /// user edits it. A visual is OneWay, since its property changes come from states, SetProperty and
    /// localization rather than user input; it writes to the source only through PushValueToViewModel.
    /// </summary>
    BindingMode DefaultBindingMode { get; }
}
