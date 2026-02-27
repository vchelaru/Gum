# Importing Elements

## Introduction

Gum supports importing elements into your project, making it easier to reuse existing work. Elements can be imported from your project's folder structure, from a different project on disk, or from projects located on the internet.

{% hint style="info" %}
Importing from projects requires Gum February 2026 or newer.
{% endhint %}

## Importing Elements in Project Subfolder

Files which are already added to a subfolder of your project can be directly imported. To do this:

1.  Save a Screen or Component to your project's Screens or Components folder, or a contained subfolder<br>

    <figure><img src="../.gitbook/assets/27_07 01 50.png" alt=""><figcaption></figcaption></figure>
2. Right click on your Screens or Components folder and select the Import item
3. Select the component in the list, or click Browse... to locate a component
4. Click the Import button

## Importing from Project (.gumx)

Gum supports importing one or more elements from an existing Gum project (.gumx). When importing elements, Gum also imports:

* Elements referenced by instances or through inheritance
* Referenced behaviors
* Referenced files, such as .png or .ganx (Gum Animation Xml)

This option is useful if you would like to import a set of components, such as a restyled set of Forms controls.

To import from a project:

1. Click the Content -> Import from .gumx... menu item
2. Select either Local File or URL depending on the location of the .gumx
3. Browser or enter the location of the project
4. Select the desired objects to import. You can click entire folders to select all contained objects. Note that dependencies (such as behaviors) are automatically checked.
5. Click the Import button
