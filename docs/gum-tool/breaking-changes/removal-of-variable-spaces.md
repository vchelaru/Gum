# Removal of Variable Spaces

## Introduction

This change, introduced in source at the end of January 2025, removes all spaces from standard variable names. Gum will automatically remove these spaces from projects upon project load. If you are using version control software, you may notice a large number of changes in your Gum files due to the removal and saving of these files. In most cases no changes are required at runtime, but this page describes the change in detail in case your project depends on variable names with spaces.

## Motivation

The removal of variable spaces unifies the variable names across all standard elements. Prior to this change, some variables included spaces in their names when saved to XML (such as `X Units`), while other variables did not include spaces and instead used Pascal Case (such as `StackSpacing`).

This inconsistent spacing adds confusion when writing variable references and when assigning state variable values in code (such as when customizing standard Forms controls in code).

After this change, all variables will use Pascal Casing, making it easier to write code and variable assignments. This also allows using the `nameof` keyword when assigning variables.

## Changed Variables

The following variables have been changed to no longer include spaces in their names:

* Base Type ⇒ BaseType
* Children Layout ⇒ ChildrenLayout
* Clips Children ⇒ ClipsChildren
* Contained Type ⇒ ContainedType
* Font Scale ⇒ FontScale
* Height Units ⇒ HeightUnits
* Texture Address ⇒ TextureAddress
* Texture Height ⇒ TextureHeight
* Texture Height Scale ⇒ TextureHeightScale
* Texture Left ⇒ TextureLeft
* Texture Top ⇒ TextureTop
* Texture Width ⇒ TextureWidth
* Texture Width Scale ⇒ TextureWidthScale
* Width Units ⇒ WidthUnits
* Wraps Children ⇒ WrapsChildren
* X Origin ⇒ XOrigin
* X Units ⇒ XUnits
* Y Origin ⇒ YOrigin
* Y Units ⇒ YUnits

## Expected Changes

Projects which have been created prior to this change will likely have variables in their XML with spaces. When a new version of the Gum tool loads a file, it loops through all variables and removes their spaces.

## Runtime Changes

The runtime objects (GraphicalUiElement) have been written to accept variables with and without spaces. Therefore, even if your game updates to a new version of the Gum libraries but you haven't yet converted variables to spaces, the game should still work the same as before.

The check for variables with spaces may be done in the future, but if so it is unlikely to happen for many years. The GraphicalUiElement object has been checking for variables with and without spaces for many years as well, so if you open an old project in Gum and convert the variables to no longer have spaces, old games will be able to load these newly-upgraded files without any problems.

## Testing

Prior to releasing this change, these have been tested on a number of projects. Each project has performed 2 tests:

1. Loading old Gum projects with the new source before the project has been upgraded
2. Upgrading the Gum project automatically by loading it in Gum and testing the project after upgrade

These changes have been tested on the following projects

* Cranky Chibi Cthulhu
* FlatRedBall Automated Test Project (.NET 6, Desktop GL)
* MonoGame Gum Forms Sample Project
* Kid Defense
* BattleCrypt Bombers

## Required Code Changes

If your code is setting any of these variables explicitly, such as creating state varaibles, you will need to adjust your code to no longer include spaces in the variable names listed above.

