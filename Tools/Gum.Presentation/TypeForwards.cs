using System.Runtime.CompilerServices;

// Moved to Gum.ProjectServices so gumcli edit shares them with the tool; plugins built against
// Gum.Presentation keep resolving them.
[assembly: TypeForwardedTo(typeof(Gum.Managers.TopLevelName))]
[assembly: TypeForwardedTo(typeof(Gum.Services.ICircularReferenceManager))]
[assembly: TypeForwardedTo(typeof(Gum.Services.CircularReferenceManager))]
[assembly: TypeForwardedTo(typeof(Gum.DataTypes.ElementSaveDefaultStateExtensions))]
