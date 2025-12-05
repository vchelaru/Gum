---
title: Running From Source
---

# Running from Source

## Introduction

Gum is an open source project so you can run it from source instead of running the pre-compiled project. Running from source is not a requirement, and is only needed if you intend to contribute to Gum or if you'd like to diagnose problems in a debugger.

## Obtaining the source code

1. Download the source file from [GitHub](https://github.com/vchelaru/gum)
   1. If you downloaded the .zip file from the GitHub main page, unzip the file
   2. If you downloaded the file through a Git client, be sure to be on the `master` branch

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Gum repository in Github Desktop</p></figcaption></figure>

## Running the code

1. Locate the Gum.sln file&#x20;
   1. If you downloaded the .zip, it is in the root folder of the zip
   2. If you cloned the repository, it is at the root of the Gum folder
2. Double-click it to open Visual Studio, or open Visual Studio and load the .sln
3. Be sure to build solution rather than pressing F5 (which only builds the current project). This guarantees that all plugins are built and copied correctly. For more information see below.

Once the project has been built, you can run (with or without a debugger attached).

### Building Plugins

Gum depends on a number of plugins for its functionality. By default if you build the project and run it (such as by pressing F5 in Visual Studio), then plugins are not automatically built. To build plugins, you need to explicitly build all plugin projects. The easiest way to do this is to select the Build -> Rebuild Solution option in Visual Studio.

<figure><img src="../../.gitbook/assets/image (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1) (1).png" alt=""><figcaption><p>Build -> Rebuild Solution in Visual Studio</p></figcaption></figure>
