# SetBinding

## Introduction

SetBinding establishes a relationship between a property on the binding context and a property on the calling FrameworkElement. SetBinding is used to keep two properties synced, usually resulting changes on one of the properties automatically updating the other property.

SetBinding requires the bound FrameworkElement having a valid BindingContext - which can be either directly set or indirectly inherited from its parent.

The following are common SetBinding Examples:

* Binding a TextBox's Text property to a ViewModel's CharacterName property
* Binding a Button's IsEnabled property to a ViewModel's HasEnoughMoney property
* Binding a ListBox's Items property to a ViewModel's AvailableFastTravelDestinations property
* Binding a custom made PlayerJoinItem to a ViewModel's JoinState property

SetBinding takes two parameters:&#x20;

1. `uiProperty` - the property on the FrameworkElement&#x20;
2. `vmProperty` - the property on the ViewModel

Usually this method is called using the `nameof`  keyword to avoid errors from typos and refactoring.

{% hint style="info" %}
SetBinding exists on both FrameworkElement (Forms objects) as well as BindableGue (Gum runtime objects). This document is written in context of Forms controls, but binding can be performed directly on a runtime object's properties, including a FrameworkElement's Visual instance.
{% endhint %}

## Code Example - Binding a TextBox's Text Property

The following code shows how to bind a TextBox's Text property to a ViewModel's CharacterName property:

```csharp
// Assuming the TextBox's BindingContext is of type PlayerViewModel, and
// also assuming PlayerViewModel has a PlayerName property:
TextBoxInstance.SetBinding(
    nameof(TextBoxInstance.Text),
    nameof(PlayerViewModel.PlayerName));
```

## Binding to a ViewModel Event

Binding can be performed on ViewModel events. When a bound event is raised, the FrameworkElement's bound handler is raised. Binding to an event is similar to explicitly adding an event handler, but the SetBinding method enables binding to events without having a ViewModel instance.

For example, consider a ViewModel which contains an event called EnemySpawned, which would be an event raised whenever a new enemy is spawned in a game.&#x20;

```csharp
public class GameViewModel : ViewModel
{
    // Regular ViewModel events can be used in SetBinding
    public event Action EnemySpawned;
    
    // If the event needs to be raised externally (outside of the ViewModel)
    // then a method for raising the event is needed:
    public void RaiseEnemySpawned() => EnemySpawned?.Invoke();
}
```

FrameworkElements can subscribe to this event using the normal event subscribing syntax in C#, but doing so requires access to the ViewModel.

```csharp
var gameViewModel = this.BindingContext as GameViewModel;
if(gameViewModel != null)
{
    gameViewModel.EnemySpawned += HandleEnemySpawned;
}
// later, HandleEnemySpawned is declared:
void HandleEnemySpawned()
{
    // Perform logic related to enemy spawning
}
```

The code above is not as straight-forward as it might seem. BindingContext must be a valid `GameViewModel`, which means the code should be written in a place where the BindingContext is guaranteed to be set, such as overwriting the FrameworkElement's `HandleVisualBindingContextChanged` method. Furthermore, to prevent memory leaks a FrameworkElement should properly unsubscribe from its ViewModel events whenever its ViewModel changes.

We can simplify this code by letting the FrameworkElement handle this complexity by using SetBinding. The following code shows how this might look:

```csharp
void CustomInitialize()
{
    SetBinding(
        nameof(HandleEnemySpawned),
        nameof(GameViewModel.EnemySpawned));
}

void HandleEnemySpawned()
{
    // Perform logic related to enemy spawning
}
```

The code above uses the same syntax as binding properties, but in this case we are binding an event to a method on our framework element.
