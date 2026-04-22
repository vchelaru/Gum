# PasswordBox

## Introduction

The PasswordBox control is a TextBox-like control which can be used for entering passwords. It uses a `SecureString` rather than regular `string` and hides the entered characters by using the `*` key for each character typed. For more information see the SecureString documentation: [https://learn.microsoft.com/en-us/dotnet/api/system.security.securestring?view=net-8.0](https://learn.microsoft.com/en-us/dotnet/api/system.security.securestring?view=net-8.0)

PasswordBox does not support the CTRL+C copy hotkey.

## Code Example: Adding a PasswordBox

The following code adds a password box.

```csharp
// Initialize
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
button.Click += (_, _) => button.Text = passwordBox.Password;
```
[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAACm2PTQvCMAyG7wP_Q-hpooj4cVCZoCLTm8hAhcKYtrjibGXLVBT_u92sX9Nb8rwJeXItWQBkmrjpnnQB45RXcyKkQBFE4sI1JscghkOQJCcVs6E6gwOSn2D2Jna5R-XHRG3AmKfmSuFPstTb7XoBrv7BhWAY6qBRLyYTLrYh6qjZKiSzKNjwUEWMxzqmZCxRV09TSvQ4ldk76xRRSfPJMG9y1Qcv-Bv4Ujd9Zt356D1-zqQocTl-3zQDo0hsdlBxwPar4JfB6RuP5-rXK6bukZJ1uwOuo6mUqAEAAA)

## PasswordChar

The `PasswordChar` property can be used to customize the character that masks the actual text entered by the user. By default, this is an asterisk (`*`).

The following changes the `PasswordChar`:

```csharp
// Initialize
var passwordBox = new PasswordBox();
passwordBox.PasswordChar = '#';
passwordBox.AddToRoot();
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA6tW8ix2L81VsiopKk3VUSotzsxLL1ayilYCCuqFZxalphUl5qYqxeooZeZllmQm5mRWpSpZKZUlFikUJBYXl-cXpTjlVyjYKuSllisEIEQ0NK1j8pBU6MHknDOAWm0VYkoNDIzMlSEUmlLHlJSQ_KD8_BKgIUq1ADyYlTeiAAAA)

<figure><img src="../../.gitbook/assets/13_22 15 31.gif" alt=""><figcaption><p>Password entered in a PasswordBox</p></figcaption></figure>
