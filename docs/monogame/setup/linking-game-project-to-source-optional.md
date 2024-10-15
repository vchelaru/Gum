# Linking Game Project to Source

### Introduction

The easiest way to use Gum is to add the NuGet packages to your game project. Alternatively you can link your game to Gum source for additional debugging, to stay up to date with the latest improvements, or if you are interested in contributing.

This document assumes you already have a game project with the Gum NuGet packages linked.

### Linking Soruce

If you have followed the Setup steps, then you should have a game which references the Gum NuGet package.

<figure><img src="../../.gitbook/assets/14_21 53 07.png" alt=""><figcaption><p>Default setup referencing NuGet Package</p></figcaption></figure>

To replace this package with source references:

1. Clone the Gum repository
2. Remove the NuGet package by selecting Gum.MonoGame and pressing the Delete key.
3. Right-click on your Solution in the Solution Explorer and select **Add** -> **Existing Project...**
4. Select \<Gum Root>/MonoGameGum/MonoGameGum.csproj
5. Repeat the previous step but select the following csproj files:
   1. \<Gum Root>/GumCommon/GumCommon.csproj
   2. \<Gum Root>/GumDataTypes/GumDataTypesNet6.csproj
   3.  \<Gum Root>/ToolsUtilities/ToolsUtilitiesStandard.csproj\


       <figure><img src="../../.gitbook/assets/14_22 07 47.png" alt=""><figcaption><p>A game project named GumTest and the other projects in the Solution Explorer</p></figcaption></figure>
6. Right-click on your game project's Dependencies folder and select Add Project Reference...
7.  Check the Gum projects you added to the solution earlier and click OK\


    <figure><img src="../../.gitbook/assets/14_22 08 45.png" alt=""><figcaption><p>Check the Gum projects then click OK</p></figcaption></figure>

You are now fully linked to source. You can build and run your game.

