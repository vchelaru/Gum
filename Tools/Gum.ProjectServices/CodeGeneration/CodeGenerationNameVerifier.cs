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
