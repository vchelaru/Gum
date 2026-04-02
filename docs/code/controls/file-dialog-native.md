# File Dialog (native)

## Introduction

Gum does not provide a file dialog, but recommends using NativeFileDialogNET. For information see the Github page: [https://github.com/atomsk-0/NativeFiledialogNET](https://github.com/atomsk-0/NativeFiledialogNET)

## Code Example

This code example assumes that your project has added the&#x20;

`NativeFileDialogNET` NuGet package.

```csharp
Label label = new Label();
label.Y = 100;
label.AddToRoot();

Button button = new Button();
button.AddToRoot();
button.Click += (_,_) =>
{
    using var selectFileDialog = new NativeFileDialog()
        .SelectFile()
        .AllowMultiple(); // Optionally allow multiple selections

    DialogResult result = selectFileDialog.Open(out string[]? output, 
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

    label.Text = result switch
    {
        DialogResult.Okay => $"Selected: {string.Join(", ", output ?? Array.Empty<string>())}",
        DialogResult.Cancel => "Selection canceled.",
        DialogResult.Error => "An error occurred while selecting a file.",
        _ => $"Unknown result {result}"
    };
};
```

