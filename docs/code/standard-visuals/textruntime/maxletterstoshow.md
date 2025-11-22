# MaxLettersToShow

## Introduction

`MaxLettersToShow` controls the number of letters that are displayed by a `TextRuntime`. This value can be used to print text out letter-by-letter. This property does not change alignment or size of text. In other words, a TextRuntime with its `WidthUnits` or `HeightUnits` of `RelativeToChildren` reports the same absolute width regardless of its `MaxLettersToShow` value.

## Code Example - Printing Letters One at a Time

The following code can be used to print letters out one at a time.

```csharp
TextRuntime textRuntime;
bool isPrinting = false;
float numberOfCharacters;
protected override void Initialize()
{
    GumUI.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);

    textRuntime = new TextRuntime();
    textRuntime.AddToRoot();
    textRuntime.Text = "This is longer text which is used to " +
        "create word wrapping. Notice that when this prints out, " +
        "it prints one letter at a time and wraps according to the " +
        "TextRuntime's bounds";

    textRuntime.Anchor(Anchor.Center);
    textRuntime.Width = 200;
    textRuntime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
    textRuntime.Height = 0;
    textRuntime.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
    textRuntime.MaxLettersToShow = 0;

    base.Initialize();
}


protected override void Update(GameTime gameTime)
{
    GumUI.Update(gameTime);

    if(GumUI.Keyboard.KeyPushed(Keys.Space))
    {
        isPrinting = true;
        numberOfCharacters = 0;
    }

    if(isPrinting)
    {
        const int charactersPerSecond = 20;
        numberOfCharacters += 
            (float)gameTime.ElapsedGameTime.TotalSeconds * charactersPerSecond;

        textRuntime.MaxLettersToShow = (int)numberOfCharacters;
    }

    base.Update(gameTime);
}

```

<figure><img src="../../../.gitbook/assets/08_13 28 26.gif" alt=""><figcaption></figcaption></figure>

