# Samples

### Introduction

The Gum repository contains multiple samples on a variety of Gum topics. This page discusses these samples, covering how to get them running on your machine and how to modify them to see different features in action.

## Cloning Gum Repository

Gum samples are located in the Gum repository. The GitHub repository can be found here:

{% embed url="https://github.com/vchelaru/Gum" %}

### Opening Samples

Once you have cloned the repository, navigate to the \<Gum Root>/Samples folder.

<figure><img src="../.gitbook/assets/image (81).png" alt=""><figcaption><p>Samples folder in &#x3C;Gum Root>/Samples</p></figcaption></figure>

Notice that multiple samples exist, each in their own folder. Each folder has a .sln file (some contain multiple) which can be opened to view the sample. For example, the sample showing how to work with Gum Forms is located at `<Gum Root>/Samples/GumFormsSample/MonoGameGumFormsSample.sln` .

The Samples folder is continually changing, and new samples are being added over time. The remainder of this page covers some of the most common samples.

### MonoGameGumFormsSample

The GumFormsSample/MonoGameGumFormsSample.sln solution shows how to work with Gum Forms.

<figure><img src="../.gitbook/assets/27_12 03 49.gif" alt=""><figcaption><p>MonoGameGumFormsSample showing the FromFileDemoScreen</p></figcaption></figure>

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
