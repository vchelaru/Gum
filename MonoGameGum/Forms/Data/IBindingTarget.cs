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

    /// <summary>Raised when the target loses focus, for <see cref="UpdateSourceTrigger.LostFocus"/>.</summary>
    event EventHandler? LostFocus;
}
