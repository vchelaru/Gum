# GumService (GumUI)

## Introduction

GumService provides access to common Gum objects and methods. It simplifies initialization, updating, and drawing the Gum system. Typically the Default instance is used in games, and a property can be created at class scope to reduce boilerplate code.

Many documentation pages create a property named GumUI as shown in the following code:

```csharp
GumService GumUI => GumService.Default;
```

## Initialize

`Initialize` is called in all Gum projects. This method performs the following:

* Internally creates all Gum systems
* Optionally loads a Gum project (.gumx)
* Specifies the version of code-only visuals to use

`Initialize` is typically called in the Game class or other top-level class depending on your current platform.

### Loading a Gum Project (.gumx)

To load a Gum project, the following code can be used:

```csharp
GumUI.Initialize(this, "GumProject/GumProject.gumx");
```

This code assumes that the Gum project is located in the following path relative to your .csproj file:

```
Content/GumProject/GumProject.gumx
```

If the Gum project is located outside of the Content folder, it can be loaded using a number of techniques:

1.  Prefix the path with "../". For example the following code can be used to load a Gum project located in a resources folder:&#x20;

    ```csharp
    GumUI.Initialize(this, "../resources/GumProject/GumProject.gumx");
    ```
2.  Set FileManager.RelativeDirectory prior to loading:

    ```csharp
    FileManager.RelativeDirectory = "resources/";
    GumUI.Initialize(this, "GumProject/GumProject.gumx");
    ```
3.  Set an absolute path. Be careful specifying an absolute path as this may prevent your project from running on other machines.

    ```csharp
    GumUI.Initialize(this, "c:/Gum/GumProject.gumx");
    ```
