using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Managers;

public static class NameVerifierExtensions
{
    /// <summary>
    /// Turns <paramref name="candidate"/>, which comes from outside data such as a dropped file's
    /// name, into a name <see cref="INameVerifier.IsInstanceNameValid"/> accepts for a new instance
    /// in <paramref name="container"/>. Invalid characters become underscores, an empty candidate
    /// becomes <paramref name="fallbackName"/>, and a reserved or already-used name gets a number.
    /// </summary>
    public static string MakeValidInstanceName(this INameVerifier nameVerifier, string candidate, string fallbackName, ElementSave container)
    {
        string name = string.IsNullOrEmpty(candidate) ? fallbackName : NameVerifier.ToValidName(candidate);
        while (!nameVerifier.IsInstanceNameValid(name, null, container, out _))
        {
            name = StringFunctions.IncrementNumberAtEnd(name);
        }
        return name;
    }
}
