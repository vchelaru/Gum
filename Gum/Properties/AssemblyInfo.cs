﻿using System.Reflection;
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

[assembly: AssemblyVersion("0.8.3.0")]
[assembly: AssemblyFileVersion("0.8.3.0")]