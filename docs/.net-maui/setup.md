# Setup

### Introduction

This page provides instructions on adding a Gum project to your .NET MAUI project. It assumes that you have an existing .NET MAUI project.

### Creating a Gum Project

Gum projects produce generated code, so the Gum files (.gumx, .gusx, and so on) do not need to be added to your project. However, you may still want to add your Gum project in a folder that is under your .csproj so that any referenced content (.png, .svg, or .lottie) can be used by your project.

Save your new Gum project to its desired location. This saves the .gumx along with all other files in the Gum project. By default this includes all standards, so keep in mind that over time this may produce a lot of files.

### Enabling CodeGen

To enable CodeGen:

1. Select the Code tab
2. Check the Is CodeGen Plugin Eabled
3. Enter the CodeProjectRoot - this is the root of where you want all generated code to be located. Consider a folder such as Views in your project. This should not be in a platform-specific folder.

### Adding Built-In Components

Next we'll add Built-In controls. You are free to create as many or as few as you would like, and which you create can expand over time as you install more third party controls. These controls are considered "built in" from Gum's perspective which means Gum will not attempt to generate code for them.

For example, the following shows how to create a Gum built in component:

1. Decide where to place your built-in controls. It's best to keep these all together since you will reference them frequently, so consider creating a MauiBuiltIn folder under Components.
2. Add a new component. This should be named the exact same as the component in code. For example, "Button" if you intend to create a component for the .NET MAUI button control.
3. Change the GenerationBehavior for this new component to "NeverGenerate"
4. You may need to delete the Button.cs and Button.Generated.cs files from your project since those were created prior to when the Button was marked as NeverGenerate.



.. under construction

