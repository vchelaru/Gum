namespace WpfDataUi;

/// <summary>Display-text helpers shared by the grids and displayers.</summary>
public static class DataUiText
{
    /// <summary>Turns "CamelCase" into "Camel Case", leaving existing spaces alone.</summary>
    public static string InsertSpacesInCamelCase(string originalString)
    {
        // The first character is never preceded by a space.
        for (int i = originalString.Length - 1; i > 0; i--)
        {
            if (char.IsUpper(originalString[i]) && originalString[i - 1] != ' ')
            {
                originalString = originalString.Insert(i, " ");
            }
        }

        return originalString;
    }
}
