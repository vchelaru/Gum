---
title: Running From Source
---

# Running from Source

## Introduction

Gum is an open source project so you can run it from source instead of running the pre-compiled project.

## Obtaining the source code



1. Download the source file from [GitHub](https://github.com/vchelaru/gum)
   1. If you downloaded the .zip file from the GitHub main page, unzip the file
   2. If you downloaded the file through a Git client, be sure to be on the `master` branch

## Running the code

1. Locate the Gum.sln file
   1. If you downloaded the .zip, it is in the root folder of the zip
   2. If you cloned the repository, it is at the root of the Gum folder
2. Double-click it to open Visual Studio, or open Visual Studio and load the .sln
3. Run the build configuration in "x86". The build configuration may default to "Mixed Platforms". If you do not change it Gum will not compile.
4. If your project depends on plugins, be sure to build solution, rather than pressing F5. This guarantees that all plugins are built and copied correctly.
