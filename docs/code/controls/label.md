# Label

## Introduction

Label provides a way to display a string to the user. Unlike TextBox, Labels cannot be changed by typing on the keyboard.

## Code Example: Adding a Label

The following code adds a Label instance to the root:

```csharp
// Initialize
var label = new Label();
label.AddToRoot();
label.Text = "This is a Gum label";
```

<figure><img src="../../.gitbook/assets/13_09 02 44.png" alt=""><figcaption><p>Label in Gum</p></figcaption></figure>

## Text Wrapping

By default a Label sizes itself to fit its text content, so text does not wrap. To enable wrapping, give the Label a fixed width by setting its `WidthUnits` to `Absolute` and specifying a `Width` value. The height can remain `RelativeToChildren` so the Label grows vertically as needed.

```csharp
// Initialize
var label = new Label();
label.AddToRoot();
label.Text = "This is a long string of text that will wrap to multiple lines when the label has a fixed width.";
label.Visual.Width = 200;
label.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
label.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACo2PQUsEMQyF7wPzH0JPClIWjy4eFhdU8LSMeplLlsluA5l2mGZ2VsX_bjoHEfEgFNJ-7_Ul-agrAPeY76fe3YCOE10thCMro_A7GXYnHEFwTwK3EGmGp3K_uFy3caF-03VN2qWkP1hDZzV765rAGewgSIpHyDqylXQALQ4NqDCzCMwjDqAJ-kmUByEQjpRhDhTNZc9lgIAl6cBn6uxbp8G37rvnC-cJxb8Wbr2vV6s_pWdbLptuS_stKjZvA2W_5Z5i5hSLXJDf7HOSSel3yAPxMeh_U3YkqHyiJt0Flm6kuHZ19VlXX-3xEnx8AQAA" target="_blank">Try on XnaFiddle.NET</a>

## Text Alignment

The horizontal and vertical alignment of the text within the Label's bounding box is controlled through the `HorizontalAlignment` and `VerticalAlignment` properties on the `TextComponent`. Cast `TextComponent` to `TextRuntime` to access these properties.

```csharp
// Initialize
var label = new Label();
label.AddToRoot();
label.Text = "Centered text";
label.Visual.Width = 300;
label.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
label.Visual.Height = 100;
label.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
var textRuntime = (MonoGameGum.GueDeriving.TextRuntime)label.TextComponent;
textRuntime.HorizontalAlignment = RenderingLibrary.Graphics.HorizontalAlignment.Center;
textRuntime.VerticalAlignment = RenderingLibrary.Graphics.VerticalAlignment.Center;
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACp2QP2vDMBTEd4O_g_CUQBEp3Vo6mBicQroYN128KPHDfiA_GUlOm5R-9z45_ROSDKGbuDv9dLqPOBIieXL50CX3wtsBbkYFCT0qjXtgOdkqK7RagxaPguBNLMN5Mn2oaFRlWtelKYzxR1oJ757jVTIH8mChFp6VKvkNrNANSstXrH3LwbvZ7KL1wk0c-9xQZsqrcteDkxl2QA4NBTtIMl07owcPp5AFYNOGJrfnDxy8f7wQBgnfKQbynOPLk2dDJlcdBEo-QAYWt0jNuMN3bPq3zNx0vSFehmFHILkwFveGvNKpxoa4QqheANXMo2aJa6vsTuZW9S1u3KW8PAx-Al6B9bi5FnuW_oEmcfQZR1--d9KRNgIAAA" target="_blank">Try on XnaFiddle.NET</a>

## Auto-Sizing to Text Content

By default a Label's `WidthUnits` and `HeightUnits` are set to `RelativeToChildren`, which means the Label automatically sizes to match its text. When you change `Text`, the Label grows or shrinks to fit the new content with no additional code required.

```csharp
// Initialize
var label = new Label();
label.AddToRoot();
// By default a Label sizes to its text content
label.Text = "Short text";
// Assign longer text and the label grows automatically
label.Text = "This is a much longer string of text";
```
<a href="https://xnafiddle.net/#snippet=H4sIAAAAAAAACmWOwUrEQAyG74W-w8-cFMS9K3tYL7LgSXvsJbbZNjBNYCbjuorvbre1JyGQ8PHnS77rCgjH_Fym8ABPhe8WIiouFOWLZxw-KCHSO0fsoXzGy3W-uX1sdaH3h75v7NXMF7bb4emCnk9UooPWNPLsynCD-Nz409GZOqtvkubK9mjD22jJl0gbVt0hZxkU0XTgtC6T9vCR_94akp0zqLhN5NJRjJd_2maUjLkIU-nGTZY9iQ6w03Yw1NVPXf0CT0_REBgBAAA" target="_blank">Try on XnaFiddle.NET</a>

To constrain a Label to a fixed size and prevent it from growing with its content, set `WidthUnits` to `Absolute` and assign an explicit `Width`.
