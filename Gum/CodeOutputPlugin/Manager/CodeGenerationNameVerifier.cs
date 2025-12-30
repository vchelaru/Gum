using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeOutputPlugin.Manager;

public class CodeGenerationNameVerifier
{
    INameVerifier _nameVerifier;

    public CodeGenerationNameVerifier(INameVerifier nameVerifier)
    {
        _nameVerifier = nameVerifier;
    }

    internal string ToCSharpName(string name) => ToCSharpName(name, out _);

    internal string ToCSharpName(string name, out bool isPrefixed)
    {
        isPrefixed = false;

        if (!_nameVerifier.IsValidCSharpName(name, out string whyNotValid, out CommonValidationError validationError))
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
