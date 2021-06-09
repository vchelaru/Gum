---
title: 3 - Files
order: 3
---

# 3 - Files

## Introduction

Gum supports loading image files for Sprites and NineSlices. We'll discuss how to load files, and how they are referenced in Gum.

## Setting up a workspace

First we'll set up a workspace. To do this 1. Create a Screen. I'll call my Screen "SpriteScreen" 1. Drag+drop a Sprite into the newly-created Screen 

![](../.gitbook/assets/GumSpriteInstance.PNG)

1. Setting the Sprite SourceFile

   The "SourceFile" property controls the image that the Sprite displays.  Common examples of source file types are .png and .tga.  To add a source file

2. Select a Sprite
3. Click on the "SourceFile" box
4. Click the "..." button to bring up a file window
5. Navigate to the location of the file you would like to load
6. Click "Open" in the file window
7. Once the source file is set the image will appear in Gum

![](../.gitbook/assets/GumSpriteSourceFile.PNG)

## Source file locations

Files referenced by Gum projects will be relative to the root gum project itself. Therefore, you may notice that the source file begins with a "../", indicating that the relative location of the file is not a subfolder of the Gum project. For portability you may want to keep all source files in a folder located under your gum project \(which has the extension .gumx\).

