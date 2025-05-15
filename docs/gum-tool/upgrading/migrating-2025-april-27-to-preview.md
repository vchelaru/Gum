# Migrating 2025 April 27 to Preview

## TextRuntime Default WidthUnits and HeightUnits

This version changes the following:

<table data-full-width="false"><thead><tr><th width="240">Variable</th><th width="181">Old Value</th><th>New Value</th></tr></thead><tbody><tr><td>TextRuntime.Height</td><td>50</td><td>0</td></tr><tr><td>TextRuntime.HeightUnits</td><td>DimensionUnitType.Absolute</td><td>DimensionUnitType.RelativeToChildren</td></tr><tr><td>TextRuntime.Width</td><td>100</td><td>0</td></tr><tr><td>TextRuntime.WidthUnits</td><td>DimensionUnitType.Absolute</td><td>DimensionUnitType.RelativeToChildren</td></tr></tbody></table>

This change was made to address the confusion of TextRuntime instances wrapping at what seemed like arbitrary points.

The following code can be used to see the difference:

```csharp
var text = new TextRuntime();
text.Text = "I am displaying a long string without wrapping";
mainPanel.AddChild(text);
```

<figure><img src="../../.gitbook/assets/15_06 42 09.png" alt=""><figcaption><p>New default TextRuntime behavior</p></figcaption></figure>

<figure><img src="../../.gitbook/assets/image.png" alt=""><figcaption><p>Old default TextRuntime behavior</p></figcaption></figure>

Note that Height and HeightUnits have also changed so that TextRuntimes now automatically adjust their heights. This allows TextRuntimes to properly stack by default, as shown in the following code:

```csharp
for(int i = 0; i < 5; i++)
{
    var text = new TextRuntime();
    text.Text = "Text " + i;
    mainPanel.AddChild(text);
}
```

<figure><img src="../../.gitbook/assets/15_06 45 04.png" alt=""><figcaption><p>New default TextRuntime behavior in a stack</p></figcaption></figure>

<figure><img src="../../.gitbook/assets/15_06 46 52.png" alt=""><figcaption><p>Old default TextRuntime behavior in a stack</p></figcaption></figure>

This change only modifies the default variables, it does not change the behavior of TextRuntime if Height, HeightUnits, Width, and WidthUnits are explicitly set. In other words, the old behavior can be obtained by setting the values as shown in the followign code:

```csharp
var textWithOldBehavior = new TextRuntime();
textWithOldBehavior.Text = "Text with old behavior";
textWithOldBehavior.Width = 100;
textWithOldBehavior.WidthUnits = DimensionUnitType.RelativeToChildren;
textWithOldBehavior.Height = 50;
textWithOldBehavior.HeightUnits = DimensionUnitType.Absolute;
```
