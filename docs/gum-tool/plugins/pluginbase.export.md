---
title: Export
---

# Export

## Introduction

The Export delegate allows you to create a custom export for Gum elements. This delegate will automatically be called by Gum whenever an element is saved. By default this occurs whenever the user makes any modification to the element. If your export is especially processor intensive you may consider not adding your export logic here and rather requiring an explicit export by the user.

## Code Example

The following code will show a message box whenever an element is exported. Keep in mind this is only for demonstration purposes. By default Gum auto-saves every change made by the user, and showing a message box after every save can be very annoying for users of your plugin.

```csharp
public override void StartUp()
{
    this.Export += HandleElementExport;
}

void HandleElementExport(Gum.DataTypes.ElementSave element)
{
    System.Windows.Forms.MessageBox.Show("Handling export of " + element);
}
```

## Accessing properties

The above example shows how to react to a component being exported, but it doesn't get at the heart of what an export plugin does.

The first thing that an export plugin needs to do is to access properties on an element (such as its position and size) as well as the instances contained within the element. This information is available through the ElementSave class.

For more information on how to access properties and instances from the ElementSave, see the [Gum Class Overview](/broken/pages/-MiveTqhTrAT7yL-hNWT) page.

## Example Export Code

The following shows what a very simple exporter might look like:

```csharp
void HandleElementExport(Gum.DataTypes.ElementSave element)
{

    StringBuilder stringBuilder = new StringBuilder();

    foreach(var instance in element.Instances)
    {
        stringBuilder.AppendFormat("{0} {1} = new {0}();", 
             instance.BaseType, instance.Name);   
    }

    stringBuilder.AppendLine();

    // We'll just use the default state for this example:
    foreach(var variable in element.DefaultState.Variables)
    {
        if(variable.Value != null && variable.SetsValue)
        {
           // You may want to process the Value.  For example,
           // if it's a float, the string representation of the 
           // value might be "1.23", but you may want to convert that to
           // "1.23f"
           // Similarly strings may need to be wrapped in quotes, and values like
           // coordinate types may need to be qualified as enumerations or
           // translated into whatever system the given engine uses.
           string rightSideOfEquals = variable.Value.ToString();

           stringBuilder.AppendFormat("{0} = {1};", 
                  variable.Name, rightSideOfEquals);
        }
    }

    string textToSave = stringBuilder.ToString();

    // Now the textToSave would get saved to disk wherever you want it exported
}
```

This may produce output that looks like this:

```csharp
Sprite SpriteInstance1 = new Sprite();
Sprite SpriteInstance2 = new Sprite();
Text TextInstance1 = new Text();

SpriteInstance1.X = 3;
SpriteInstance1.Y = 4;
SpriteInstance2.Width = 10;
Text.X = 3;
```
