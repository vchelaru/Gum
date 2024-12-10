# MonoGameGumFormsSample

### Introduction

The MonoGameGumFormsSample includes a number of pages showing different features related to using Gum Forms.

<figure><img src="../../.gitbook/assets/27_12 03 49.gif" alt=""><figcaption><p>MonoGameGumFormsSample showing the FromFileDemoScreen</p></figcaption></figure>

The screens can be switched by modifying the `screenNumber` variable in the Initialize code:

```csharp
protected override void Initialize()
{
    SystemManagers.Default = new SystemManagers();
    SystemManagers.Default.Initialize(_graphics.GraphicsDevice, fullInstantiation: true);
    FormsUtilities.InitializeDefaults();

    const int screenNumber = 0;

    switch (screenNumber)
    {
        case 0:
            InitializeFromFileDemoScreen();
            break;
        case 1:
            InitializeFrameworkElementExampleScreen();
            break;
        case 2:
            InitializeFormsCustomizationScreen();
            break;
    }

    base.Initialize();
}
```

### ComplexListBoxItemScreen

The ComplexListBoxItemScreen shows how to create a ListBoxItem which is fully customizable. This page uses a ListBoxItem which has the following characteristics:

* The layout is defined in the Gum tool rather than in code
* Each item displays multiple pieces of information with multiple Text instances rather than a single Text instance
* Each item displays information from a complex view model with multiple properties

<figure><img src="../../.gitbook/assets/image (2) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>ListBoxItems displaying complex information about weapons</p></figcaption></figure>
