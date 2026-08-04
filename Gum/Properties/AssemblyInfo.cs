using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
// AssemblyTitle/Configuration/Company/Product/Version/FileVersion are deliberately NOT
// declared here (and GenerateAssemblyInfo is false, so the SDK doesn't generate them
// either): the WPF XAML markup-compile temp project (Gum_*_wpftmp.csproj, used for the
// precompiled BAML pass) always independently (re)generates exactly one of these SDK-managed
// attributes for itself no matter what this project does, and having any of them declared
// here too - manually or via MSBuild's <AssemblyTitle>/<Version>/etc. properties - causes
// CS0579 duplicate-attribute errors in wpftmp's own compile (a real WPF/SDK bug, not
// something in this repo's control; see e.g. https://github.com/dotnet/wpf/issues/93).
// The build version lives in AssemblyMetadata below instead, which isn't part of that
// auto-generated set and so isn't affected.
[assembly: AssemblyDescription("")]
[assembly: AssemblyCopyright("Copyright © FlatRedBall 2017-2026.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("2ba68ce8-394b-4b73-98c2-9bc83f43e8c3")]

// BuildVersion is the source of truth for the tool's displayed version (see
// MenuStripManager's About dialog). Kept as an ordinary AssemblyMetadata attribute rather
// than AssemblyVersion/AssemblyFileVersion - see the comment above.
[assembly: AssemblyMetadata("BuildVersion", "2026.08.04")]
[assembly: InternalsVisibleTo("GumToolUnitTests")]
[assembly: InternalsVisibleTo("EditorTabPlugin_XNA")]
