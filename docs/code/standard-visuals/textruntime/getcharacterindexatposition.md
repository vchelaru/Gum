# GetCharacterIndexAtPosition

## Introduction

The `GetCharacterIndexAtPosition` method returns the character index under the cursor.

The returned index is the index into a string obtained by combining all strings in `WrappedText`, so additional logic is needed to find the character if the text is not all displayed in one line.

This method can also be used on text which includes BBCode, returning an index after BBCode has been removed.

## Code Example: Printing Character at Position

The following code shows how to use the Gum cursor to hover over a TextRuntime and display the character under the cursor.

```csharp
TextRuntime textRuntime;

protected override void Initialize()
{
    GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.Newest);

    textRuntime = new TextRuntime();
    textRuntime.AddToRoot();

    textRuntime.Text = 
        "I am a text runtime which should line wrap because my " +
        "width is set to an absolute 200 units";

    textRuntime.Width = 200;
    textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;

    textRuntime.Anchor(Anchor.Center);

    textRuntime.HorizontalAlignment = HorizontalAlignment.Left;
    textRuntime.VerticalAlignment = VerticalAlignment.Top;

    base.Initialize();
}


protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    var cursor = GumUI.Cursor;
    var x = cursor.XRespectingGumZoomAndBounds();
    var y = cursor.YRespectingGumZoomAndBounds();

    if(textRuntime.HasCursorOver(GumUI.Cursor))
    {
        var innerText = (RenderingLibrary.Graphics.Text)textRuntime.RenderableComponent;

        int characterIndex = innerText.GetCharacterIndexAtPosition(x, y);

        if(characterIndex != -1)
        {
            int indexLeft = characterIndex;

            // We need to loop through the wrapped text to see which line we're on:
            foreach(var line in textRuntime.WrappedText)
            {
                if(line.Length > indexLeft)
                {
                    Debug.WriteLine($"At character {characterIndex}");
                    Debug.WriteLine($"Character is {line[indexLeft]}");
                    break;
                }
                else
                {
                    indexLeft -= line.Length;
                }
            }
        }
    }
    base.Update(gameTime);
}
```

<figure><img src="../../../.gitbook/assets/25_07 13 42.gif" alt=""><figcaption><p>Cursor hovering over characters, and the character is printed out to the debug window</p></figcaption></figure>
