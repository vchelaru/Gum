<#
.SYNOPSIS
The saved-file comparison behind Tools/project-sweep.ps1, dot-sourced by it and by
Tools/project-sweep-facts-tests.ps1.

.DESCRIPTION
A saved file becomes a flat list of "path = value" facts, so two files that hold the same data
compare equal regardless of formatting: XML attributes and child elements are the same fact (the
compact and verbose formats), and comments, xmlns and xsi:type are ignored. JSON is flattened the
same way. Get-RemovedFacts lists the facts of the original that the saved copy lost.
#>

# The XML walk and the multiset comparison are C#: as PowerShell functions they took minutes over
# the corpus, most of the sweep's run time.
Add-Type -TypeDefinition @'
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

public static class ProjectSweepFacts
{
    public static void AddXmlFacts(XmlElement element, string path, List<string> facts)
    {
        foreach (XmlAttribute attribute in element.Attributes)
        {
            if (attribute.Name.StartsWith("xmlns") || attribute.Prefix == "xsi") continue;
            facts.Add(path + "/" + attribute.LocalName + " = " + attribute.Value);
        }
        var children = new List<XmlElement>();
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlElement childElement) children.Add(childElement);
        }
        if (children.Count == 0)
        {
            if (element.Attributes.Count == 0 || !string.IsNullOrEmpty(element.InnerText)) facts.Add(path + " = " + element.InnerText);
            return;
        }
        foreach (var child in children) AddXmlFacts(child, path + "/" + child.LocalName, facts);
    }

    public static string HashWithoutCarriageReturns(string file)
    {
        var bytes = System.IO.File.ReadAllBytes(file);
        var kept = System.Array.FindAll(bytes, b => b != 13);
        return System.Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(kept));
    }

    // Facts in the original missing from the saved list, counted as a multiset.
    public static List<string> Removed(List<string> original, List<string> saved, string ignore)
    {
        var remaining = new Dictionary<string, int>();
        foreach (var fact in saved) remaining[fact] = remaining.TryGetValue(fact, out var n) ? n + 1 : 1;
        // Case-insensitive, as PowerShell's -match was.
        var defaults = new Regex(" = (false|0|null|)$", RegexOptions.IgnoreCase);
        var ignoreRegex = string.IsNullOrEmpty(ignore) ? null : new Regex(ignore, RegexOptions.IgnoreCase);
        var removed = new List<string>();
        foreach (var fact in original)
        {
            if (remaining.TryGetValue(fact, out var count) && count > 0) { remaining[fact] = count - 1; continue; }
            if (defaults.IsMatch(fact)) continue;
            if (ignoreRegex != null && ignoreRegex.IsMatch(fact)) continue;
            removed.Add(fact);
        }
        return removed;
    }

    private sealed class StateVariable
    {
        public string Key;
        public string Name;
        public string Value;
        // Removes the whole variable, or with field names only those fields.
        public System.Action<string[]> Remove;
    }

    // The tool's load migrates a Circle's legacy Radius r to Width = Height = 2r
    // (GumProjectSaveExtensionMethods.MigrateCircleRadiusToWidthHeight), overwriting any Width and
    // Height the state already had. Drops the Radius, and the value of the old Width and Height,
    // from the original document wherever the saved document holds exactly that result in the same
    // state, so only an unexpected loss is reported. Takes the parsed documents (XmlDocument, or the hashtables of ConvertFrom-Json
    // -AsHashtable) and returns how many Radius values it accepted.
    public static int DropMigratedCircleRadius(object original, object saved)
    {
        var savedValues = new Dictionary<string, string>();
        foreach (var variable in CollectVariables(saved)) savedValues[variable.Key] = variable.Value;
        var originalVariables = CollectVariables(original);

        var radiusVariables = new List<StateVariable>();
        var resized = new List<StateVariable>();
        int accepted = 0;
        foreach (var variable in originalVariables)
        {
            var lastDot = variable.Name.LastIndexOf('.');
            if (variable.Name.Substring(lastDot + 1) != "Radius") continue;
            if (!TryParseFloat(variable.Value, out var radius)) continue;
            var prefix = variable.Key.Substring(0, variable.Key.Length - "Radius".Length);
            if (savedValues.ContainsKey(prefix + "Radius")) continue;
            if (!HasFloat(savedValues, prefix + "Width", radius * 2f) || !HasFloat(savedValues, prefix + "Height", radius * 2f)) continue;

            accepted++;
            radiusVariables.Add(variable);
            foreach (var other in originalVariables)
            {
                if (other.Key == prefix + "Width" || other.Key == prefix + "Height") resized.Add(other);
            }
        }
        // The migration sets Width and Height in place, so their other fields (Category,
        // ExposedAsName...) must survive the save like any other value.
        foreach (var variable in radiusVariables) variable.Remove(null);
        foreach (var variable in resized) variable.Remove(new[] { "Type", "Value", "SetsValue" });
        return accepted;
    }

    private static bool HasFloat(Dictionary<string, string> values, string key, float expected)
    {
        return values.TryGetValue(key, out var text) && TryParseFloat(text, out var value) && value == expected;
    }

    private static bool TryParseFloat(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    // Every variable of every state, keyed by the state's place in the document plus the variable
    // name, so the same variable in the original and the saved copy has the same key.
    private static List<StateVariable> CollectVariables(object document)
    {
        var variables = new List<StateVariable>();
        if (document is XmlDocument xml) CollectXml(xml.DocumentElement, "", variables);
        else CollectJson(document, "", variables);
        return variables;
    }

    private static void CollectXml(XmlElement element, string path, List<StateVariable> variables)
    {
        path = path + "/" + element.LocalName + "[" + XmlField(element, "Name") + "]";
        foreach (XmlNode child in element.ChildNodes)
        {
            if (!(child is XmlElement childElement)) continue;
            if (element.LocalName == "State" && childElement.LocalName == "Variable")
            {
                var name = XmlField(childElement, "Name");
                if (name == null) continue;
                variables.Add(new StateVariable
                {
                    Key = path + "|" + name,
                    Name = name,
                    Value = XmlField(childElement, "Value"),
                    Remove = fields => RemoveXml(childElement, fields)
                });
            }
            else
            {
                CollectXml(childElement, path, variables);
            }
        }
    }

    private static void RemoveXml(XmlElement variable, string[] fields)
    {
        if (fields == null) { variable.ParentNode.RemoveChild(variable); return; }
        foreach (var field in fields)
        {
            variable.RemoveAttribute(field);
            var children = new List<XmlNode>();
            foreach (XmlNode child in variable.ChildNodes)
            {
                if (child is XmlElement childElement && childElement.LocalName == field) children.Add(child);
            }
            foreach (var child in children) variable.RemoveChild(child);
        }
    }

    // A field in either format: an attribute (compact) or a child element (verbose).
    private static string XmlField(XmlElement element, string name)
    {
        if (element.HasAttribute(name)) return element.GetAttribute(name);
        foreach (XmlNode child in element.ChildNodes)
        {
            if (child is XmlElement childElement && childElement.LocalName == name) return childElement.InnerText;
        }
        return null;
    }

    // A JSON value is stored under Value or a typed key such as ValueAsFloat.
    private static void RemoveJson(IDictionary variable, string[] fields)
    {
        if (fields == null) { variable.Clear(); return; }
        var keys = new List<string>();
        foreach (var key in variable.Keys)
        {
            var name = (string)key;
            foreach (var field in fields)
            {
                if (name == field || (field == "Value" && name.StartsWith("ValueAs"))) keys.Add(name);
            }
        }
        foreach (var key in keys) variable.Remove(key);
    }

    private static void CollectJson(object node, string path, List<StateVariable> variables)
    {
        if (node is IDictionary dictionary)
        {
            // Named like the XML walk, so same-named states in different categories differ.
            path = path + "[" + dictionary["Name"] + "]";
            if (dictionary["Variables"] is IList stateVariables)
            {
                foreach (var item in stateVariables)
                {
                    if (!(item is IDictionary variable) || variable["Name"] == null) continue;
                    var name = variable["Name"].ToString();
                    var value = variable["ValueAsFloat"] ?? variable["Value"];
                    variables.Add(new StateVariable
                    {
                        Key = path + "|" + name,
                        Name = name,
                        Value = value == null ? null : System.Convert.ToString(value, CultureInfo.InvariantCulture),
                        // Arrays from ConvertFrom-Json can't shrink; an emptied object adds no facts.
                        Remove = fields => RemoveJson(variable, fields)
                    });
                }
            }
            foreach (DictionaryEntry entry in dictionary)
            {
                if ((string)entry.Key != "Variables") CollectJson(entry.Value, path + "/" + entry.Key, variables);
            }
        }
        else if (node is IList list && !(node is string))
        {
            foreach (var item in list) CollectJson(item, path, variables);
        }
    }
}
'@

function Add-JsonFacts($node, [string]$path, [System.Collections.Generic.List[string]]$facts) {
    if ($node -is [System.Collections.IDictionary]) {
        foreach ($key in $node.Keys) { Add-JsonFacts $node[$key] "$path/$key" $facts }
    } elseif ($node -is [System.Collections.IList]) {
        if ($node.Count -eq 0) { $facts.Add("$path = ") }
        foreach ($item in $node) { Add-JsonFacts $item $path $facts }
    } elseif ($null -eq $node) {
        $facts.Add("$path = null")
    } elseif ($node -is [bool]) {
        $facts.Add("$path = $($node.ToString().ToLowerInvariant())")
    } else {
        $facts.Add("$path = $([System.Convert]::ToString($node, [System.Globalization.CultureInfo]::InvariantCulture))")
    }
}

# A saved file parsed: an XmlDocument, or JSON as nested hashtables.
function Read-SavedDocument([string]$file) {
    $text = [System.IO.File]::ReadAllText($file)
    if ($text.TrimStart([char]0xFEFF).TrimStart().StartsWith('<')) {
        $document = [System.Xml.XmlDocument]::new()
        $document.LoadXml($text.TrimStart([char]0xFEFF))
        return $document
    }
    # The comma keeps PowerShell from unrolling a top-level JSON array.
    return , ($text | ConvertFrom-Json -AsHashtable)
}

function Get-DocumentFacts($document) {
    $facts = [System.Collections.Generic.List[string]]::new()
    if ($document -is [System.Xml.XmlDocument]) {
        [ProjectSweepFacts]::AddXmlFacts($document.DocumentElement, $document.DocumentElement.LocalName, $facts)
    } else {
        Add-JsonFacts $document '' $facts
    }
    # The comma keeps PowerShell from unrolling the list into an array.
    return , $facts
}

# Facts (as a multiset) in the original that are gone from the saved copy, ignoring default values
# (false, 0, null, empty) that a newer serializer omits. Reordering and additions are not removals.
# -AllowCircleRadiusMigration accepts the tool load's Radius -> Width/Height migration (see
# DropMigratedCircleRadius); only a tool-style load runs it, so a raw save must not.
function Get-RemovedFacts([string]$original, [string]$saved, [string]$ignore, [switch]$AllowCircleRadiusMigration) {
    if (-not (Test-Path -LiteralPath $original)) { return @() }
    try {
        $originalDocument = Read-SavedDocument $original
        $savedDocument = Read-SavedDocument $saved
        if ($AllowCircleRadiusMigration) {
            [ProjectSweepFacts]::DropMigratedCircleRadius($originalDocument, $savedDocument) | Out-Null
        }
        return @([ProjectSweepFacts]::Removed((Get-DocumentFacts $originalDocument), (Get-DocumentFacts $savedDocument), $ignore))
    } catch {
        return @("(could not compare: $($_.Exception.Message))")
    }
}
