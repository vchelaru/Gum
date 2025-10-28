# Linking Game Project to Source (Optional)

### Introduction

The easiest way to use Gum is to add the NuGet packages to your game project. Alternatively you can link your game to Gum source for additional debugging, to stay up to date with the latest improvements, or if you are interested in contributing.

This document assumes you already have a game project with the Gum NuGet packages linked.

{% hint style="info" %}
As of July 22, 2025 the following two libraries have been merged into GumCore:

* ToolsUtilitiesStandard
* GumDataTypesNet6

These two projects are no longer needed. Old projects can safely remove these and only reference GumCore and the platform-specific runtime library such as MonoGameGum.
{% endhint %}

### Linking Source

{% tabs %}
{% tab title="MonoGame" %}
If you have followed the Setup steps, then you should have a game which references the Gum NuGet package.

<figure><img src="../../../.gitbook/assets/14_21 53 07.png" alt=""><figcaption><p>Default setup referencing NuGet Package</p></figcaption></figure>

To replace this package with source references:

1. Clone the Gum repository [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)
2. Remove the NuGet package by selecting your Gum.MonoGame and pressing the Delete key.
3. Right-click on your Solution in the Solution Explorer and select **Add** -> **Existing Project...**
4. Select \<Gum Root>/MonoGameGum/MonoGameGum.csproj
5.  Repeat the previous step for the following csproj file: \<Gum Root>/GumCommon/GumCommon.csproj

    <figure><img src="../../../.gitbook/assets/22_20 38 19.png" alt=""><figcaption><p>A game project and Gum source code projects in the Visual Studio Solution Explorer</p></figcaption></figure>
6. Right-click on your game project's Dependencies folder and select Add Project Reference...
7.  Check the Gum projects you added to the solution earlier and click OK\


    <figure><img src="../../../.gitbook/assets/22_20 39 50.png" alt=""><figcaption><p>Check the Gum projects then click OK</p></figcaption></figure>
{% endtab %}

{% tab title="raylib" %}
If you have followed the Setup steps, then you should have a game which references the Gum NuGet package.

<figure><img src="../../../.gitbook/assets/18_09 41 19.png" alt=""><figcaption><p>Default setup referencing NuGet Package</p></figcaption></figure>

To replace this package with source references:

1. Clone the Gum repository [https://github.com/vchelaru/Gum](https://github.com/vchelaru/Gum)
2. Remove the NuGet package by selecting your Gum.raylib and pressing the Delete key. If you are using VSCode, you can also edit the .csproj to remove this package reference
3. Right-click on your Solution in the Solution Explorer and select **Add** -> **Existing Project...**
4. Select \<Gum Root>/Runtimes/RaylibGum/RaylibGum.csproj
5.  Repeat the previous step for the following csproj file: \<Gum Root>/GumCommon/GumCommon.csproj\


    <figure><img src="../../../.gitbook/assets/18_09 43 25.png" alt=""><figcaption><p>A game project and Gum source code projects in the Visual Studio Solution Explorer</p></figcaption></figure>
6. Right-click on your game project's Dependencies folder and select Add Project Reference...
7.  Check the Gum projects you added to the solution earlier and click OK\


    <figure><img src="../../../.gitbook/assets/18_09 45 05.png" alt=""><figcaption><p>Check the Gum projects then click OK</p></figcaption></figure>
{% endtab %}
{% endtabs %}

You are now fully linked to source. You can build and run your game.
