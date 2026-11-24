# ProgressBar

## Introduction

The ProgressBar control shows how far along a task is. Its indicator fills the track according to `Value`. It does not respond to input.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

## Code Example: Creating a ProgressBar

The following code creates a ProgressBar and fills it as a task reports progress:

```csharp
// Initialize
var progressBar = new ProgressBar();
progressBar.AddToRoot();
progressBar.X = 50;
progressBar.Y = 50;
progressBar.Width = 250;

// Later, as the task progresses:
progressBar.Value = 35;
```

## Value, Minimum, and Maximum

`Minimum` defaults to 0 and `Maximum` defaults to 100. `Value` is always between Minimum and Maximum, inclusive. Setting a value outside of its bounds forces the value to the bounds.

The indicator's width is the portion of the range that `Value` covers, so the following code fills a quarter of the track:

```csharp
// Initialize
progressBar.Minimum = 50;
progressBar.Maximum = 150;
progressBar.Value = 75;
```

`ValueChanged` is raised whenever `Value` changes.

## IsIndeterminate

Set `IsIndeterminate` to `true` when the amount of progress is unknown. The indicator then moves across the track repeatedly instead of showing `Value`. Setting it back to `false` shows `Value` again.

```csharp
// Initialize
progressBar.IsIndeterminate = true;
```

The indicator only moves while the ProgressBar is part of the root that `GumUI.Update` animates, such as after `AddToRoot`.

## Custom Visuals

A custom visual needs a child named `IndicatorInstance`. The ProgressBar sets its `Width` as a percentage of its parent, so place it inside the element that acts as the track, usually named `TrackInstance`.

`IsIndeterminate` sets the `ProgressBarCategoryState` state to `Indeterminate` or `Determinate`. The visual decides how the Indeterminate state looks and moves. While indeterminate, the ProgressBar does not change the indicator's width.
