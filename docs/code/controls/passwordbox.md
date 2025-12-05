# PasswordBox

## Introduction

The PasswordBox control is a TextBox-like control which can be used for entering passwords. It uses a `SecureString` rather than regular `string` and hides the entered characters by using the `*` key for each character typed. For more information see the SecureString documentation: [https://learn.microsoft.com/en-us/dotnet/api/system.security.securestring?view=net-8.0](https://learn.microsoft.com/en-us/dotnet/api/system.security.securestring?view=net-8.0)

PasswordBox does not support the CTRL+C copy hotkey.

## Code Example: Adding a PasswordBox

The following code adds a password box.

```csharp
var passwordBox = new PasswordBox();
passwordBox.AddToRoot();
passwordBox.X = 50;
passwordBox.Y = 50;
passwordBox.Width = 200;
passwordBox.Height = 34;
passwordBox.Placeholder = "Enter Password";

var button = new Button();
button.AddToRoot();
button.X = 50;
button.Y = 90;
button.Text = "Get Password";
button.Click += (_, _) => Debug.WriteLine(passwordBox.Password.ToString());
```

<figure><img src="../../.gitbook/assets/13_09 34 46.gif" alt=""><figcaption><p>Password entered in a PasswordBox</p></figcaption></figure>
