# File Dialog (native)

## Introduction

Gum does not provide a file dialog, but recommends using NativeFileDialogCore. For information see the Github page: [https://github.com/lofcz/NativeFileDialogCore](https://github.com/lofcz/NativeFileDialogCore)

## Code Example

This code example assumes that your project has added the&#x20;

`NativeFileDialogCore` NuGet package.

```csharp
Label label = new Label();
label.Y = 100;
label.AddToRoot();

Button button = new Button();
button.AddToRoot();
button.Click += (_,_) =>
{
    var result = Dialog.FileOpen();
    if (result.IsOk)
    {
        label.Text = result.Path;
    }
};
```

