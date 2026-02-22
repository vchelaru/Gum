using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Gum.DataTypes;

public static class VariableSaveSerializer
{
    private static readonly Dictionary<Type, XmlSerializer> _compactSerializers = new();
    private static readonly Dictionary<Type, XmlSerializer> _legacyInstancesCompactSerializers = new();
    private static XmlSerializer? _gumProjectCompactSerializer;

    // Properties to serialize as XML attributes (Value intentionally excluded)
    private static readonly string[] AttributeProps =
    [
        "Type", "Name", "SetsValue", "Category", "ExposedAsName",
        "StandardizedName", "IsFile", "IsFont", "IsHiddenInPropertyGrid", "IsCustomVariable"
    ];

    private static void AddVariableSaveOverrides(XmlAttributeOverrides overrides)
    {
        foreach (var prop in AttributeProps)
        {
            XmlAttributes attrs = new XmlAttributes();
            attrs.XmlAttribute = new XmlAttributeAttribute(prop);
            overrides.Add(typeof(VariableSave), prop, attrs);
        }
    }

    private static void AddInstanceSaveOverrides(XmlAttributeOverrides overrides)
    {
        foreach (var member in new[] { "Name", "BaseType", "Locked", "IsSlot" })
        {
            XmlAttributes attrs = new XmlAttributes();
            attrs.XmlAttribute = new XmlAttributeAttribute(member);
            overrides.Add(typeof(InstanceSave), member, attrs);
        }

        XmlAttributes definedByBaseAttrs = new XmlAttributes();
        definedByBaseAttrs.XmlAttribute = new XmlAttributeAttribute("DefinedByBase");
        definedByBaseAttrs.XmlDefaultValue = false;
        overrides.Add(typeof(InstanceSave), "DefinedByBase", definedByBaseAttrs);
    }

    /// <summary>
    /// Full compact serializer: VariableSave and InstanceSave members as XML attributes.
    /// Use for v2 files where both variables and instances are in attribute format.
    /// </summary>
    public static XmlSerializer GetCompactSerializer(Type rootType)
    {
        lock (_compactSerializers)
        {
            if (_compactSerializers.TryGetValue(rootType, out var cached))
                return cached;

            XmlAttributeOverrides overrides = new XmlAttributeOverrides();
            AddVariableSaveOverrides(overrides);
            AddInstanceSaveOverrides(overrides);

            var serializer = new XmlSerializer(rootType, overrides);
            _compactSerializers[rootType] = serializer;
            return serializer;
        }
    }

    /// <summary>
    /// Mixed serializer: VariableSave members as XML attributes, InstanceSave as child elements.
    /// Use for transitional files saved before instance compaction was introduced.
    /// </summary>
    public static XmlSerializer GetLegacyInstancesCompactSerializer(Type rootType)
    {
        lock (_legacyInstancesCompactSerializers)
        {
            if (_legacyInstancesCompactSerializers.TryGetValue(rootType, out var cached))
                return cached;

            XmlAttributeOverrides overrides = new XmlAttributeOverrides();
            AddVariableSaveOverrides(overrides);

            var serializer = new XmlSerializer(rootType, overrides);
            _legacyInstancesCompactSerializers[rootType] = serializer;
            return serializer;
        }
    }

    public static XmlSerializer GetGumProjectCompactSerializer()
    {
        lock (_compactSerializers)
        {
            if (_gumProjectCompactSerializer != null)
                return _gumProjectCompactSerializer;

            XmlAttributeOverrides overrides = new XmlAttributeOverrides();

            foreach (var member in new[] { "Name", "Link" })
            {
                XmlAttributes attrs = new XmlAttributes();
                attrs.XmlAttribute = new XmlAttributeAttribute(member);
                overrides.Add(typeof(ElementReference), member, attrs);
            }

            XmlAttributes linkTypeAttrs = new XmlAttributes();
            linkTypeAttrs.XmlAttribute = new XmlAttributeAttribute("LinkType");
            linkTypeAttrs.XmlDefaultValue = LinkType.ReferenceOriginal;
            overrides.Add(typeof(ElementReference), "LinkType", linkTypeAttrs);

            XmlAttributes elementTypeIgnore = new XmlAttributes();
            elementTypeIgnore.XmlIgnore = true;
            overrides.Add(typeof(ElementReference), "ElementType", elementTypeIgnore);

            XmlAttributes behaviorNameAttrs = new XmlAttributes();
            behaviorNameAttrs.XmlAttribute = new XmlAttributeAttribute("Name");
            overrides.Add(typeof(BehaviorReference), "Name", behaviorNameAttrs);

            _gumProjectCompactSerializer = new XmlSerializer(typeof(GumProjectSave), overrides);
            return _gumProjectCompactSerializer;
        }
    }

    /// <summary>
    /// Reads a file and detects whether it is in compact (v2) format or legacy (v1) format.
    /// V1 files have a &lt;Type&gt; child element inside &lt;Variable&gt;.
    /// Returns both the file content and whether it is in compact format.
    /// </summary>
    public static (string content, bool isCompact) ReadAndDetectFormat(string fileName)
    {
        string content = File.ReadAllText(fileName);
        // v1 files have <Variable> as a child-element opening tag with no attributes.
        // v2 files have attributes inline: <Variable Type="..." ...>
        bool isV1 = content.Contains("<Variable>");
        return (content, !isV1);
    }
}
