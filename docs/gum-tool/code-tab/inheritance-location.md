# Inheritance Location

### Introduction

The `InheritanceLocation` setting controls which of an element's two files declares the base class. It is saved in `ProjectCodeSettings.codsj` and does not have a row on the Code generation settings page.

### Custom and Generated Code

Each Screen and Component is saved as a generated file (`MyScreen.Generated.cs`) and a custom file (`MyScreen.cs`), which together make one `partial` class. See [Folder Layout](README.md#folder-layout).

* `InGeneratedCode` is the default and the recommended value. The generated file declares the base class and the custom file does not.
* `InCustomCode` declares the base class in the custom file instead. Some environments, such as .NET Maui, can benefit from this.

After a settings edit changes the value, Gum asks before rewriting the declaration in each existing custom file, and keeps a backup that **Content > Restore Last Code File Migration** puts back.
