# PasswordBox

## Introduction

The PasswordBox control is a TextBox-like control which can be used for entering passwords. It uses a `SecureString` rather than regular `string` and hides the entered characters by using the `*` key for each character typed. For more information see the SecureString documentation: [https://learn.microsoft.com/en-us/dotnet/api/system.security.securestring?view=net-8.0](https://learn.microsoft.com/en-us/dotnet/api/system.security.securestring?view=net-8.0)

PasswordBox does not support the CTRL+C copy hotkey.

## Code Example: Adding a PasswordBox

The following code adds a password box.&#x20;

```csharp
var passwordBox = new PasswordBox();
this.Root.Children.Add(passwordBox.Visual);
passwordBox.X = 50;
passwordBox.Y = 50;
passwordBox.Width = 200;
passwordBox.Height = 34;
passwordBox.Placeholder = "Enter Password";

var button = new Button();
this.Root.Children.Add(button.Visual);
button.X = 50;
button.Y = 90;
button.Text = "Get Password";
button.Click += (_, _) => Debug.WriteLine(passwordBox.Password.ToString());
```

<figure><img src="../../../../.gitbook/assets/24_06 59 02.gif" alt=""><figcaption><p>Entering a password in the PasswordBux and obtaining it through the Password property</p></figcaption></figure>
