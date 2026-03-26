# HorizontalAlignment

## Introduction

HorizontalAlignment controls the placement of the letters inside the Text's bounds. To align a TextRuntime in its parent, its XOrigin and XUnits may need to be modified as well to adjust its bounds.

## Example - Right-Alignment in a Parent

The following code shows how to right-align a TextRuntime in its parent:

```csharp
// Initialize
var leftAligned = new TextRuntime();
leftAligned.AddToRoot();
leftAligned.Text = "Hi, I am some text that is left aligned, even if I line wrap";
leftAligned.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Left;
leftAligned.Width = 100;
leftAligned.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
leftAligned.X = 50;
leftAligned.Y = 100;


var centerAligned = new TextRuntime();
centerAligned.AddToRoot();
centerAligned.Text = "Hi, I am some text that is center aligned, even if I line wrap";
centerAligned.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
centerAligned.Width = 100;
centerAligned.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
centerAligned.X = 250;
centerAligned.Y = 100;


var rightAligned = new TextRuntime();
rightAligned.AddToRoot();
rightAligned.Text = "Hi, I am some text that is right aligned, even if I line wrap";
rightAligned.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Right;
rightAligned.Width = 100;
rightAligned.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
rightAligned.X = 450;
rightAligned.Y = 100;
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA62TUWvCMBDHv0rI0waldDJfHHuQCSr4VBxuzD1Ee9qDNpHkqtOx776ksmFS2Yr4ev-73zX3o598bIZVyXukK4h4ZVCuDe-9cVuMZ6hhpUUJ_D3iKJFQFHgA3uNboVkBK-oXuJaQsUcmYcem8EFpJQlLuLl9mMuTjrifZVOVKkWNxE1ZwLxKkk5nhBEbM1Eyo0pg5CLKBTE09T4mjkMRgy1IhivbXKAEttNicyQE9JHSeFCSRFFXSpBuWQoyA23fOsGFFnofD-18jktzrj-eWF6AnWFGuQXdJcm55Nkey9jYXXEgSEz3GzDxwB5GGlTSxa4U9xdGFRVBwHixo90Q_Pq7zl1_aT8M9N_393oCA37W1sFxqp0Ff8M1PDzVxAbad3Emu8CGT3E-Ot0m3VeicZ3_80ectgRCvKitj3qonQ6Pfw0bqQOGYN9FM7pAhQdxJu67DfaPCP71DSBXFAHRBAAA)

<figure><img src="../../../.gitbook/assets/26_11 27 16.png" alt=""><figcaption></figcaption></figure>



