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
//  - Variables in states inside of categories must be made default with an X button shown when selecting the category.
//  - Added support for instance members to undo their Make Default command.
// 0.8.6.1
//  - Removed ability to rename/remove default state.
//  - Fixed font rendering bug which would offset centered fonts on the Y axis.hat bo
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
// 0.8.7.1
//  - Fixed vertical alignment issues found in Brake Neck 
// 0.8.7.2
//  - Fixed crash in creating new state caused by variable propagation 
// 0.8.7.3
//  - Fixed crash when running Gum from an outside directory
// 0.8.7.4
//  - Fixed rendering of dimension based wrapping when not using power of 2 sprites
// 0.8.7.5
//  - Added Text.TextRenderingPositionMode so that games can turn off snap to pixel if they want - use at your own risk!
// 0.8.7.6
//  - Added new popup when trying to remove variables from a categorized state, linking to the docs
// 0.9
//  - Added new Width Unit and Height Unit value - dependent on other dimension so that objects can stay a certain aspect ratio when one dimension changes
//  - Fonts with spaces now generate with an underscore in their name instead of space. This makes it easier to work with in engines like FlatRedBall.
// 0.9.0.1
//  - Added NaN check on Height - will throw an exception. This is mainly for games using Gum runtime.
//  - Added GraphicalUiElement.PositionChanged and SizeChanged which is raised in the UpdateLayout
// 0.9.1.0
//  - Added new Error window for behaviors
// 0.9.1.1
//  - Lots of fixes related to categorized states on standard elements
//  - Fixed a crash when setting alignment with category selected.
// 0.9.2
//  - Behaviors now can reference instances which themselves require behaviors
//  - Error window now reports if there are no instances with the required behaviors.
// 0.9.2.1
//  - Fixed bug where components could be named the same as other components if placed in a subfolder.
// 0.9.3
//  - Added icons for % of file for width and height units.
// 0.9.4
//  - Added support for font smoothing to be turned on/off on automatically generated fonts.
// 0.9.4.2
//  - Fixed a bug with letter positioning in Text objects when using scale < 1.
// 0.9.4.3
//  - Renaming a category now adjusts variable names and types, eliminating possible orphan variables
//    which will show up in the property grid with no dropdown.
// 0.9.4.4 
//  - Fixed project ignoring the next file change on a new Gum project that has missing files.
// 0.9.4.5
//  - Removed the performance tab, it doesn't do anything yet
//  - Added try/catch around showing project properties
// 0.9.5
//  - Added vertical/horizontal scroll bars to the wireframe area
[assembly: AssemblyVersion("0.9.5.0")]
[assembly: AssemblyFileVersion("0.9.5.0")]