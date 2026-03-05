# Migrating to 2026 March

## Introduction

This page discusses breaking changes and other considerations when migrating from `2026 February` to `2026 March` .

The March version of Gum is currently only available in source code and has not been released as binary/NuGet files yet.

### \[Breaking] Blend Property is Now Nullable

The `Blend` property on runtime objects now returns `Blend?` (nullable) instead of `Blend`. Previously, if a runtime object had a custom `BlendState` assigned that did not correspond to any known `Blend` enum value, the `Blend` getter would silently return `Blend.Normal`. This was incorrect — `Blend.Normal` is a meaningful value (NonPremultiplied blending), and returning it for an unrecognized state made it impossible to distinguish between "this object uses Normal blending" and "this object has an unknown blend state."

The following runtime objects are affected:

* `SpriteRuntime`
* `TextRuntime`
* `NineSliceRuntime`
* `ColoredRectangleRuntime`
* `ContainerRuntime`

In practice, a `null` return value only occurs when a custom `BlendState` that does not map to a known `Blend` enum value has been programmatically assigned to a renderable. Under all standard usage — including the Gum tool, state application, and the built-in `BlendState` constants — the `Blend` getter will continue to return a non-null value.

#### How to Update

If your code reads the `Blend` property and assigns it to a non-nullable variable, you will receive a compiler error or warning. The recommended fix depends on how you use the value.

If you want to preserve the previous fallback behavior of defaulting to `Blend.Normal` for unknown states, use the null-coalescing operator:

❌ Old:

```csharp
Gum.RenderingLibrary.Blend blend = mySprite.Blend;
```

✅ New:

```csharp
Gum.RenderingLibrary.Blend blend = mySprite.Blend ?? Gum.RenderingLibrary.Blend.Normal;
```

If you want to handle the unknown state explicitly:

```csharp
if (mySprite.Blend is Gum.RenderingLibrary.Blend blend)
{
    // blend is a known value
}
else
{
    // BlendState does not correspond to a known Blend enum value
}
```

Code that only writes to the `Blend` property (setter) is not affected by this change.
