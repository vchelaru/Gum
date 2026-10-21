# GumService (GumUI)

## Introduction

GumService provides access to common Gum objects and methods. It simplifies initialization, updating, and drawing the Gum system. Typically the Default instance is used in games, and a property can be created at class scope to reduce boilerplate code.

Many documentation pages create a property named GumUI as shown in the following code:

```csharp
GumService GumUI => GumService.Default;
```

## GumService and IGumService

`GumService` is the concrete service for your platform, and it is the most convenient choice for most games.

`IGumService` is the interface for code that should work on several runtimes, and for projects that expect to migrate between them. Both are supported.

## Initialize

`Initialize` is called in all Gum projects. This method performs the following:

* Internally creates all Gum systems
* Optionally loads a Gum project (.gumj)
* Specifies the version of code-only visuals to use

`Initialize` is typically called in the Game class or other top-level class depending on your current platform.

### Loading a Gum Project (.gumj/.gumx)

To load a Gum project, the following code can be used:

```csharp
// Initialize
GumUI.Initialize(this, "GumProject/GumProject.gumj");
```

This code assumes that the Gum project is located in the following path relative to your .csproj file:

```
Content/GumProject/GumProject.gumj
```

If the Gum project is located outside of the Content folder, it can be loaded using a number of techniques:

1.  Prefix the path with "../". For example the following code can be used to load a Gum project located in a resources folder:&#x20;

    ```csharp
    // Initialize
    GumUI.Initialize(this, "../resources/GumProject/GumProject.gumj");
    ```
2.  Set FileManager.RelativeDirectory prior to loading:

    ```csharp
    // Initialize
    FileManager.RelativeDirectory = "resources/";
    GumUI.Initialize(this, "GumProject/GumProject.gumj");
    ```
3.  Set an absolute path. Be careful specifying an absolute path as this may prevent your project from running on other machines.

    ```csharp
    // Initialize
    GumUI.Initialize(this, "c:/Gum/GumProject.gumj");
    ```

## More Than One GumService

Most games use one GumService. If you create a second one with `new GumService()`, `IGumService.Default` (and `GumService.Default`) is the service that code runs as. The first service to call `Initialize` becomes the default and a second `Initialize` leaves it alone. `Update` and `Draw` make their own service the default for the length of the call, so event handlers see the service that raised them.

Assign the default yourself before you build or change another service's elements from outside its `Update`, for example `IGumService.Default = editorService;`. Changing a font or a source file on an element finds its own service, so those do not need the assignment.

All services share one content cache. `LoaderManager.Self.DisposeAndClear()` unloads the content for every service, and a service's `Uninitialize` only does this when it is the last one left. If you installed your own `SynchronizationContext`, see [Async Programming](../../async-programming.md) for how to keep `await` continuations on the right service.
