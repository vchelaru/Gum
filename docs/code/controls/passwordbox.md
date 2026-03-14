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
button.Click += (_, _) => button.Text = passwordBox.Password.ToString();
```

[Try on XnaFiddle.NET](https://xnafiddle.net/#snippet=H4sIAAAAAAAAA22PXwuCMBTFv8rYU0GIaD1k-JAh1VuUUIEgq40c2RbzWlH03Zu5_mlvO-d37-45NzzNx8UBe6AK1sFccOAk41eGPXwiCh1Jnp-looG8IB8Jdkazj9NqD2LxNWENKY3kXEpokJXe7tk1c_3PXHIKqQaOXScTxncpaOR2a2SWkS1LZUaZ0jgubNtxQgFavdJWnl4rS20KAClMn-ApnoErv9bCmO8CRpfZ-186Yhd43x4zaF42g6OMb_fVXOCjVtJBSRv5pXZD9PvZT0XztiK5AMXFTofD9wcMCTaCvwEAAA)

<figure><img src="../../.gitbook/assets/13_22 15 31.gif" alt=""><figcaption><p>Password entered in a PasswordBox</p></figcaption></figure>
