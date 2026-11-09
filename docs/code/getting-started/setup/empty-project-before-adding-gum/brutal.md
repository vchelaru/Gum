# BRUTAL

## Introduction

Gum can be used in [BRUTAL](https://github.com/ahwootech/BRUTAL) projects by importing Gum's `Gum.Brutal` NuGet package. `Gum.Brutal` renders through SkiaSharp and reads input from the GLFW window that BRUTAL's `Ahwoo.Brutal.Glfw` package creates. BRUTAL ships Vulkan, OpenGL and window bindings but no renderer, so your game owns the window, the graphics device or context and the code that puts the frame on screen.

## Creating a New Project

{% hint style="info" %}
To skip these steps, create the project in the Gum tool: select **File** > **New Project**, check **Create C# project**, and pick BRUTAL as the platform. The project already references Gum and loads a Gum project. See [Creating a Project](../../../../gum-tool/setup/creating-a-project.md).
{% endhint %}

Before using Gum, you should first verify that you can create a normal BRUTAL project that opens a window and creates a Vulkan device or an OpenGL context. For the full API reference, see the BRUTAL repository: [https://github.com/ahwootech/BRUTAL](https://github.com/ahwootech/BRUTAL)

{% tabs %}
{% tab title="Visual Studio" %}
First, create an empty console project

1. Open Visual Studio
2. Select File -> New Project, or select the **Create a new project** option in the popup window.
3. Select the option to create a **Console App**
4. Enter a name, select a location, then click **Next**
5. Select **.NET 10.0** as the **Framework**, then click **Create**

Next, add the needed NuGet packages:

1. Expand your game project in the Solution Explorer
2. Right-click on **Dependencies** and select **Manage NuGet Packages**
3. Search for and install `Ahwoo.Brutal.Glfw`
4. For Vulkan, also install `Ahwoo.Brutal.Vulkan` and `Ahwoo.Brutal.Vulkan.Abstractions`. For OpenGL, also install `Ahwoo.Brutal.OpenGl`
{% endtab %}
{% endtabs %}

BRUTAL's packages ship `net10.0` builds only, so your project must target `net10.0`. BRUTAL runs on Windows and Linux and needs a Vulkan 1.3 driver, or an OpenGL 3.3 driver if you use OpenGL.

Next, you can begin adding Gum to your project. For more information see the [Adding/Initializing Gum](../adding-initializing-gum/brutal.md) page.
