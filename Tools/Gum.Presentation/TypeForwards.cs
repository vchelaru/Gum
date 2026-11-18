using System.Runtime.CompilerServices;

// Moved to Gum.ProjectServices so gumcli edit shares them with the tool; plugins built against
// Gum.Presentation keep resolving them.
[assembly: TypeForwardedTo(typeof(Gum.Managers.TopLevelName))]
[assembly: TypeForwardedTo(typeof(Gum.Services.ICircularReferenceManager))]
[assembly: TypeForwardedTo(typeof(Gum.Services.CircularReferenceManager))]
[assembly: TypeForwardedTo(typeof(Gum.DataTypes.ElementSaveDefaultStateExtensions))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.IReferenceFinder))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.ReferenceFinder))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.IReferenceFinderProjectProvider))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.ElementReferences))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.InstanceReferences))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.StateReferences))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.BehaviorReferences))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.CategoryReferences))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.NameChangeAction))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.SideOfEquals))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.VariableChange))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.VariableReferenceChange))]
[assembly: TypeForwardedTo(typeof(Gum.Logic.VariableChangeResponse))]
