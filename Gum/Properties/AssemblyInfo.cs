using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Gum")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("FlatRedBall")]
[assembly: AssemblyProduct("Gum")]
[assembly: AssemblyCopyright("Copyright © FlatRedBall 2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("2ba68ce8-394b-4b73-98c2-9bc83f43e8c3")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
// 0.8.2 adds improved stacking + relative to children behavior.
// 0.8.2.1 fixes outline thickness not creating custom fonts like it should
// 0.8.2.2 Adds ExposesChildrenEvents property
// 0.8.2.3
//  - Drag+drop an instance on its parent detaches the instance from any parents
//  - Copy/paste components now works
//  - Gum no longer crashes when FNT file is missing PNG
//  - Replaced how visible/invisible component boundaries worked by creating new InvisibleRenderable type
// 0.8.3.0
//  - Fixed Drag+drop standard element into component folder crash
//  - Added support for renaming component folders
// 0.8.3.1
//  - States are always applied first, then non-state variables are applied after. This allows state variables
//    to be overridden.
//  - Drag+drop a file on a sprite now asks you if you want to reference in the original location or copy.
//  - Drag+drop now registers to the undo system, so it can be undone
//  - Drag+drop object on another object will now make it a child of the dropped-on object if dropped on
//    an object rather than the top componet/screen.
// 0.8.3.2
//  - Fixed reordering not showing up on tree views if dealing with child/parents
//  - Fixed bug where absolute custom fonts wouldn't render properly. Not sure how this broke before
// 0.8.3.3
//  - Fixed clicking on a circle causing crash
// 0.8.3.4
//  - Fixed default text (font) not showing up.
//  - Fixed bug with exposing a variable on non-default state causing weird behavior
//  - Drag+dropping multiple objects will set the parent on all selected objects
// 0.8.4
//  - Rectangle color can now be set
//  - Circle color can now be set
// 0.8.5
//  - Setting a variable in a state in a category sets the same variable on other states in the same category,
//    making animations much easier to create.
// 0.8.5.1
//  - Fixed possible crash when creating Text objects.
// 0.8.6.0
//  - Reorganized code for property grid (not functional, just moved files to be under the plugin folder)
//  - Variables on states inside of categories cannot be made default anymore
//  - Variables in states inidde of categories must be made default with an X button shown when selecting the category.
//  - Added support for instance members to undo their Make Default command.
// 0.8.6.1
//  - Removed ability to rename/remove default state.
//  - Fixed font rendering bug which would offset centered fonts on the Y axis.
// 0.8.6.2
//  - Fixed variables not propagating to sibling states in a category when editing in the wireframe window.
// 0.8.6.3
//  - Fixed crash when setting text color in a categorized state.
// 0.8.6.4
//  - Fixed rendering issue where text objects sometimes render using the wrong render states
// 0.8.6.5
//  - Added support for setting the single pixel texture and source rectangle on renderer, to be used with spritesheets at runtime for rendering speed.
// 0.8.7
//  - Fixed more bugs on text rendering when doing character-by-character, may be ready for prime time
[assembly: AssemblyVersion("0.8.7.0")]
[assembly: AssemblyFileVersion("0.8.7.0")]