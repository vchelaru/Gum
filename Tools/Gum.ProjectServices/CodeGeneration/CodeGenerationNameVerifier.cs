using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.ProjectServices.CodeGeneration;

public class CodeGenerationNameVerifier
{
    private delegate bool IsValidCSharpNameCheck(string name, out string? whyNotValid, out CommonValidationError commonValidationError);

    private readonly IsValidCSharpNameCheck _isValidCSharpName;

    /// <summary>
    /// Uses the tool's name verifier, which answers with the shared <see cref="NameRules"/>.
    /// </summary>
    public CodeGenerationNameVerifier(INameVerifier nameVerifier)
    {
        _isValidCSharpName = nameVerifier.IsValidCSharpName;
    }

    /// <summary>
    /// Uses the shared <see cref="NameRules"/> directly, for hosts (the CLI) with no <see cref="INameVerifier"/>.
    /// </summary>
    public CodeGenerationNameVerifier(NameRules nameRules)
    {
        _isValidCSharpName = nameRules.IsValidCSharpName;
    }

    private HashSet<string>? _reservedMemberNames;

    /// <summary>
    /// Names that a member of the class being generated can't take (the class's own name, the methods and
    /// state properties the generator declares). <see cref="ToCSharpMemberName"/> steps around them until the returned scope is disposed.
    /// </summary>
    internal IDisposable ReserveMemberNames(IEnumerable<string> names)
    {
        HashSet<string>? previous = _reservedMemberNames;
        _reservedMemberNames = new HashSet<string>(names.Select(name => name.TrimStart('@')), StringComparer.Ordinal);
        return new ReservedMemberNamesScope(this, previous);
    }

    /// <summary>
    /// <see cref="ToCSharpName(string)"/> for a name that becomes a member of the generated class (an instance,
    /// a custom or exposed variable, an animation). A name that collides with a reserved member name gets a
    /// trailing underscore; C# allows neither a member named like its enclosing type (CS0542) nor two members of one name.
    /// </summary>
    internal string ToCSharpMemberName(string name)
    {
        string csharpName = ToCSharpName(name);
        return _reservedMemberNames != null && _reservedMemberNames.Contains(csharpName.TrimStart('@'))
            ? csharpName + "_"
            : csharpName;
    }

    private sealed class ReservedMemberNamesScope : IDisposable
    {
        private readonly CodeGenerationNameVerifier _owner;
        private readonly HashSet<string>? _previous;

        public ReservedMemberNamesScope(CodeGenerationNameVerifier owner, HashSet<string>? previous)
        {
            _owner = owner;
            _previous = previous;
        }

        public void Dispose() => _owner._reservedMemberNames = _previous;
    }

    internal string ToCSharpName(string name) => ToCSharpName(name, out _);

    internal string ToCSharpName(string name, out bool isPrefixed)
    {
        isPrefixed = false;

        if (!_isValidCSharpName(name, out string? whyNotValid, out CommonValidationError validationError))
        {
            if (validationError == CommonValidationError.InvalidStartingCharacterForCSharp)
            {
                name = "_" + name;
                isPrefixed = true;
            }
            else if (validationError == CommonValidationError.ReservedCSharpKeyword)
            {
                name = "@" + name;
                isPrefixed = true;
            }
            else
            {
                throw new NotImplementedException("Reason why name is invalid C# name is unhandled.\n" +
                                                  $"Reason: {whyNotValid}");
            }
        }

        return name.Replace(" ", "_");
    }
}
