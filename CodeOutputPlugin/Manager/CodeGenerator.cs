using CodeOutputPlugin.Models;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using GumDataTypes.Variables;
using GumRuntime;
using Newtonsoft.Json.Linq;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using SharpDX.DirectWrite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using ToolsUtilities;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace CodeOutputPlugin.Manager;

#region Enums

public enum VisualApi
{
    Gum,
    XamarinForms
}

#endregion

#region CodeGenerationContext Class

public struct CodeGenerationContext
{
    /// <summary>
    /// the prefix with no period, such as "casted"
    /// </summary>
    public string ThisPrefix { get; set; }

    bool? _isInstanceFormsObject;

    InstanceSave? _instance;
    public InstanceSave? Instance
    {
        get => _instance;
        set
        {
            if (_instance != value)
            {
                _instance = value;

                RefreshIsInstanceFormsObject();
            }

        }
    }

    private void RefreshIsInstanceFormsObject()
    {
        if (_instance == null || CodeOutputProjectSettings?.OutputLibrary != OutputLibrary.MonoGameForms)
        {
            _isInstanceFormsObject = false;
        }
        else
        {
            _isInstanceFormsObject = false;
            var instanceElement = ObjectFinder.Self.GetElementSave(_instance);
            if (instanceElement != null)
            {
                if (this.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
                {
                    // everything is a form if it's not a standard:
                    var isStandard = instanceElement is StandardElementSave;
                    _isInstanceFormsObject = !isStandard;
                }
                else
                {
                    CodeGenerator.GetGumFormsTypeFromBehaviors(instanceElement, out string? formsType, out _);

                    _isInstanceFormsObject = !string.IsNullOrEmpty(formsType);
                }
            }
        }
    }

    public ElementSave Element { get; set; }
    public StringBuilder StringBuilder { get; set; }

    CodeOutputProjectSettings _codeOutputProjectSettings;
    public CodeOutputProjectSettings CodeOutputProjectSettings
    {
        get => _codeOutputProjectSettings;
        set
        {
            if (_codeOutputProjectSettings != value)
            {
                _codeOutputProjectSettings = value;
                RefreshIsInstanceFormsObject();
            }
        }
    }

    public CodeOutputElementSettings ElementSettings { get; set; }

    public string GumVariablePrefix
    {
        get
        {
            if (Instance == null)
            {
                return String.Empty;
            }
            else
            {
                return Instance.Name + ".";
            }
        }
    }

    /// <summary>
    /// The prefix of the code, with tabs and no trailing period
    /// </summary>
    public string CodePrefix
    {
        get
        {
            return ToTabs(TabCount) + CodePrefixNoTabs;
        }
    }

    public string InstanceNameInCode => Instance == null ? string.Empty : GetInstanceNameInCode(Instance);

    public string GetInstanceNameInCode(InstanceSave instance)
    {
        if (instance.Name.Length > 0 &&
            char.IsDigit(instance.Name[0]))
        {
            return '_' + instance.Name.Replace(" ", "_");
        }

        return instance.Name.Replace(" ", "_");
    }

    public string CodePrefixNoTabs
    {
        get
        {
            if (Instance == null)
            {
                if (string.IsNullOrEmpty(ThisPrefix))
                {
                    if (CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
                    {
                        return "this.Visual";
                    }
                    else
                    {
                        return "this";
                    }
                }
                else
                {
                    return ThisPrefix;
                }
            }
            else
            {
                if (_isInstanceFormsObject == true)
                {
                    if (string.IsNullOrEmpty(ThisPrefix))
                    {
                        return "this." + InstanceNameInCode + "." + "Visual";
                    }
                    else
                    {
                        return ThisPrefix + "." + InstanceNameInCode + "." + "Visual";
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(ThisPrefix))
                    {
                        return "this." + InstanceNameInCode;
                    }
                    else
                    {
                        return ThisPrefix + "." + InstanceNameInCode;
                    }
                }
            }
        }
    }

    private static string ToTabs(int tabCount) => new string(' ', System.Math.Max(0, tabCount) * 4);

    public string Tabs => new string(' ', TabCount * 4);

    int _tabs;
    public int TabCount
    {
        get => _tabs;
        set
        {
            if (_tabs < 0)
            {
                throw new InvalidOperationException();
            }
            _tabs = value;
        }
    }

    public VisualApi VisualApi => CodeGenerator.GetVisualApiForElement(Element);

}

#endregion

public class CodeGenerator
{
    #region CodeGenerator Fields/Properties

    public static int CanvasWidth { get; set; } = 480;
    public static int CanvasHeight { get; set; } = 854;

    public static LocalizationManager LocalizationManager { get; set; }

    /// <summary>
    /// if true, then pixel sizes are maintained regardless of pixel density. This allows layouts to maintain pixel-perfect.
    /// Update: This is now set to false because .... well, it makes it hard to create flexible layouts. It's best to set a resolution of 
    /// 320 wide and let density scale things up
    /// </summary>
    static bool AdjustPixelValuesForDensity { get; set; } = false;

    static CodeGenerationFileLocationsService _codeGenerationFileLocationsService;

    #endregion
    // All the methods here need be changed to instance first (not be static),
    // then we can get rid of this and make it a proper constructor with DI
    static CodeGenerator()
    {
        _codeGenerationFileLocationsService = new CodeGenerationFileLocationsService();
    }

    #region Using Statements

    private void GenerateUsingStatements(CodeGenerationContext context)
    {

        // This code is used to automatially add needed using statements:
        // https://github.com/vchelaru/Gum/issues/598
        HashSet<string> neededUsings = new HashSet<string>();
        neededUsings.Add("GumRuntime");
        neededUsings.Add("System.Linq");

        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame ||
            context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
        {
            neededUsings.Add("MonoGameGum");
            neededUsings.Add("MonoGameGum.GueDeriving");
        }

        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Skia)
        {
            // https://github.com/vchelaru/Gum/issues/895
            neededUsings.Add("SkiaGum.GueDeriving");
        }

        foreach (var instance in context.Element.Instances)
        {
            var gumType = instance.BaseType;

            var instanceElement = ObjectFinder.Self.GetElementSave(instance);

            if (instanceElement != null && instanceElement is not StandardElementSave)
            {
                var elementNamespace = GetElementNamespace(instanceElement, context.ElementSettings, context.CodeOutputProjectSettings);

                if (!string.IsNullOrEmpty(elementNamespace) && !neededUsings.Contains(elementNamespace))
                {
                    neededUsings.Add(elementNamespace);
                }
            }
        }

        foreach (var neededUsing in neededUsings)
        {
            context.StringBuilder.AppendLine($"using {neededUsing};");
        }


        // The regex's here fix this bug:
        // https://github.com/vchelaru/Gum/issues/242
        if (!string.IsNullOrWhiteSpace(context.CodeOutputProjectSettings?.CommonUsingStatements))
        {
            var originalString = context.CodeOutputProjectSettings?.CommonUsingStatements;

            string result = Regex.Replace(originalString, @"(?<!\r)\n", "\r\n");

            context.StringBuilder.AppendLine(result);
        }

        if (!string.IsNullOrEmpty(context.ElementSettings?.UsingStatements))
        {
            string originalString = context.ElementSettings!.UsingStatements;
            string result = Regex.Replace(originalString, @"(?<!\r)\n", "\r\n");

            context.StringBuilder.AppendLine(result);
        }
    }

    #endregion

    #region Namespace

    public static string GetElementNamespace(ElementSave element, CodeOutputElementSettings elementSettings, CodeOutputProjectSettings projectSettings)
    {
        var namespaceName = elementSettings?.Namespace;

        if (string.IsNullOrEmpty(namespaceName) && !string.IsNullOrWhiteSpace(projectSettings.RootNamespace))
        {
            namespaceName = projectSettings.RootNamespace;
            if (element is ScreenSave)
            {
                namespaceName += ".Screens";
            }
            else if (element is ComponentSave)
            {
                namespaceName += ".Components";
            }
            else // standard element
            {
                namespaceName += ".Standards";
            }

            if(projectSettings.AppendFolderToNamespace)
            {
                var splitElementName = element.Name.Replace("\\", "/").Split('/').ToArray();
                var splitPrefix = splitElementName.Take(splitElementName.Length - 1).ToArray();
                var whatToAppend = string.Join(".", splitPrefix);
                if (!string.IsNullOrEmpty(whatToAppend))
                {
                    namespaceName += "." + whatToAppend;
                }
            }
        }

        return namespaceName;
    }

    #endregion

    #region Class Name and Header, Inheritance

    private static void GenerateClassHeader(CodeGenerationContext context)
    {
        //const string access = "public";
        // According to this:https://github.com/vchelaru/Gum/issues/581
        // We should not provide any access, an dinstead should
        // default to internal so that users can control their class
        // scope in the generated code.
        const string accessWithSpace = "";

        var header =
            $"{accessWithSpace}partial class {GetClassNameForType(context.Element, context.VisualApi, context)}";

        if (context.CodeOutputProjectSettings.InheritanceLocation == InheritanceLocation.InGeneratedCode)
        {
            var inheritance = GetInheritance(context.Element, context.CodeOutputProjectSettings);
            header += " : " + inheritance;
        }

        context.StringBuilder.AppendLine(context.Tabs + header);
    }
    
    /// <returns>
    /// The corresponding class name, or <c>null</c> if that type couldn't be found.
    /// </returns>
    public static string? GetClassNameForType(InstanceSave instanceSave, VisualApi visualApi, CodeGenerationContext context, bool isFullyQualified = false)
    {
        var element = ObjectFinder.Self.GetElementSave(instanceSave);
        return element == null ? null : GetClassNameForType(element, visualApi, context, isFullyQualified);
    }
    
    public static string? GetClassNameForType(IStateContainer container, VisualApi visualApi, CodeGenerationContext context, bool isFullyQualified = false)
    {
        string? className = null;
        var specialHandledCase = false;

        if (visualApi == VisualApi.XamarinForms)
        {
            switch (container.Name)
            {
                case "Text":
                    className = "Label";
                    specialHandledCase = true;
                    break;
            }
        }

        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
        {
            // see if it's a forms object:
            if (container is ScreenSave or ComponentSave)
            {
                var strippedType = container.Name;
                if (strippedType.Contains("/"))
                {
                    strippedType = strippedType.Substring(strippedType.LastIndexOf("/") + 1);
                }
                if (strippedType.Contains("\\"))
                {
                    strippedType = strippedType.Substring(strippedType.LastIndexOf("\\") + 1);
                }

                className = strippedType;
                specialHandledCase = true;
            }
        }

        if (!specialHandledCase)
        {

            var strippedType = container.Name;
            if (strippedType.Contains("/"))
            {
                strippedType = strippedType.Substring(strippedType.LastIndexOf("/") + 1);
            }
            if (strippedType.Contains("\\"))
            {
                strippedType = strippedType.Substring(strippedType.LastIndexOf("\\") + 1);
            }

            string suffix = visualApi == VisualApi.Gum ? "Runtime" : "";
            className = $"{strippedType}{suffix}";

        }

        if(isFullyQualified && container is ElementSave elementSave)
        {
            className = GetElementNamespace(elementSave, context.ElementSettings, context.CodeOutputProjectSettings) + "." + className;
        }

        return className;
    }

    public static string GetInheritance(ElementSave element, CodeOutputProjectSettings projectSettings)
    {
        string? inheritance = null;
        if (element is ScreenSave)
        {
            if (projectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
            {
                if (string.IsNullOrEmpty(element.BaseType))
                {
                    inheritance = "MonoGameGum.Forms.Controls.FrameworkElement";
                }
                else
                {
                    inheritance = element.BaseType;
                }
            }
            else
            {
                inheritance = element.BaseType ?? projectSettings.DefaultScreenBase;
            }
        }
        else if (element.BaseType == "XamarinForms/SkiaGumCanvasView")
        {
            inheritance = "SkiaGum.SkiaGumCanvasView";
        }
        else if (element.BaseType == "Container" && projectSettings.OutputLibrary == OutputLibrary.MonoGame)
        {
            if (projectSettings.OutputLibrary == OutputLibrary.MonoGame)
            {
                inheritance = "ContainerRuntime";
            }
        }

        else if (element.BaseType == "Container" ||

            // This allows forms controls like Label to inherit directly from Text, yet still
            // be a Forms control:
            ObjectFinder.Self.GetStandardElement(element.BaseType) != null)
        {
            if (projectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
            {
                GetGumFormsTypeFromBehaviors(element, out string? gumFormsType, out _);

                if (string.IsNullOrEmpty(gumFormsType))
                {
                    // if it inherits from a standard element that is not a container, and it doesn't have any Forms behaviors
                    if (element.BaseType == "Container")
                    {
                        gumFormsType = "MonoGameGum.Forms.Controls.FrameworkElement";
                    }
                    // else it is something like a NineSlice-inheriting object, so don't return a forms inheritance
                    else if (ObjectFinder.Self.GetStandardElement(element.BaseType) != null)
                    {
                        inheritance = "Invalid inheritance - Forms controls must either inherit from Container, or must have Forms behaviors";
                    }
                }

                if (!string.IsNullOrEmpty(gumFormsType))
                {
                    inheritance = gumFormsType;
                }
            }
            else if(projectSettings.OutputLibrary == OutputLibrary.MonoGame)
            {
                var standardElement = ObjectFinder.Self.GetStandardElement(element.BaseType);
                if(standardElement != null)
                {
                    inheritance = "global::MonoGameGum.GueDeriving." + element.BaseType + "Runtime";
                }
                else
                {
                    inheritance = element.BaseType + "Runtime";
                }
            }
            else
            {
                inheritance = "SkiaGum.GueDeriving.ContainerRuntime";
            }
        }
        else
        {
            inheritance = element.BaseType;
            if (inheritance?.Contains("/") == true)
            {
                inheritance = inheritance.Substring(inheritance.LastIndexOf('/') + 1);
            }
            if (projectSettings.OutputLibrary == OutputLibrary.MonoGame)
            {
                // for standards, append "Runtime"
                // Update March 14, 2025
                // Why only on standards?
                // A component (such as MessageBox)
                // could inherit from UserControl, which
                // generates UserControlRuntime.
                var parentElement = ObjectFinder.Self.GetElementSave(inheritance);
                if (element != null)
                {
                    inheritance = inheritance + "Runtime";
                }
            }
        }

        return inheritance;
    }

    #endregion

    #region BindingBehavior Enum
    enum BindingBehavior
    {
        NoBinding,
        BindablePropertyWithEventAssignment,
        BindablePropertyWithBoundInstance
    }

    #endregion

    #region Variables Properties (Exposed and "new")

    private static void FillWithNewVariables(CodeGenerationContext context)
    {
        var variables = context.Element.DefaultState.Variables;

        var stringBuilder = context.StringBuilder;

        foreach (var variable in variables)
        {
            if (variable.IsCustomVariable)
            {
                var type = variable.Type;
                var name = variable.Name;
                stringBuilder.AppendLine(context.Tabs + $"public {type} {name}");
                stringBuilder.AppendLine(context.Tabs + "{");
                context.TabCount++;
                stringBuilder.AppendLine(context.Tabs + $"get;");
                stringBuilder.AppendLine(context.Tabs + $"set;");
                context.TabCount--;

                stringBuilder.AppendLine(context.Tabs + "}");
            }
        }
    }

    private static void FillWithExposedVariables(CodeGenerationContext context)
    {
        var exposedVariables = context.Element.DefaultState.Variables
            .Where(item => !string.IsNullOrEmpty(item.ExposedAsName))
            .ToArray();

        foreach (var exposedVariable in exposedVariables)
        {
            // 
            FillWithExposedVariable(exposedVariable, context);
            context.StringBuilder.AppendLine();
        }
    }

    private static void FillWithExposedVariable(VariableSave exposedVariable, CodeGenerationContext context)
    {
        var container = context.Element;
        var stringBuilder = context.StringBuilder;
        var tabCount = context.TabCount;

        // if both the container and the instance are xamarin forms objects, then we can try to do some bubble-up binding
        var instanceName = exposedVariable.SourceObject;
        var foundInstance = container.GetInstance(instanceName);
        ///////////////Early Out//////////////////////
        if (foundInstance == null)
        {
            return;
        }
        //////////////End Early Out///////////////////

        var bindingBehavior = GetBindingBehavior(container, instanceName);
        var type = exposedVariable.Type;

        var isState = exposedVariable.IsState(container, out ElementSave stateContainer, out StateSaveCategory category);

        var shouldGenerate = true;

        if (isState)
        {
            if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms && stateContainer is StandardElementSave)
            {
                shouldGenerate = false;
            }
        }

        if (shouldGenerate)
        {
            if (isState)
            {
                string stateContainerType;
                VisualApi visualApi = GetVisualApiForElement(stateContainer);
                stateContainerType = GetClassNameForType(stateContainer, visualApi, context);
                type = $"{stateContainerType}.{category.Name}?";
            }

            if (bindingBehavior == BindingBehavior.BindablePropertyWithBoundInstance)
            {
                var containerClassName = GetClassNameForType(container, VisualApi.XamarinForms, context);
                stringBuilder.AppendLine($"{ToTabs(tabCount)}public static readonly BindableProperty {exposedVariable.ExposedAsName}Property = " +
                    $"BindableProperty.Create(nameof({exposedVariable.ExposedAsName}),typeof({type}),typeof({containerClassName}), defaultBindingMode: BindingMode.TwoWay);");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {type} {exposedVariable.ExposedAsName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => ({type})GetValue({exposedVariable.ExposedAsName.Replace(" ", "_")}Property);");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set => SetValue({exposedVariable.ExposedAsName.Replace(" ", "_")}Property, value);");
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
            else if (bindingBehavior == BindingBehavior.BindablePropertyWithEventAssignment)
            {
                var rcv = new RecursiveVariableFinder(container.DefaultState);
                var defaultValue = rcv.GetValue(exposedVariable.Name);
                var defaultValueAsString = VariableValueToGumCodeValue(exposedVariable, context, forcedValue: defaultValue);
                var containerClassName = GetClassNameForType(container, VisualApi.XamarinForms, context);

                string defaultAssignmentWithComma = null;

                if (!string.IsNullOrEmpty(defaultValueAsString))
                {
                    defaultAssignmentWithComma = $", defaultValue:{defaultValueAsString}";
                }

                stringBuilder.AppendLine($"{ToTabs(tabCount)}public static readonly BindableProperty {exposedVariable.ExposedAsName}Property = " +
                    $"BindableProperty.Create(nameof({exposedVariable.ExposedAsName}),typeof({type}),typeof({containerClassName}), defaultBindingMode: BindingMode.TwoWay, propertyChanged:Handle{exposedVariable.ExposedAsName}PropertyChanged{defaultAssignmentWithComma});");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {type} {exposedVariable.ExposedAsName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => ({type})GetValue({exposedVariable.ExposedAsName.Replace(" ", "_")}Property);");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set => SetValue({exposedVariable.ExposedAsName.Replace(" ", "_")}Property, value);");
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"private static void Handle{exposedVariable.ExposedAsName}PropertyChanged(BindableObject bindable, object oldValue, object newValue)");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"var casted = bindable as {containerClassName};");

                if (!string.IsNullOrWhiteSpace(exposedVariable.SourceObject))
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{exposedVariable.SourceObject}.{exposedVariable.GetRootName()} = ({type})newValue;");
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{exposedVariable.SourceObject}?.EffectiveManagers?.InvalidateSurface();");
                }
                else
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{exposedVariable.Name.Replace(" ", "_")} = ({type})newValue;");
                }

                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
            else
            {

                var shouldSetStateByString = false;

                // see if the state is defined by a standard element. If so, we 
                var rootVariable = ObjectFinder.Self.GetRootVariable(exposedVariable.Name, context.Element);
                var isStateOnVisual = false;
                if (isState && context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame)
                {
                    isStateOnVisual = rootVariable != null && ObjectFinder.Self.GetContainerOf(rootVariable) is StandardElementSave;
                    if (isStateOnVisual)
                    {
                        shouldSetStateByString = true;
                    }
                }

                if (shouldSetStateByString)
                {
                    type = "string";
                }

                string sourceObjectName = exposedVariable.SourceObject;
                if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
                {
                    // only if the object is not a standard element
                    var element = ObjectFinder.Self.GetElementSave(foundInstance);
                    if (element is not StandardElementSave)
                    {
                        if (isState == false || isStateOnVisual)
                        {
                            sourceObjectName = exposedVariable.SourceObject + ".Visual";
                        }
                    }
                }

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {type} {exposedVariable.ExposedAsName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                //TryWriteExposedVariableGetter(exposedVariable, context, stringBuilder, tabCount, isState, rootVariable);

                var hasGetter = true;
                if (isState)
                {
                    if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame)
                    {
                        hasGetter = false;
                    }
                }
                if (rootVariable?.Name == "SourceFile")
                {
                    // SourceFileName has no getter by default
                    hasGetter = false;
                }

                if (hasGetter)
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"get => {sourceObjectName.Replace(" ", "_")}.{rootVariable?.Name};");
                }


                if (shouldSetStateByString)
                {
                    var rightSide = $"{sourceObjectName}.SetProperty(\"{exposedVariable.GetRootName()}\", value?.ToString())";
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"set => {rightSide};");
                }
                else
                {
                    if (rootVariable?.Name == "SourceFile")
                    {
                        var variableName = sourceObjectName + ".SourceFileName";
                        stringBuilder.AppendLine(ToTabs(tabCount) + $"set => {variableName} = value;");
                    }
                    else
                    {
                        stringBuilder.AppendLine(ToTabs(tabCount) + $"set => {sourceObjectName.Replace(" ", "_")}.{rootVariable?.Name} = value;");
                    }
                }

                tabCount--;

                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
        }


    }




    #endregion

    #region Instance Properties (like ColoredRectangleInstance or ButtonInstance)

    private static void FillWithInstanceDeclaration(CodeGenerationContext context)
    {
        VisualApi visualApi = VisualApi.Gum;

        var defaultState = context.Element.DefaultState;
        var isXamForms = defaultState.GetValueRecursive($"{context.Instance.Name}.IsXamarinFormsControl") as bool?;
        if (isXamForms == true)
        {
            visualApi = VisualApi.XamarinForms;
        }


        string? className = GetClassNameForType(context.Instance, visualApi, context);

        bool isPublic = true;
        string accessString = isPublic ? "public " : "";

        var isOverride = (defaultState.GetValueRecursive($"{context.Instance.Name}.IsOverrideInCodeGen") as bool?) ?? false;
        if (isOverride)
        {
            accessString += "override ";
        }
        
        if (className == null)
        {
            string message = $"Could not find instance {context.InstanceNameInCode} Gum type." +
                             "Check if it is an instance of a deleted Gum component.";
            context.StringBuilder.AppendLine($"{context.Tabs}// {message}");
            return;
        }
        
        // If this is private, it cannot override anything. Therefore, we'll mark the setter as protected:
        //stringBuilder.AppendLine($"{tabs}{accessString}{className} {instance.Name} {{ get; private set; }}");
        context.StringBuilder.AppendLine($"{context.Tabs}{accessString}{className} {context.InstanceNameInCode} {{ get; protected set; }}");
    }



    #endregion

    #region Initialize

    static bool DoesElementInheritFromCodeGeneratedElement(ElementSave element, CodeOutputProjectSettings projectSettings)
    {
        var foundBase = ObjectFinder.Self.GetElementSave(element.BaseType);
        var isDerived = foundBase != null && (foundBase is StandardElementSave) == false;

        if (isDerived && projectSettings?.BaseTypesNotCodeGenerated != null)
        {
            // check the settings:
            var split = projectSettings?.BaseTypesNotCodeGenerated.Split('\n').Select(item => item.Trim()).ToArray();

            if (split.Contains(element.BaseType))
            {
                isDerived = false;
            }
        }

        return isDerived;
    }

    private static void GenerateInitializeInstancesMethod(CodeGenerationContext context)
    {
        var isDerived = DoesElementInheritFromCodeGeneratedElement(context.Element, context.CodeOutputProjectSettings);

        bool isFullyInstantiatingInCode =
            context.CodeOutputProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode;

        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
        {
            if (isFullyInstantiatingInCode)
            {
                var virtualOrOverride = isDerived
                    ? "override"
                    : "virtual";
                var line = $"protected {virtualOrOverride} void InitializeInstances()";
                context.StringBuilder.AppendLine(context.Tabs + line);

            }
            else
            {
                var line = $"protected override void ReactToVisualChanged()";
                context.StringBuilder.AppendLine(context.Tabs + line);

            }

        }
        else
        {
            if (isFullyInstantiatingInCode)
            {
                var virtualOrOverride = isDerived
                    ? "override"
                    : "virtual";

                var line = $"protected {virtualOrOverride} void InitializeInstances()";
                context.StringBuilder.AppendLine(context.Tabs + line);
            }
            else
            {
                var line = $"public override void AfterFullCreation()";
                context.StringBuilder.AppendLine(context.Tabs + line);
            }
        }

        context.StringBuilder.AppendLine(context.Tabs + "{");

        context.TabCount++;
        context.Instance = null;

        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
        {
            context.StringBuilder.AppendLine(context.Tabs + "base.ReactToVisualChanged();");
        }

        if (isDerived)
        {
            if (isFullyInstantiatingInCode)
            {
                context.StringBuilder.AppendLine(context.Tabs + "base.InitializeInstances();");
            }
            else
            {
                if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
                {
                    // July 17, 2025
                    // I don't think this
                    // is needed for Forms-first
                    // codegen
                    //context.StringBuilder.AppendLine(context.Tabs + "Visual?.AfterFullCreation();");
                }
                else
                {
                    context.StringBuilder.AppendLine(context.Tabs + "base.AfterFullCreation();");
                }

            }
        }


        // Vic asks 
        // March 14 2025
        // Why are we doing
        // this Forms generation
        // only in find by name? 
        // Because if this is fully
        // instantiated in code, then
        // the instances will not yet be
        // attached to this, so the Forms
        // control that searches for items 
        // by name will not be able to find
        // the needed instances. Therefore, this
        // must be done in the constructor after instnaces
        // are attached.
        if (!isFullyInstantiatingInCode)
        {
            TryInstantiateForms(context);
        }

        foreach (var instance in context.Element.Instances)
        {
            if (!instance.DefinedByBase)
            {
                context.Instance = instance;

                if (isFullyInstantiatingInCode)
                {
                    FillWithInstanceInstantiation(context);
                }
                else
                {
                    AddFindByNameAssignment(context);
                }
            }
            context.Instance = null;
        }

        if (!isFullyInstantiatingInCode)
        {
            context.StringBuilder.AppendLine(context.Tabs + "CustomInitialize();");
        }

        context.TabCount--;
        context.StringBuilder.AppendLine(context.Tabs + "}");
    }

    private static void TryInstantiateForms(CodeGenerationContext context)
    {
        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame)
        {
            GetGumFormsTypeFromBehaviors(context.Element, out string? gumFormsType, out _);
            if (gumFormsType != null)
            {
                context.StringBuilder.AppendLine($"{context.Tabs}if (FormsControl == null)");
                context.StringBuilder.AppendLine($"{context.Tabs}{{");
                {
                    context.TabCount++;
                    context.StringBuilder.AppendLine($"{context.Tabs}FormsControlAsObject = new {gumFormsType}(this);");
                    context.TabCount--;
                }
                context.StringBuilder.AppendLine($"{context.Tabs}}}");
            }

        }
    }

    private static void AddFindByNameAssignment(CodeGenerationContext context)
    {
        var isGeneratingFormsControls = context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms;

        if (isGeneratingFormsControls)
        {
            var instance = context.Instance;

            var element = ObjectFinder.Self.GetElementSave(instance);

            var isInstanceFormsForms = element is ComponentSave;

            if (isInstanceFormsForms)
            {

                var classNameString = GetClassNameForType(context.Instance, context.VisualApi, context);

                context.StringBuilder.AppendLine(
                    $"{context.Tabs}{context.InstanceNameInCode} = " +
                    $"global::MonoGameGum.Forms.GraphicalUiElementFormsExtensions.TryGetFrameworkElementByName<{classNameString}>(this.Visual,\"{context.Instance.Name}\");");
            }
            else
            {
                string className = GetClassNameForType(context.Instance, context.VisualApi, context);
                if (className == null) return;
                
                context.StringBuilder.AppendLine(
                    $"{context.Tabs}{context.InstanceNameInCode} = this.Visual?.GetGraphicalUiElementByName(\"{context.Instance.Name}\") as " +
                    $"global::MonoGameGum.GueDeriving.{className};");
            }
        }
        else
        {
            var isStandardElement = ObjectFinder.Self.GetStandardElement(context.Instance.BaseType) != null;
            if(isStandardElement)
            {
                context.StringBuilder.AppendLine(
                    $"{context.Tabs}{context.InstanceNameInCode} = this.GetGraphicalUiElementByName(\"{context.Instance.Name}\") as " +
                    $"global::MonoGameGum.GueDeriving.{GetClassNameForType(context.Instance, context.VisualApi, context)};");

            }
            else
            {
                context.StringBuilder.AppendLine(
                    $"{context.Tabs}{context.InstanceNameInCode} = this.GetGraphicalUiElementByName(\"{context.Instance.Name}\") as " +
                    $"{GetClassNameForType(context.Instance, context.VisualApi, context)};");
            }
        }
    }

    private static void FillWithInstanceInstantiation(CodeGenerationContext context)
    {
        var instance = context.Instance;
        var instanceName = context.InstanceNameInCode;

        var strippedType = instance.BaseType;
        if (strippedType.Contains("/"))
        {
            strippedType = strippedType.Substring(strippedType.LastIndexOf("/") + 1);
        }

        var visualApi = GetVisualApiForInstance(instance, context.Element);

        var tabs = context.Tabs;

        string prefix = "";
        if(context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Skia)
        {
            context.StringBuilder.AppendLine($"{tabs}{instanceName} = new global::SkiaGum.GueDeriving.{GetClassNameForType(instance, visualApi, context)}();");
        }
        else
        {
            var isInstanceStandard = ObjectFinder.Self.GetStandardElement(instance.BaseType) != null;
            if(isInstanceStandard)
            {
                context.StringBuilder.AppendLine($"{tabs}{instanceName} = new global::MonoGameGum.GueDeriving.{GetClassNameForType(instance, visualApi, context)}();");
            }
            else
            {
                // todo - eventually we may want to prefix the expected namespace?
                context.StringBuilder.AppendLine($"{tabs}{instanceName} = new {GetClassNameForType(instance, visualApi, context)}();");
            }

        }

        if (context.CodeOutputProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
        {
            var instanceElement = ObjectFinder.Self.GetElementSave(instance);
            if (instanceElement is StandardElementSave)
            {
                // We could do some kind of caching to speed this up? Fortunately there aren't a lot of ElementSaves in a typical project
                context.StringBuilder.AppendLine($"{tabs}{instanceName}.ElementSave = ObjectFinder.Self.GetStandardElement(\"{instanceElement.Name}\");");
                // Background.AddStatesAndCategoriesRecursivelyToGue(Background.ElementSave);
                context.StringBuilder.AppendLine($"{tabs}if ({instanceName}.ElementSave != null) {instanceName}.AddStatesAndCategoriesRecursivelyToGue({instanceName}.ElementSave);");


                context.StringBuilder.AppendLine($"{tabs}if ({instanceName}.ElementSave != null) {instanceName}.SetInitialState();");
            }
        }

        var shouldSetBinding =
            visualApi == VisualApi.XamarinForms && context.Element.DefaultState.Variables.Any(item => !string.IsNullOrEmpty(item.ExposedAsName) && item.SourceObject == instance.Name);
        // If it's xamarin forms and we have exposed variables, then let's set up binding to this
        if (shouldSetBinding)
        {
            context.StringBuilder.AppendLine($"{tabs}{instanceName}.BindingContext = this;");
        }

        if (visualApi == VisualApi.Gum)
        {
            // Use instance.Name so it is "raw"
            context.StringBuilder.AppendLine($"{tabs}{instanceName}.Name = \"{instance.Name}\";");
        }

        if (visualApi == VisualApi.XamarinForms)
        {
            if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui || context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.XamarinForms)
            {
                // If defined by base, then the automation ID will already be set there, and 
                // Xamarin.Forms doesn't like an automation ID being set 2x
                if (instance.DefinedByBase == false)
                {
                    // Use instance.Name so it is "raw"
                    context.StringBuilder.AppendLine($"{tabs}{instanceName}.AutomationId = \"{instance.Name}\";");
                }

                if (IsOfXamarinFormsType(context.Instance, "ActivityIndicator"))
                {
                    // If we don't do this, it is invisible which is confusing for the user...
                    context.StringBuilder.AppendLine($"{tabs}{instanceName}.IsRunning = true;");
                }
            }
        }
    }

    #endregion

    #region Register (MonoGame)

    static void RegisterRuntimeType(CodeGenerationContext context)
    {

        var outputLibrary = context.CodeOutputProjectSettings.OutputLibrary;

        if (outputLibrary == OutputLibrary.MonoGameForms)
        {
            var builder = context.StringBuilder;

            builder.AppendLine(context.Tabs + "[System.Runtime.CompilerServices.ModuleInitializer]");
            builder.AppendLine(context.Tabs + "public static void RegisterRuntimeType()");
            builder.AppendLine(context.Tabs + "{");
            context.TabCount++;



            var className = CodeGenerator.GetClassNameForType(context.Element, context.VisualApi, context);




            builder.AppendLine(context.Tabs + "var template = new global::MonoGameGum.Forms.VisualTemplate((vm, createForms) =>");
            builder.AppendLine(context.Tabs + "{");
            context.TabCount++;

            var inheritsFromText = context.Element.BaseType == "Text";
            if(inheritsFromText)
            {
                // special case for label:
                builder.AppendLine(context.Tabs + "var visual = new global::MonoGameGum.GueDeriving.TextRuntime();");
            }
            else
            {
                builder.AppendLine(context.Tabs + "var visual = new global::MonoGameGum.GueDeriving.ContainerRuntime();");
            }


            builder.AppendLine(context.Tabs +
                $"var element = ObjectFinder.Self.GetElementSave(\"{context.Element.Name}\");");

            builder.AppendLine(context.Tabs +
                $"element.SetGraphicalUiElement(visual, RenderingLibrary.SystemManagers.Default);");

            builder.AppendLine(context.Tabs +
                $"if(createForms) visual.FormsControlAsObject = new {className}(visual);");



            if (context.Element is ScreenSave)
            {
                builder.AppendLine(context.Tabs + "visual.Width = 0;");
                builder.AppendLine(context.Tabs + "visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;");
                builder.AppendLine(context.Tabs + "visual.Height = 0;");
                builder.AppendLine(context.Tabs + "visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;");
            }

            builder.AppendLine(context.Tabs + "return visual;");

            context.TabCount--;
            builder.AppendLine(context.Tabs + "});");

            builder.AppendLine(context.Tabs +
                $"global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates" +
                $"[typeof({className})] = template;");

            var element = context.Element;
            GetGumFormsTypeFromBehaviors(element, out string? formsType, out ElementBehaviorReference? behaviorReference);
            if (formsType != null)
            {
                var behavior = ObjectFinder.Self.GetBehavior(behaviorReference);
                if (behavior?.DefaultImplementation == element.Name)
                {
                    // This is the default, so let's register it:
                    builder.AppendLine(context.Tabs +
                        $"global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsTemplates[typeof({formsType})] = template;");
                }
            }

            builder.AppendLine(context.Tabs +
                $"ElementSaveExtensions.RegisterGueInstantiation(\"{context.Element.Name}\", () => ");
            builder.AppendLine(context.Tabs + "{");
            context.TabCount++;

            builder.AppendLine(context.Tabs + "var gue = template.CreateContent(null, true) as InteractiveGue;");
            builder.AppendLine(context.Tabs + "return gue;");

            context.TabCount--;
            builder.AppendLine(context.Tabs + "});");

            context.TabCount--;
            builder.AppendLine(context.Tabs + "}");
        }

        else if (outputLibrary == OutputLibrary.MonoGame
            // Other objects could still be instantiating this object by component, so let's register the type no matter
            // how it's generated:
            // && context.CodeOutputProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FindByName
            )
        {
            var builder = context.StringBuilder;

            builder.AppendLine(context.Tabs + "[System.Runtime.CompilerServices.ModuleInitializer]");
            builder.AppendLine(context.Tabs + "public static void RegisterRuntimeType()");
            builder.AppendLine(context.Tabs + "{");
            context.TabCount++;

            var className = CodeGenerator.GetClassNameForType(context.Element, context.VisualApi, context);

            builder.AppendLine(context.Tabs + $"GumRuntime.ElementSaveExtensions.RegisterGueInstantiationType(\"{context.Element.Name}\", typeof({className}));");

            var element = context.Element;
            GetGumFormsTypeFromBehaviors(element, out string? formsType, out ElementBehaviorReference? behaviorReference);
            if (formsType != null)
            {
                var behavior = ObjectFinder.Self.GetBehavior(behaviorReference);
                if (behavior?.DefaultImplementation == element.Name)
                {
                    // This is the default, so let's register it:
                    builder.AppendLine(context.Tabs +
                        $"global::MonoGameGum.Forms.Controls.FrameworkElement.DefaultFormsComponents[typeof({formsType})] = typeof({className});");
                }
            }

            context.TabCount--;
            builder.AppendLine(context.Tabs + "}");
        }
    }

    #endregion

    #region Gum Forms (MonoGame)

    internal static void GetGumFormsTypeFromBehaviors(ElementSave element, out string? formsType, out ElementBehaviorReference? behavior)
    {
        formsType = null;
        behavior = null;
        var behaviors = element?.Behaviors;

        if (behaviors != null)
        {
            foreach (var possibleBehavior in behaviors)
            {
                if (BehaviorGumFormsTypes.ContainsKey(possibleBehavior.BehaviorName))
                {
                    formsType = BehaviorGumFormsTypes[possibleBehavior.BehaviorName];
                    behavior = possibleBehavior;
                    break;
                }
            }
        }
    }

    static Dictionary<string, string> BehaviorGumFormsTypes = new Dictionary<string, string>()
    {
        { "ButtonBehavior", "global::MonoGameGum.Forms.Controls.Button" },
        { "CheckBoxBehavior", "global::MonoGameGum.Forms.Controls.CheckBox" },
        { "ComboBoxBehavior", "global::MonoGameGum.Forms.Controls.ComboBox" },
        { "LabelBehavior", "global::MonoGameGum.Forms.Controls.Label" },
        { "ListBoxBehavior", "global::MonoGameGum.Forms.Controls.ListBox" },
        { "ListBoxItemBehavior", "global::MonoGameGum.Forms.Controls.ListBoxItem" },
        { "MenuBehavior", "global::MonoGameGum.Forms.Controls.Menu" },
        { "MenuItemBehavior", "global::MonoGameGum.Forms.Controls.MenuItem" },
        { "PanelBehavior", "global::MonoGameGum.Forms.Controls.Panel" },
        { "PasswordBoxBehavior", "global::MonoGameGum.Forms.Controls.PasswordBox" },
        { "RadioButtonBehavior", "global::MonoGameGum.Forms.Controls.RadioButton" },
        { "ScrollBarBehavior", "global::MonoGameGum.Forms.Controls.ScrollBar" },
        { "ScrollViewerBehavior", "global::MonoGameGum.Forms.Controls.ScrollViewer" },
        { "SliderBehavior", "global::MonoGameGum.Forms.Controls.Slider" },
        { "SplitterBehavior", "global::MonoGameGum.Forms.Controls.Splitter" },
        { "StackPanelBehavior", "global::MonoGameGum.Forms.Controls.StackPanel" },
        { "TextBoxBehavior", "global::MonoGameGum.Forms.Controls.TextBox" },
        { "WindowBehavior", "global::MonoGameGum.Forms.Window" },
    };

    static void AddGumFormsMembers(CodeGenerationContext context)
    {
        if (context.CodeOutputProjectSettings.OutputLibrary != OutputLibrary.MonoGame) return;

        GetGumFormsTypeFromBehaviors(context.Element, out string? gumFormsType, out _);

        if (gumFormsType == null) return;

        var stringBuilder = context.StringBuilder;

        stringBuilder.AppendLine($"{context.Tabs}public {gumFormsType} FormsControl => FormsControlAsObject as {gumFormsType};");
    }

    #endregion

    #region Position / Size

    private static void ProcessXamarinFormsPositionAndSize(List<VariableSave> variablesToConsider, StateSave state, InstanceSave? instance, ElementSave container, StringBuilder stringBuilder, CodeGenerationContext context)
    {
        //////////////////Early out/////////////////////
        if (container is ScreenSave && instance == null)
        {
            // screens can't be positioned
            return;
        }
        /////////////// End Early Out/////////////

        string variablePrefix = instance?.Name == null ? "" : "" + instance.Name + ".";

        bool setsAny = GetIfStateSetsAnyPositionValues(state, variablePrefix, variablesToConsider);

        InstanceSave? parent = null;
        if (instance != null)
        {
            var parentName = state.GetValueRecursive(instance.Name + ".Parent") as string;
            if (!string.IsNullOrEmpty(parentName))
            {
                parent = container.GetInstance(parentName);
            }
        }

        string? parentType = parent?.BaseType;
        if (parent == null)
        {
            if (instance == null && context.VisualApi == VisualApi.XamarinForms)
            {
                // The most common layout is in a XamForms absolute layout so let's use that. This may also
                // contain values whcih are okay for stacklayout
                parentType = "/AbsoluteLayout";
            }
            else if (container is ScreenSave)
            {
                parentType = "/AbsoluteLayout";
            }
            else
            {
                parentType = container.BaseType;
            }
        }

        // Only run this code if any of the properties are set or if we're in default. Otherwise
        // categorized states may screw up the positioning of an object.
        if (setsAny || state == container.DefaultState)
        {
            var isParentAbsoluteLayout =
                parentType?.EndsWith("/AbsoluteLayout") == true;

            if (!isParentAbsoluteLayout && !string.IsNullOrEmpty(parentType))
            {
                var parentElementSave = ObjectFinder.Self.GetElementSave(parentType);
                if (parentElementSave != null)
                {

                    isParentAbsoluteLayout = IsOfXamarinFormsType(parentElementSave, "AbsoluteLayout");
                }
            }

            if (isParentAbsoluteLayout)
            {
                SetXamarinFormsLayoutPosition(variablesToConsider, state, context, stringBuilder, parentType);
            }
            else //if(parent?.BaseType?.EndsWith("/StackLayout") == true)
            {
                SetNonAbsoluteLayoutPosition(variablesToConsider, state, context, stringBuilder, parentType);
            }
        }

    }



    private static void SetNonAbsoluteLayoutPosition(List<VariableSave> variablesToConsider, StateSave defaultState, CodeGenerationContext context,
        StringBuilder stringBuilder, string parentBaseType)
    {
        var variableFinder = new RecursiveVariableFinder(defaultState);

        var variablePrefix = context.GumVariablePrefix;

        bool setsAny = GetIfStateSetsAnyPositionValues(defaultState, variablePrefix, variablesToConsider);

        var isVariableOwnerAbsoluteLayout = false;
        if (context.Instance != null)
        {
            isVariableOwnerAbsoluteLayout = IsOfXamarinFormsType(context.Instance, "AbsoluteLayout");
        }
        else
        {
            isVariableOwnerAbsoluteLayout = IsOfXamarinFormsType(context.Element, "AbsoluteLayout");
        }
        var isContainedInStackLayout = parentBaseType?.EndsWith("/StackLayout") == true;

        #region Get recursive values for position and size
        var x = variableFinder.GetValue<float>(variablePrefix + "X");
        var y = variableFinder.GetValue<float>(variablePrefix + "Y");
        var width = variableFinder.GetValue<float>(variablePrefix + "Width");
        var height = variableFinder.GetValue<float>(variablePrefix + "Height");

        var xUnits = variableFinder.GetValue<PositionUnitType>(variablePrefix + "XUnits");
        var yUnits = variableFinder.GetValue<PositionUnitType>(variablePrefix + "YUnits");
        var widthUnits = variableFinder.GetValue<DimensionUnitType>(variablePrefix + "WidthUnits");
        var heightUnits = variableFinder.GetValue<DimensionUnitType>(variablePrefix + "HeightUnits");

        var xOrigin = variableFinder.GetValue<HorizontalAlignment>(variablePrefix + "XOrigin");
        var yOrigin = variableFinder.GetValue<VerticalAlignment>(variablePrefix + "YOrigin");

        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "X");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Y");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Width");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Height");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "XUnits");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "YUnits");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "WidthUnits");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "HeightUnits");

        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "XOrigin");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "YOrigin");

        #endregion

        var codePrefix = context.CodePrefix;

        float leftMargin = 0;
        float rightMargin = 0;
        float topMargin = 0;
        float bottomMargin = 0;

        float leftPadding = 0;
        float rightPadding = 0;
        float topPadding = 0;
        float bottomPadding = 0;

        #region Apply WidthUnits

        if (widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
        {
            var multiple = "1.0f";
            if (widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                multiple = "RenderingLibrary.SystemManagers.GlobalFontScale";
            }
            if (AdjustPixelValuesForDensity)
            {
                multiple += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
            }
            stringBuilder.AppendLine(
                $"{codePrefix}.WidthRequest = {width.ToString(CultureInfo.InvariantCulture)}f * {multiple};");
        }

        // In MAUI it seems like we need to -1 the WidthRequest if we are going to depend on the container and use margins:
        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
        {
            if (widthUnits == DimensionUnitType.RelativeToParent)
            {
                stringBuilder.AppendLine($"{codePrefix}.WidthRequest = -1;");
            }
        }

        #endregion

        #region Apply HeightUnits

        if (heightUnits == DimensionUnitType.Absolute || heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
        {
            var multiple = "1.0f";

            if (heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                multiple = "RenderingLibrary.SystemManagers.GlobalFontScale";
            }
            if (AdjustPixelValuesForDensity)
            {
                multiple += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
            }

            stringBuilder.AppendLine(
                $"{codePrefix}.HeightRequest = {height.ToString(CultureInfo.InvariantCulture)}f * {multiple};");
        }
        else if (heightUnits == DimensionUnitType.RelativeToChildren)
        {
            if (isVariableOwnerAbsoluteLayout)
            {
                stringBuilder.AppendLine(context.Tabs + $"Intentional error: The object {context.Instance?.Name ?? "this"} height depends on its children, but it is an absolute layout. Use a StackLayout instead!");
            }
        }

        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
        {
            if (heightUnits == DimensionUnitType.RelativeToParent)
            {
                stringBuilder.AppendLine($"{codePrefix}.HeightRequest = -1;");
            }
        }

        // If it's in a stack layout and it uses a height request of RelativeToParent, generate a compile error. This is not allowed!
        if (heightUnits == DimensionUnitType.RelativeToParent && isContainedInStackLayout)
        {
            stringBuilder.AppendLine(context.Tabs +
                $"Intentional compile error - the object {context.Instance?.Name ?? context.Element.Name} has a parent which is not an absolute layout, but its height is RelativeToContainer. This is not allowed in Xamarin Forms. The parent should be an Absolute layout in this case.");
        }
        #endregion



        #region Apply XUnits

        if (xUnits == PositionUnitType.PixelsFromLeft)
        {
            leftMargin = x;
        }
        if (xUnits == PositionUnitType.PixelsFromLeft && widthUnits == DimensionUnitType.RelativeToParent)
        {
            rightMargin = -width - x;
        }
        if (xUnits == PositionUnitType.PixelsFromCenterX &&
            xOrigin == HorizontalAlignment.Center)
        {
            if (widthUnits == DimensionUnitType.RelativeToParent)
            {
                leftMargin = x - width / 2.0f;
                rightMargin = -x - width / 2.0f;
            }
            else if (widthUnits == DimensionUnitType.Absolute ||
                widthUnits == DimensionUnitType.RelativeToChildren ||
                widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale ||
                widthUnits == DimensionUnitType.MaintainFileAspectRatio ||
                widthUnits == DimensionUnitType.PercentageOfOtherDimension ||
                widthUnits == DimensionUnitType.PercentageOfSourceFile ||
                widthUnits == DimensionUnitType.Ratio
                )
            {
                // Vic says - not sure why but we have to 2x the margin here...
                leftMargin = x * 2;
            }
        }
        else if (xUnits == PositionUnitType.PixelsFromRight && xOrigin == HorizontalAlignment.Right)
        {
            rightMargin = -x;
            if (widthUnits == DimensionUnitType.RelativeToParent)
            {
                leftMargin = -width;
            }
        }

        #endregion

        #region Apply YUnits

        if (yUnits == PositionUnitType.PixelsFromTop)
        {
            topMargin = y;
            if (heightUnits == DimensionUnitType.RelativeToChildren)
            {
                if (isContainedInStackLayout == false)
                {
                    // If it's a stack layout, we don't want to subtract from here.
                    // Update Feb 14, 2022
                    // Not sure why we subtract the height...
                    //bottomMargin = -height - y;
                    // If a Gum object is relative to children with
                    // a height of 10, that means it should be 10 units
                    // bigger than its children, so we should add 10
                    // Update November 16, 2022
                    // Why do we subtract the Y value? Shouldn't it just be
                    // whatever the extra height? Not sure if it should be in all situations
                    // or only when YOrigin is top, so let's be safe and check to only change the
                    // one case:
                    if (yOrigin == VerticalAlignment.Top)
                    {
                        bottomMargin = height;
                    }
                    else
                    {
                        bottomMargin = height - y;
                    }
                }
                else
                {
                    // in a stack layout, so give the margin according to the y origin:
                    if (yOrigin == VerticalAlignment.Top)
                    {
                        // Adding a margin will move the next item after this, but it doesn't make "this" bigger...
                        //bottomMargin = height;
                        //... so instead, we use padding
                        bottomPadding = height;
                    }
                    else if (yOrigin == VerticalAlignment.Center)
                    {
                        // does this need padding...?
                        topMargin += height / 2.0f;
                        bottomMargin = height / 2.0f;
                    }
                    else if (yOrigin == VerticalAlignment.Bottom)
                    {
                        //... or this?
                        topMargin += height;
                    }
                }
            }
        }
        if (yUnits == PositionUnitType.PixelsFromCenterY &&
            yOrigin == VerticalAlignment.Center)
        {
            if (heightUnits == DimensionUnitType.RelativeToParent)
            {
                topMargin = y - height / 2.0f;
                bottomMargin = -y - height / 2.0f;
            }
            // else - should we copy the code above for width units?
        }

        #endregion

        if (isVariableOwnerAbsoluteLayout && heightUnits == DimensionUnitType.RelativeToChildren)
        {
            stringBuilder.AppendLine($"Error: The object {context.Instance?.ToString() ?? context.Element?.ToString()} uses a HeightUnits of RelativeToChildren, but it is an AbsoluteLayout which is not supported in Xamarin.Forms");
        }

        if (setsAny)
        {
            if (AdjustPixelValuesForDensity)
            {
                stringBuilder.AppendLine($"{codePrefix}.Margin = new Thickness(" +
                    $"{leftMargin.ToString(CultureInfo.InvariantCulture)}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, " +
                    $"{topMargin.ToString(CultureInfo.InvariantCulture)}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, " +
                    $"{rightMargin.ToString(CultureInfo.InvariantCulture)}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, " +
                    $"{bottomMargin.ToString(CultureInfo.InvariantCulture)}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density);");

            }
            else
            {
                stringBuilder.AppendLine($"{codePrefix}.Margin = new Thickness(" +
                    $"{leftMargin.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{topMargin.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{rightMargin.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{bottomMargin.ToString(CultureInfo.InvariantCulture)});");
            }
        }

        #region Write Padding
        var hasPadding = topPadding != 0 || leftPadding != 0 || rightPadding != 0 || bottomPadding != 0;
        if (hasPadding)
        {
            stringBuilder.AppendLine($"{codePrefix}.Padding = new Thickness(" +
                $"{leftPadding.ToString(CultureInfo.InvariantCulture)}, " +
                $"{topPadding.ToString(CultureInfo.InvariantCulture)}, " +
                $"{rightPadding.ToString(CultureInfo.InvariantCulture)}, " +
                $"{bottomPadding.ToString(CultureInfo.InvariantCulture)});");
        }
        #endregion

        #region Write HorizontalOptions
        if (widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.RelativeToChildren || widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
        {
            if (setsAny)
            {
                if (xUnits == PositionUnitType.PixelsFromCenterX && xOrigin == HorizontalAlignment.Center)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.HorizontalOptions = LayoutOptions.Center;");
                }
                else if (xUnits == PositionUnitType.PixelsFromRight && xOrigin == HorizontalAlignment.Right)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.HorizontalOptions = LayoutOptions.End;");
                }
                else
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.HorizontalOptions = LayoutOptions.Start;");

                }
            }
        }
        else if (widthUnits == DimensionUnitType.RelativeToParent ||
            widthUnits == DimensionUnitType.PercentageOfParent)
        {
            stringBuilder.AppendLine(
                $"{codePrefix}.HorizontalOptions = LayoutOptions.Fill;");
        }

        #endregion

        #region Write Vertical Options

        if (heightUnits == DimensionUnitType.Absolute || heightUnits == DimensionUnitType.RelativeToChildren || heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
        {
            if (yUnits == PositionUnitType.PixelsFromCenterY && xOrigin == HorizontalAlignment.Center)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.VerticalOptions = LayoutOptions.Center;");
            }
            else
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.VerticalOptions = LayoutOptions.Start;");
            }
        }
        else if (heightUnits == DimensionUnitType.RelativeToParent ||
            heightUnits == DimensionUnitType.PercentageOfParent)
        {
            stringBuilder.AppendLine(
                $"{codePrefix}.VerticalOptions = LayoutOptions.Fill;");
        }

        #endregion

        bool isVariableOwnerSkiaGumCanvasView = IsVariableOwnerSkiaGumCanvasView(ref context);

        if (isVariableOwnerSkiaGumCanvasView)
        {
            var generateAfterAutoSizeChanged = false;
            if (heightUnits == DimensionUnitType.RelativeToChildren)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.AutoSizeHeightAccordingToContents = true;");

                if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
                {
                    // In maui if it's not at least 1 pixel height, it won't ever call its update call so it never gets resized
                    // We may want to have some kind of explicit call that resizes it outside of rendering, but this hooks into the
                    // existing system, so let's just do that:
                    stringBuilder.AppendLine($"{codePrefix}.HeightRequest = 1;");

                    // On IOS there's a bug where the page needs to be forcefully refreshed to handle the resize. This is a workaround. Not sure if we'll ever test this in the future to see  if it's fixed:
                    // This is Tula-specific so this will have to be generalized if anyone else uses this:
                    generateAfterAutoSizeChanged = true;
                }
            }
            if (widthUnits == DimensionUnitType.RelativeToChildren)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.AutoSizeWidthAccordingToContents = true;");
                if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
                {
                    stringBuilder.Append($"{codePrefix}.WidthRequest = 1;");
                    generateAfterAutoSizeChanged = true;

                }
            }
            if (generateAfterAutoSizeChanged)
            {
                stringBuilder.AppendLine($"{codePrefix}.AfterAutoSizeChanged += () => (BioCheck.DependencyInjection.DiCommon.TryGet<BioCheck.Managers.BioCheckNavigation>().NavigationStack.LastOrDefault() as BioCheck.Pages.BioCheckPage).CallInvalidateMeasure();");
            }
        }
    }

    #region Const values
    const string WidthProportionalFlag = "AbsoluteLayoutFlags.WidthProportional";
    const string HeightProportionalFlag = "AbsoluteLayoutFlags.HeightProportional";
    const string XProportionalFlag = "AbsoluteLayoutFlags.XProportional";
    const string YProportionalFlag = "AbsoluteLayoutFlags.YProportional";
    #endregion

    private static void SetXamarinFormsLayoutPosition(List<VariableSave> variablesToConsider, StateSave state, CodeGenerationContext context, StringBuilder stringBuilder, string parentBaseType)
    {
        var variableFinder = new RecursiveVariableFinder(state);

        var variablePrefix = context.GumVariablePrefix;

        // October 18, 2022
        // If an instance has
        // its position controlled
        // by a state, then the state
        // is responsible for doing the
        // layout. However, if we always
        // append the bounds text, then we
        // will overwrite the state setting.
        // Therefore, we should only do it if
        // a position-related value is set:
        bool setsAny = GetIfStateSetsAnyPositionValues(state, variablePrefix, variablesToConsider);

        var isVariableOwnerAbsoluteLayout = false;
        if (context.Instance != null)
        {
            isVariableOwnerAbsoluteLayout = IsOfXamarinFormsType(context.Instance, "AbsoluteLayout");
        }
        else
        {
            isVariableOwnerAbsoluteLayout = IsOfXamarinFormsType(context.Element, "AbsoluteLayout");
        }

        #region Get recursive values for position and size

        var x = variableFinder.GetValue<float>(variablePrefix + "X");
        var y = variableFinder.GetValue<float>(variablePrefix + "Y");
        var width = variableFinder.GetValue<float>(variablePrefix + "Width");
        var height = variableFinder.GetValue<float>(variablePrefix + "Height");
        var originalHeight = height;

        var xUnits = variableFinder.GetValue<PositionUnitType>(variablePrefix + "XUnits");
        var yUnits = variableFinder.GetValue<PositionUnitType>(variablePrefix + "YUnits");
        var widthUnits = variableFinder.GetValue<DimensionUnitType>(variablePrefix + "WidthUnits");
        var heightUnits = variableFinder.GetValue<DimensionUnitType>(variablePrefix + "HeightUnits");

        var xOrigin = variableFinder.GetValue<HorizontalAlignment>(variablePrefix + "XOrigin");
        var yOrigin = variableFinder.GetValue<VerticalAlignment>(variablePrefix + "YOrigin");

        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "X");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Y");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Width");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Height");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "XUnits");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "YUnits");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "WidthUnits");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "HeightUnits");

        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "XOrigin");
        variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "YOrigin");
        #endregion

        var codePrefix = context.CodePrefix;

        var proportionalFlags = new HashSet<string>();

        int leftMargin = 0;
        int topMargin = 0;
        int rightMargin = 0;
        int bottomMargin = 0;

        float leftPadding = 0;
        float rightPadding = 0;
        float topPadding = 0;
        float bottomPadding = 0;

        #region Apply WidthUnits

        if (widthUnits == DimensionUnitType.PercentageOfParent)
        {
            width /= 100.0f;
            proportionalFlags.Add(WidthProportionalFlag);
        }
        else if (widthUnits == DimensionUnitType.RelativeToParent)
        {
            if (xOrigin == HorizontalAlignment.Center)
            {
                // we'll achieve margins with offsets
                leftMargin = MathFunctions.RoundToInt(x - width / 2);
                rightMargin = MathFunctions.RoundToInt(-x - width / 2);
            }
            else if (xOrigin == HorizontalAlignment.Right)
            {
                rightMargin = MathFunctions.RoundToInt(-x - width);
                leftMargin = MathFunctions.RoundToInt(x - width);
            }
            else
            {
                // we'll achieve margins with offsets
                rightMargin = MathFunctions.RoundToInt(-x - width);
            }
            width = 1;
            proportionalFlags.Add(WidthProportionalFlag);
        }
        else if (widthUnits == DimensionUnitType.RelativeToChildren)
        {
            // in this case we want to auto-size, which is what -1 indicates
            width = -1;
        }

        #endregion

        #region Apply HeightUnits

        if (heightUnits == DimensionUnitType.PercentageOfParent)
        {
            height /= 100.0f;
            proportionalFlags.Add(HeightProportionalFlag);
        }
        else if (heightUnits == DimensionUnitType.RelativeToParent)
        {
            // just like width units, achieve this with margins:
            if (yOrigin == VerticalAlignment.Center)
            {
                topMargin = MathFunctions.RoundToInt(y - height / 2);
                bottomMargin = MathFunctions.RoundToInt(-y - height / 2);
            }
            else
            {
                bottomMargin = MathFunctions.RoundToInt(-y - height);
            }
            height = 1;
            proportionalFlags.Add(HeightProportionalFlag);
        }
        else if (heightUnits == DimensionUnitType.RelativeToChildren)
        {
            if (isVariableOwnerAbsoluteLayout)
            {
                stringBuilder.AppendLine(context.Tabs + $"Intentional error: The object {context.Instance?.Name ?? "this"} height depends on its children, but it is an absolute layout. Use a StackLayout instead!");
            }
            // see above on width relative to container for information
            height = -1;
        }

        #endregion

        var isContainedInStackLayout = parentBaseType?.EndsWith("/StackLayout") == true;


        #region Apply XUnits

        // special case
        // If we're using the center with x=0 we'll pretend it's the same as 50% 
        if (xUnits == PositionUnitType.PixelsFromCenterX
            // why does the width unit even matter? Should be the same regardless of width unit...
            //widthUnits == DimensionUnitType.Absolute && 
            )
        {
            // Always do this (instead of only when X==0), because the margins will offset from center
            //if (x == 0)
            // treat it like it's 50%:
            if (xOrigin == HorizontalAlignment.Left)
            {
                if (widthUnits == DimensionUnitType.Absolute)
                {
                    leftMargin += (int)(width / 2.0f);
                    rightMargin -= (int)(width / 2.0f);
                }
            }
            else if (xOrigin == HorizontalAlignment.Right)
            {
                if (widthUnits == DimensionUnitType.Absolute)
                {
                    leftMargin -= (int)(width / 2.0f);
                    rightMargin += (int)(width / 2.0f);
                }
            }

            // we know the x units so just add:
            leftMargin += (int)x;
            rightMargin -= (int)x;

            // add X before setting X to .5

            x = .5f;
            proportionalFlags.Add(XProportionalFlag);
        }
        // Xamarin forms uses a weird anchoring system to combine both position and anchor into one value. Gum splits those into two values
        // We need to convert from the gum units to xamforms units:
        // for now assume it's all %'s:

        else if (xUnits == PositionUnitType.PercentageWidth)
        {
            x /= 100.0f;

            if (widthUnits == DimensionUnitType.PercentageOfParent)
            {
                var adjustedCanvasWidth = 1 - width;
                if (adjustedCanvasWidth > 0)
                {
                    x /= adjustedCanvasWidth;
                }
            }
            proportionalFlags.Add(XProportionalFlag);
        }
        else if (xUnits == PositionUnitType.PixelsFromLeft)
        {
            if (widthUnits == DimensionUnitType.RelativeToParent)
            {
                leftMargin = MathFunctions.RoundToInt(x);
                x = 0;
            }

            if (xOrigin == HorizontalAlignment.Center)
            {
                if (widthUnits == DimensionUnitType.Absolute)
                {
                    leftMargin -= (int)(width / 2.0f);
                    rightMargin += (int)(width / 2.0f);
                }
            }
            else if (xOrigin == HorizontalAlignment.Right)
            {
                if (widthUnits == DimensionUnitType.Absolute)
                {
                    leftMargin -= (int)(width);
                    rightMargin += (int)(width);
                }
            }
        }
        else if (xUnits == PositionUnitType.PixelsFromRight)
        {
            if (xOrigin == HorizontalAlignment.Right)
            {
                rightMargin = MathFunctions.RoundToInt(-x);
            }
            else if (xOrigin == HorizontalAlignment.Center)
            {
                if (widthUnits == DimensionUnitType.Absolute)
                {
                    rightMargin = MathFunctions.RoundToInt(-x - width / 2);
                    leftMargin = MathFunctions.RoundToInt(x + width / 2);
                }
            }
            else if (xOrigin == HorizontalAlignment.Left)
            {
                if (widthUnits == DimensionUnitType.Absolute)
                {
                    rightMargin = MathFunctions.RoundToInt(-x - width);
                    leftMargin = MathFunctions.RoundToInt(x + width);
                }
            }
            x = 1;
            proportionalFlags.Add(XProportionalFlag);
        }

        #endregion

        #region Apply YUnits

        if (yUnits == PositionUnitType.PixelsFromTop)
        {
            if (heightUnits == DimensionUnitType.RelativeToParent)
            {
                topMargin = MathFunctions.RoundToInt(y);
                y = 0;

                // originalHeight greater than 0 means we are going to make this bigger, but not have the 
                // inner controls be abel to use that space
                if (yOrigin == VerticalAlignment.Top && originalHeight > 0)
                {
                    bottomPadding = originalHeight;
                }

            }
            else if (heightUnits == DimensionUnitType.RelativeToChildren)
            {
                if (yOrigin == VerticalAlignment.Top && originalHeight > 0)
                {
                    bottomPadding = originalHeight;
                }

            }
        }
        else if (yUnits == PositionUnitType.PixelsFromCenterY)
        {
            if (yOrigin == VerticalAlignment.Center)
            {
                // If relative to container, it's already handled up above
                if (heightUnits != DimensionUnitType.RelativeToParent)
                {
                    topMargin = MathFunctions.RoundToInt(y);
                    bottomMargin = MathFunctions.RoundToInt(-y);
                }
                y = .5f;
                proportionalFlags.Add(YProportionalFlag);
            }
            else if (yOrigin == VerticalAlignment.Top)
            {
                // the margin should be the height of this / 2
                topMargin = MathFunctions.RoundToInt(y + height / 2.0f);
                bottomMargin = -topMargin;
                y = .5f;
                proportionalFlags.Add(YProportionalFlag);
            }
            else if (yOrigin == VerticalAlignment.Top)
            {
                // the margin should be the height of this / 2
                topMargin = MathFunctions.RoundToInt(y - height / 2.0f);
                y = .5f;
                proportionalFlags.Add(YProportionalFlag);
            }
        }

        else if (yUnits == PositionUnitType.PercentageHeight)
        {
            y /= 100.0f;
            var adjustedCanvasHeight = 1 - height;
            if (adjustedCanvasHeight > 0)
            {
                y /= adjustedCanvasHeight;
            }
            proportionalFlags.Add(YProportionalFlag);
        }
        else if (yUnits == PositionUnitType.PixelsFromBottom)
        {
            if (yOrigin == VerticalAlignment.Bottom)
            {
                bottomMargin = MathFunctions.RoundToInt(-y);
                y = 1;
                proportionalFlags.Add(YProportionalFlag);
            }
            else
            {
                // We could be smarter about this but we'll add this
                // when it's needed.
                y += CanvasHeight;
            }
        }


        #endregion

        #region Write Padding

        var hasPadding = topPadding != 0 || leftPadding != 0 || rightPadding != 0 || bottomPadding != 0;
        if (hasPadding)
        {
            stringBuilder.AppendLine($"{codePrefix}.Padding = new Thickness(" +
                $"{leftPadding.ToString(CultureInfo.InvariantCulture)}, " +
                $"{topPadding.ToString(CultureInfo.InvariantCulture)}, " +
                $"{rightPadding.ToString(CultureInfo.InvariantCulture)}, " +
                $"{bottomPadding.ToString(CultureInfo.InvariantCulture)});");
        }

        #endregion

        #region Write HorizontalOptions

        //If the object is width proportional, then it must use a .HorizontalOptions = LayoutOptions.Fill; or else the proportional width won't apply
        if (proportionalFlags.Contains(WidthProportionalFlag))
        {
            stringBuilder.AppendLine($"{context.CodePrefix}.HorizontalOptions = LayoutOptions.Fill;");
        }
        else if (widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.RelativeToChildren || widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
        {
            // See setsAny variable definition for discussion about this check
            if (setsAny)
            {
                if (xUnits == PositionUnitType.PixelsFromCenterX && xOrigin == HorizontalAlignment.Center)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.HorizontalOptions = LayoutOptions.Center;");
                }
                else if (xUnits == PositionUnitType.PixelsFromRight && xOrigin == HorizontalAlignment.Right)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.HorizontalOptions = LayoutOptions.End;");
                }
                else
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.HorizontalOptions = LayoutOptions.Start;");

                }
            }
        }
        else if (widthUnits == DimensionUnitType.RelativeToParent ||
            widthUnits == DimensionUnitType.PercentageOfParent)
        {
            stringBuilder.AppendLine(
                $"{codePrefix}.HorizontalOptions = LayoutOptions.Fill;");
        }

        #endregion

        #region Write Vertical Options

        if (heightUnits == DimensionUnitType.Absolute || heightUnits == DimensionUnitType.RelativeToChildren || heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
        {
            if (yUnits == PositionUnitType.PixelsFromCenterY && xOrigin == HorizontalAlignment.Center)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.VerticalOptions = LayoutOptions.Center;");
            }
            else if (yUnits == PositionUnitType.PixelsFromBottom && yOrigin == VerticalAlignment.Bottom)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.VerticalOptions = LayoutOptions.End;");
            }
            else
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.VerticalOptions = LayoutOptions.Start;");
            }
        }
        else if (heightUnits == DimensionUnitType.RelativeToParent ||
            heightUnits == DimensionUnitType.PercentageOfParent)
        {
            stringBuilder.AppendLine(
                $"{codePrefix}.VerticalOptions = LayoutOptions.Fill;");
        }

        #endregion

        var xString = x.ToString(CultureInfo.InvariantCulture) + "f";
        var yString = y.ToString(CultureInfo.InvariantCulture) + "f";
        var widthString = width.ToString(CultureInfo.InvariantCulture) + "f";
        var heightString = height.ToString(CultureInfo.InvariantCulture) + "f";

        if (heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
        {
            heightString = $"({heightString} * RenderingLibrary.SystemManagers.GlobalFontScale)";
        }
        if (widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
        {
            widthString = $"({widthString} * RenderingLibrary.SystemManagers.GlobalFontScale)";
        }

        // When using AbsoluteLayout in XamarinForms, adding a margin will actually shrink the object. Therefore, if using margin to 
        // position an object relative to its right or bottom edges, the object's width and height should be increased by the offset amount.

        if (xUnits == PositionUnitType.PixelsFromRight && xOrigin == HorizontalAlignment.Right)
        {
            if (widthUnits == DimensionUnitType.RelativeToChildren)
            {
                widthString = $"({widthString})";
            }
            // Vic asks - do we still want to do this? Isn't this handled by the margins above:
            //else
            else if (widthUnits != DimensionUnitType.RelativeToParent)
            {
                widthString = $"({widthString} + {rightMargin})";
            }
        }
        if (yUnits == PositionUnitType.PixelsFromBottom && yOrigin == VerticalAlignment.Bottom)
        {
            if (heightUnits == DimensionUnitType.RelativeToChildren)
            {
                heightString = $"({heightString})";
            }
            else
            {
                heightString = $"({heightString} + {bottomMargin})";
            }
        }

        if (AdjustPixelValuesForDensity)
        {
            if (proportionalFlags.Contains(XProportionalFlag) == false)
            {
                xString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
            }
            if (proportionalFlags.Contains(YProportionalFlag) == false)
            {
                yString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
            }
            if (proportionalFlags.Contains(WidthProportionalFlag) == false && widthUnits != DimensionUnitType.RelativeToChildren)
            {
                widthString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
            }
            if (proportionalFlags.Contains(HeightProportionalFlag) == false && heightUnits != DimensionUnitType.RelativeToChildren)
            {
                heightString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
            }
        }

        string rectangleName = context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.XamarinForms
            ? "Rectangle"
            : "Rect";

        string boundsText =
            $"{ToTabs(context.TabCount)}AbsoluteLayout.SetLayoutBounds({context.CodePrefixNoTabs}, new {rectangleName}({xString}, {yString}, {widthString}, {heightString} ));";
        string flagsText = null;

        if (proportionalFlags.Count == 0)
        {
            // A default state could set values, we override it but don't use any porportional, we probably still want to adjust here...
            proportionalFlags.Add("AbsoluteLayoutFlags.None");
        }

        if (proportionalFlags.Count > 0)
        {
            string flagsArguments = null;
            int i = 0;
            foreach (var flag in proportionalFlags)
            {
                if (i > 0)
                {
                    flagsArguments += " | ";
                }
                flagsArguments += flag;
                i++;
            }
            flagsText = $"{ToTabs(context.TabCount)}AbsoluteLayout.SetLayoutFlags({context.CodePrefixNoTabs}, {flagsArguments});";
        }

        // See variable definition for discussion:
        if (setsAny)
        {
            if (string.IsNullOrWhiteSpace(flagsText))
            {
                stringBuilder.AppendLine(boundsText);
            }
            else
            {
                stringBuilder.AppendLine($"{boundsText}\n{flagsText}");
            }
        }

        // not sure why these apply even though we're using values on the AbsoluteLayout
        if (!proportionalFlags.Contains(WidthProportionalFlag) && (widthUnits == DimensionUnitType.RelativeToParent || widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale))
        {
            string rightSide;

            if (widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                rightSide = $"{width.ToString(CultureInfo.InvariantCulture)}f * RenderingLibrary.SystemManagers.GlobalFontScale";
            }
            else
            {
                rightSide = $"{width.ToString(CultureInfo.InvariantCulture)}f";
            }

            if (AdjustPixelValuesForDensity)
            {
                rightSide += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
            }

            stringBuilder.AppendLine($"{context.CodePrefix}.WidthRequest = {rightSide};");

        }
        if (!proportionalFlags.Contains(HeightProportionalFlag) && (heightUnits == DimensionUnitType.RelativeToParent || heightUnits == DimensionUnitType.Absolute || heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale))
        {
            string rightSide;

            if (heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                rightSide = $"{height.ToString(CultureInfo.InvariantCulture)}f * RenderingLibrary.SystemManagers.GlobalFontScale";
            }
            else
            {
                rightSide = $"{height.ToString(CultureInfo.InvariantCulture)}f";
            }

            if (AdjustPixelValuesForDensity)
            {
                rightSide += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
            }

            stringBuilder.AppendLine($"{context.CodePrefix}.HeightRequest = {rightSide};");
        }

        // If there are no margins, we should still explicitly set them. Otherwise, states that modify margins will not properly be set back
        //if (leftMargin != 0 || rightMargin != 0 || topMargin != 0 || bottomMargin != 0)
        if (AdjustPixelValuesForDensity)
        {
            stringBuilder.AppendLine($"{context.CodePrefix}.Margin = new Thickness({leftMargin}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, {topMargin}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, {rightMargin}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, {bottomMargin}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density);");
        }
        else
        {
            stringBuilder.AppendLine($"{context.CodePrefix}.Margin = new Thickness({leftMargin}, {topMargin}, {rightMargin}, {bottomMargin});");
        }
        // should we do the same to vertical? Maybe, but waiting for a natural use case to test it

        bool isVariableOwnerSkiaGumCanvasView = IsVariableOwnerSkiaGumCanvasView(ref context);

        if (isVariableOwnerSkiaGumCanvasView)
        {
            var generateAfterAutoSizeChanged = false;
            if (heightUnits == DimensionUnitType.RelativeToChildren)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.AutoSizeHeightAccordingToContents = true;");
                if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
                {
                    stringBuilder.AppendLine($"{codePrefix}.HeightRequest = 1;");
                    generateAfterAutoSizeChanged = true;
                }
            }
            if (widthUnits == DimensionUnitType.RelativeToChildren)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.AutoSizeWidthAccordingToContents = true;");
                if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
                {
                    stringBuilder.AppendLine($"{codePrefix}.HeightRequest = 1;");
                    generateAfterAutoSizeChanged = true;
                }
            }

            if (generateAfterAutoSizeChanged)
            {
                stringBuilder.AppendLine($"{codePrefix}.AfterAutoSizeChanged += () => (BioCheck.DependencyInjection.DiCommon.TryGet<BioCheck.Managers.BioCheckNavigation>().NavigationStack.LastOrDefault() as BioCheck.Pages.BioCheckPage).CallInvalidateMeasure();");
            }
        }

        // In MAUI it seems like we need to -1 the WidthRequest if we are going to depend on the container and use margins:
        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
        {
            if (widthUnits == DimensionUnitType.RelativeToParent)
            {
                stringBuilder.AppendLine($"{codePrefix}.WidthRequest = -1;");
            }
            if (heightUnits == DimensionUnitType.RelativeToParent)
            {
                stringBuilder.AppendLine($"{codePrefix}.HeightRequest = -1;");
            }
        }
    }

    private static bool IsVariableOwnerSkiaGumCanvasView(ref CodeGenerationContext context)
    {
        var isVariableOwnerSkiaGumCanvasView = false;
        if (context.Instance != null)
        {
            isVariableOwnerSkiaGumCanvasView = context.Instance.BaseType?.EndsWith("/SkiaGumCanvasView") == true;

            if (!isVariableOwnerSkiaGumCanvasView)
            {
                var element = ObjectFinder.Self.GetElementSave(context.Instance.BaseType);
                return IsElementSkiaGumCanvasView(element);
            }
        }
        else
        {
            isVariableOwnerSkiaGumCanvasView = IsElementSkiaGumCanvasView(context.Element);
        }

        return isVariableOwnerSkiaGumCanvasView;
    }

    private static bool IsElementSkiaGumCanvasView(ElementSave element)
    {
        if (element.BaseType?.EndsWith("/SkiaGumCanvasView") == true)
        {
            return true;
        }
        else
        {
            return ObjectFinder.Self.GetBaseElements(element)
                .Any(item => item.BaseType?.EndsWith("/SkiaGumCanvasView") == true);
        }

    }


    #endregion

    #region Parent

    private static void FillWithParentAssignments(CodeGenerationContext context)
    {
        var container = context.Element;
        var instance = context.Instance;
        var instanceNameInCode = context.GetInstanceNameInCode(instance);
        //context.Instance, context.Element, context.StringBuilder, context.TabCount, context.CodeOutputProjectSettings;

        // Some history on this:
        // Initially parent assignment
        // was mixed in with normal variable
        // assignment. This was separated out
        // into its own method on July 14 so that
        // a derived element can assign parent and
        // children elements all at once. To do this, 
        // we'll get the recursive parent value instead
        // of relying on top-level variables:

        var defaultState = container.DefaultState;

        var rfv = new RecursiveVariableFinder(defaultState);

        var parentVariable = rfv.GetVariable($"{instance.Name}.Parent");

        //var parentVariables = GetVariablesForValueAssignmentCode(instance, container)
        //    // make "Parent" first
        //    // .. actually we need to make parent last so that it can properly assign parent on scrollables
        //    .Where(item => item.GetRootName() == "Parent")
        //    .ToList();

        VisualApi visualApi = GetVisualApiForInstance(instance, container);


        //FillWithVariableAssignments(instance, container, stringBuilder, tabCount, parentVariables);

        var parentValue = parentVariable?.Value as string;
        var parentInstance = parentValue != null
            ? ObjectFinder.Self.GetInstanceRecursively(container, parentValue)
            : (InstanceSave)null;
        var hasParent = parentInstance != null;
        //container.GetInstance(parentValue) != null;

        if (hasParent)
        {
            var codeLine = GetCodeLine(parentVariable, container, visualApi, defaultState, context);


            // the line of code could be " ", a string with a space. This happens
            // if we want to skip a variable so we dont return null or empty.
            // But we also don't want a ton of spaces generated.
            if (!string.IsNullOrWhiteSpace(codeLine))
            {
                context.StringBuilder.AppendLine(context.Tabs + codeLine);
            }
        }

        // For scrollable GumContainers we need to have the parent assigned *after* the AbsoluteLayout rectangle:
        #region If no parent, add to "this"


        if (!hasParent)
        {
            if (visualApi == VisualApi.Gum)
            {
                // add it to "this"
                if (container is ScreenSave)
                {
                    if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
                    {
                        context.StringBuilder.AppendLine($"{context.Tabs}this.AddChild({context.GetInstanceNameInCode(instance)});");
                    }
                    else if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame)
                    {
                        // If it's a screen it may have children, or it may not. We just don't know, so we need to check

                        context.StringBuilder.AppendLine($"{context.Tabs}if(this.Children != null) this.Children.Add({context.GetInstanceNameInCode(instance)});");
                        context.StringBuilder.AppendLine($"{context.Tabs}else this.WhatThisContains.Add({context.GetInstanceNameInCode(instance)});");
                    }
                    else
                    {
                        context.StringBuilder.AppendLine($"{context.Tabs}this.WhatThisContains.Add({context.GetInstanceNameInCode(instance)});");
                    }
                }
                else
                {
                    if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
                    {
                        context.StringBuilder.AppendLine($"{context.Tabs}this.AddChild({context.GetInstanceNameInCode(instance)});");
                    }
                    else
                    {
                        context.StringBuilder.AppendLine($"{context.Tabs}this.Children.Add({context.GetInstanceNameInCode(instance)});");
                    }
                }
            }
            else // forms
            {
                var instanceBaseType = instance.BaseType;
                var isContainerStackLayout = IsOfXamarinFormsType(container, "StackLayout");
                var isContainerScrollView = container.BaseType?.EndsWith("/ScrollView") == true;
                var isContainerAbsoluteLayout = IsOfXamarinFormsType(container, "AbsoluteLayout") == true;

                var instanceName = instance.Name;

                if (instanceBaseType.EndsWith("/GumCollectionView"))
                {
                    context.StringBuilder.AppendLine($"{context.Tabs}var tempFor{instanceNameInCode} = GumScrollBar.CreateScrollableAbsoluteLayout({instanceNameInCode}, ScrollableLayoutParentPlacement.Free);");
                    instanceName = $"tempFor{instanceNameInCode}";
                }

                if (isContainerStackLayout || isContainerAbsoluteLayout)
                {
                    context.StringBuilder.AppendLine($"{context.Tabs}this.Children.Add({instanceNameInCode});");
                }
                else if (DoesTypeHaveContent(container.BaseType))
                {
                    context.StringBuilder.Append($"{context.Tabs}this.Content = {instanceNameInCode};");
                }
                else
                {
                    context.StringBuilder.AppendLine($"{context.Tabs}MainLayout.Children.Add({instanceNameInCode});");
                }
            }
        }

        #endregion
    }

    private static void GenerateAddToParentsMethod(CodeGenerationContext context)
    {
        var isDerived = DoesElementInheritFromCodeGeneratedElement(context.Element, context.CodeOutputProjectSettings);

        var virtualOrOverride = isDerived
            ? "override"
            : "virtual";

        var line = $"protected {virtualOrOverride} void AssignParents()";
        context.StringBuilder.AppendLine(context.Tabs + line);
        context.StringBuilder.AppendLine(context.Tabs + "{");
        context.TabCount++;

        if (isDerived)
        {
            context.StringBuilder.AppendLine(context.Tabs + "// Intentionally do not call base.AssignParents so that this class can determine the addition of order");
        }

        foreach (var instance in context.Element.Instances)
        {
            context.Instance = instance;
            FillWithParentAssignments(context);
        }
        context.Instance = null;

        context.TabCount--;
        context.StringBuilder.AppendLine(context.Tabs + "}");
    }

    #endregion

    #region Constructors

    private static void GenerateConstructors(CodeGenerationContext context)
    {
        var element = context.Element;
        var visualApi = context.VisualApi;
        var stringBuilder = context.StringBuilder;
        var projectSettings = context.CodeOutputProjectSettings;

        var elementClassName = GetClassNameForType(element, visualApi, context);

        if (visualApi == VisualApi.Gum)
        {
            if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
            {
                stringBuilder.AppendLine(context.Tabs + $"public {elementClassName}(InteractiveGue visual) : base(visual)");
                stringBuilder.AppendLine(context.Tabs + "{");

                context.TabCount++;

                if (context.CodeOutputProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
                {
                    if (!DoesElementInheritFromCodeGeneratedElement(element, projectSettings))
                    {
                        stringBuilder.AppendLine(context.Tabs + "InitializeInstances();");
                    }
                    // as mentioned below, if not fully in code then this is handled in AfterFullCreation
                    stringBuilder.AppendLine(context.Tabs + "CustomInitialize();");
                }


                context.TabCount--;
                stringBuilder.AppendLine(context.Tabs + "}");

            }

            #region Constructor Header

            if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame)
            {
                // MonoGame expects 0 or 2-arg constructors. We'll start with 0 for now, and eventually go to 2 if we need Forms support
                // Update November 3, 2024 - there's code that is generated that expects fullInstantiation. Also the docs recommend a 2-arg
                // approach so let's do that:
                stringBuilder.AppendLine(context.Tabs + $"public {elementClassName}(bool fullInstantiation = true, bool tryCreateFormsObject = true)");
            }
            else if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
            {
                if (context.CodeOutputProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
                {
                    stringBuilder.AppendLine(context.Tabs + $"public {elementClassName}() : base(new ContainerRuntime())");
                }
                else
                {
                    stringBuilder.AppendLine(context.Tabs + $"public {elementClassName}()");
                }
            }
            else
            {
                stringBuilder.AppendLine(context.Tabs + $"public {elementClassName}(bool fullInstantiation = true)");
            }

            stringBuilder.AppendLine(context.Tabs + "{");
            context.TabCount++;

            #endregion

            #region Gum-required constructor code

            var shouldGenerateFullInstantiation =
                context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame ||
                context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Skia;

            if (shouldGenerateFullInstantiation)
            {
                stringBuilder.AppendLine(context.Tabs + "if(fullInstantiation)");
                stringBuilder.AppendLine(context.Tabs + "{");
                context.TabCount++;
            }
            if (projectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
            {
                if (element.BaseType == "Container" &&
                    // In MonoGame the Container is a ContainerRuntime which handles this already
                    (projectSettings.OutputLibrary != OutputLibrary.MonoGame && projectSettings.OutputLibrary != OutputLibrary.MonoGameForms))
                {
                    stringBuilder.AppendLine(context.Tabs + "this.SetContainedObject(new InvisibleRenderable());");
                }
            }
            else
            {
                if (projectSettings.OutputLibrary == OutputLibrary.MonoGame)
                {

                    context.StringBuilder.AppendLine(context.Tabs +
                        $"var element = ObjectFinder.Self.GetElementSave(\"{element.Name}\");");

                    context.StringBuilder.AppendLine(context.Tabs +
                        "element?.SetGraphicalUiElement(this, global::RenderingLibrary.SystemManagers.Default);");

                    // March 8, 2025
                    // This is not needed
                    // because it's called
                    // internally in element.SetGraphicalUiElement
                    // I'm not sure why I put it here in the first place...
                    //context.StringBuilder.AppendLine(context.Tabs +
                    //"AfterFullCreation();");
                }
                else if (projectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
                {
                    // This is handled in the register function 
                }
            }

            if (shouldGenerateFullInstantiation)
            {
                context.TabCount--;
                stringBuilder.AppendLine(context.Tabs + "}");

            }

            stringBuilder.AppendLine();

            #endregion
        }
        else // xamarin forms
        {
            #region Constructor Header

            stringBuilder.AppendLine(context.Tabs + $"public {elementClassName}(bool fullInstantiation = true)");

            stringBuilder.AppendLine(context.TabCount + "{");
            context.TabCount++;

            #endregion

            #region XamarinForms-required constructor code

            stringBuilder.AppendLine(context.Tabs + "var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended;");
            stringBuilder.AppendLine(context.Tabs + "GraphicalUiElement.IsAllLayoutSuspended = true;");

            var elementBaseType = element?.BaseType;
            var baseElements = element != null ? ObjectFinder.Self.GetBaseElements(element) : new List<ElementSave>();

            var isThisAbsoluteLayout = element != null && IsOfXamarinFormsType(element, "AbsoluteLayout");
            var isStackLayout = element != null && IsOfXamarinFormsType(element, "StackLayout");
            var isSkiaCanvasView = element != null && IsOfXamarinFormsType(element, "SkiaGumCanvasView");

            if (isThisAbsoluteLayout)
            {
                // August 4, 2023 - why is it "var"? That seems like a mistake...
                //stringBuilder.AppendLine(ToTabs(tabCount) + "var MainLayout = this;");
                stringBuilder.AppendLine(context.Tabs + "MainLayout = this;");
            }
            else if (!isSkiaCanvasView && !isStackLayout)
            {
                bool shouldAddMainLayout = GetIfShouldAddMainLayout(element, projectSettings);

                if (shouldAddMainLayout)
                {
                    stringBuilder.AppendLine(context.Tabs + "MainLayout = new AbsoluteLayout();");
                    stringBuilder.AppendLine(context.Tabs + "BaseGrid.Children.Add(MainLayout);");
                }
            }
            #endregion
        }

        context.Instance = null;

        if (context.CodeOutputProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
        {
            FillWithVariableAssignments(visualApi, stringBuilder, context);
        }

        stringBuilder.AppendLine();

        if (!DoesElementInheritFromCodeGeneratedElement(element, projectSettings))
        {
            if (context.CodeOutputProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
            {
                stringBuilder.AppendLine(context.Tabs + "InitializeInstances();");
            }

            if (context.CodeOutputProjectSettings.GenerateGumDataTypes)
            {
                stringBuilder.AppendLine(context.Tabs + "AssignGumReferences();");
            }
        }

        stringBuilder.AppendLine();



        // fill with variable binding after the instances have been created
        if (visualApi == VisualApi.XamarinForms)
        {
            FillWithVariableBinding(element, stringBuilder, context.TabCount);
        }

        if (context.CodeOutputProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
        {
            // this may not be necessary anymore:
            //stringBuilder.AppendLine(context.Tabs + "if(fullInstantiation)");
            //stringBuilder.AppendLine(context.Tabs + "{");
            //context.TabCount++;
            stringBuilder.AppendLine(context.Tabs + "ApplyDefaultVariables();");
            //context.TabCount--;
            //stringBuilder.AppendLine(context.Tabs + "}");

            if (!DoesElementInheritFromCodeGeneratedElement(element, projectSettings))
            {
                // AssignParents doesn't call base so that the derived can control the ultimate order.
                // However, it overrides and the base calls AssignParents. Therefore, no need for us to
                // call it here, I don't think...
                stringBuilder.AppendLine(context.Tabs + "AssignParents();");
            }

            if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame)
            {
                stringBuilder.AppendLine(context.Tabs + "if(tryCreateFormsObject)");
                stringBuilder.AppendLine(context.Tabs + "{");
                context.TabCount++;
                // Do this after assigning parents
                TryInstantiateForms(context);
                context.TabCount--;
                stringBuilder.AppendLine(context.Tabs + "}");
            }

            // If not fully in code, we do this in AfterFullCreation
            stringBuilder.AppendLine(context.Tabs + "CustomInitialize();");
        }

        if (visualApi == VisualApi.Gum)
        {
        }
        else
        {
            stringBuilder.AppendLine(context.Tabs + "GraphicalUiElement.IsAllLayoutSuspended = wasSuspended;");

        }

        DoInitialSizeUpdates(context);


        context.TabCount--;
        stringBuilder.AppendLine(context.Tabs + "}");
    }

    private static void DoInitialSizeUpdates(CodeGenerationContext context)
    {
        var element = context.Element;

        foreach (var instance in element.Instances)
        {
            var isSkiaSharpCanvasView = IsOfXamarinFormsType(instance, "SkiaGumCanvasView");

            if (isSkiaSharpCanvasView)
            {
                var variableFinder = new RecursiveVariableFinder(instance, element);

                // See if its width or height units depend on children:
                var heightUnits = variableFinder.GetValue<DimensionUnitType>("HeightUnits");
                var widthUnits = variableFinder.GetValue<DimensionUnitType>("WidthUnits");

                if (heightUnits == DimensionUnitType.RelativeToChildren || widthUnits == DimensionUnitType.RelativeToChildren)
                {
                    if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
                    {
                        context.StringBuilder.AppendLine("// This hurts performance a little but it's needed because of an iOS MAUI bug where these do not behave the same as in Android");
                        context.StringBuilder.AppendLine(context.Tabs + context.GetInstanceNameInCode(instance) + ".ForceGumLayout();");
                        context.StringBuilder.AppendLine(context.Tabs + context.GetInstanceNameInCode(instance) + ".UpdateDimensionsFromAutoSize();");
                    }
                }
            }
        }
    }

    private static bool GetIfShouldAddMainLayout(ElementSave element, CodeOutputProjectSettings? projectSettings)
    {
        var elementBaseType = element?.BaseType;
        var isThisAbsoluteLayout = IsOfXamarinFormsType(element, "AbsoluteLayout");
        var isThisStackLayout = IsOfXamarinFormsType(element, "StackLayout");
        var isSkiaCanvasView = IsOfXamarinFormsType(element, "SkiaGumCanvasView");

        var isContainer = elementBaseType == "Container";

        var shouldAddMainLayout = !isSkiaCanvasView && !isContainer && !isThisStackLayout &&
            (projectSettings.OutputLibrary == OutputLibrary.XamarinForms || projectSettings.OutputLibrary == OutputLibrary.Maui);
        if (shouldAddMainLayout && element is ScreenSave && !string.IsNullOrEmpty(element.BaseType) && !projectSettings.BaseTypesNotCodeGenerated.Contains(element.BaseType))
        {
            shouldAddMainLayout = false;
        }

        return shouldAddMainLayout;
    }


    #endregion

    #region Top Level Methods

    public string GetGeneratedCodeForElement(ElementSave element, CodeOutputElementSettings elementSettings, CodeOutputProjectSettings projectSettings)
    {
        #region Initial Values

        AdjustPixelValuesForDensity = projectSettings.AdjustPixelValuesForDensity;
        VisualApi visualApi = GetVisualApiForElement(element);

        var stringBuilder = new StringBuilder();
        var context = new CodeGenerationContext();
        context.TabCount = 0;
        context.Element = element;
        context.CodeOutputProjectSettings = projectSettings;
        context.StringBuilder = stringBuilder;
        context.ElementSettings = elementSettings;


        #endregion

        #region Using Statements

        GenerateUsingStatements(context);

        #endregion

        #region Namespace Header/Opening {

        string namespaceName = GetElementNamespace(element, elementSettings, projectSettings);

        if (!string.IsNullOrEmpty(namespaceName))
        {
            stringBuilder.AppendLine(context.Tabs + $"namespace {namespaceName};");
        }

        #endregion

        #region Class Header/Opening {

        GenerateClassHeader(context);
        stringBuilder.AppendLine(context.Tabs + "{");
        context.TabCount++;
        #endregion

        #region Register

        RegisterRuntimeType(context);

        #endregion

        AddGumFormsMembers(context);

        #region States

        FillWithStateEnums(context);
        FillWithStateProperties(context);

        #endregion

        #region Instances

        foreach (var instance in element.Instances.Where(item => item.DefinedByBase == false))
        {
            context.Instance = instance;
            FillWithInstanceDeclaration(context);
        }

        AddAbsoluteLayoutIfNecessary(element, context.TabCount, stringBuilder, projectSettings);

        stringBuilder.AppendLine();

        #endregion

        if (projectSettings.GenerateGumDataTypes)
        {
            GenerateGumSaveObjects(context, stringBuilder);
        }

        #region Variables (Exposed and "new")

        FillWithNewVariables(context);

        FillWithExposedVariables(context);
        // -- no need for AppendLine here since FillWithExposedVariables does it after every variable --
        #endregion

        GenerateConstructors(context);

        GenerateInitializeInstancesMethod(context);

        if (context.CodeOutputProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
        {
            GenerateAddToParentsMethod(context);
            GenerateApplyDefaultVariables(context);
        }
        else
        {
            context.StringBuilder.AppendLine(context.Tabs + "//Not assigning variables because Object Instantiation Type is set to By Name rather than Fully In Code");
        }


        if (projectSettings.GenerateGumDataTypes)
        {
            GenerateAssignGumReferences(context);
        }

        GenerateApplyLocalizationMethod(element, context.TabCount, stringBuilder);

        stringBuilder.AppendLine(context.Tabs + "partial void CustomInitialize();");

        #region Class Closing }
        context.TabCount--;
        stringBuilder.AppendLine(context.Tabs + "}");
        #endregion

        return stringBuilder.ToString();
    }


    #endregion

    #region Assign Gum References (this.ComponentSave = ...)

    private static void GenerateAssignGumReferences(CodeGenerationContext context)
    {

        var stringBuilder = context.StringBuilder;
        var tabCount = context.TabCount;
        var element = context.Element;

        var line = "private void AssignGumReferences()";

        stringBuilder.AppendLine(ToTabs(tabCount) + line);
        stringBuilder.AppendLine(ToTabs(tabCount) + "{");
        tabCount++;

        stringBuilder.AppendLine(ToTabs(tabCount) + "var gumProjectSave = ObjectFinder.Self.GumProjectSave;");

        stringBuilder.AppendLine(ToTabs(tabCount) + "//////////////Early Out/////////////");
        stringBuilder.AppendLine(ToTabs(tabCount) + "if (gumProjectSave == null) return;");
        stringBuilder.AppendLine(ToTabs(tabCount) + "////////////End Early Out///////////");

        string screenOrComponent = "UNKNOWN";
        if (element is ScreenSave)
        {
            stringBuilder.AppendLine(ToTabs(tabCount) + $"ScreenSave = gumProjectSave.Screens.Find(item => item.Name == \"{element.Name}\");");
            screenOrComponent = "ScreenSave";
        }
        else if (element is ComponentSave)
        {
            stringBuilder.AppendLine(ToTabs(tabCount) + $"ComponentSave = gumProjectSave.Components.Find(item => item.Name == \"{element.Name}\");");
            screenOrComponent = "ComponentSave";
        }

        foreach (var instance in element.Instances)
        {
            var instanceVisualApi = GetVisualApiForInstance(instance, element);
            if (instanceVisualApi == VisualApi.Gum)
            {
                // todo - will need Forms too, but we'll do this for now:
                stringBuilder.AppendLine(ToTabs(tabCount) + $"{context.GetInstanceNameInCode(instance)}.Tag = {screenOrComponent}.Instances.Find(item => item.Name == \"{instance.Name}\");");
            }
        }

        tabCount--;
        stringBuilder.AppendLine(ToTabs(tabCount) + "}");
    }

    #endregion

    #region States

    public static string GetCodeForState(ElementSave container, StateSave stateSave, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        var stringBuilder = new StringBuilder();

        var context = new CodeGenerationContext();
        context.Element = container;
        context.CodeOutputProjectSettings = codeOutputProjectSettings;


        FillWithVariablesInState(stateSave, stringBuilder, 0, context);

        var code = stringBuilder.ToString();
        return code;
    }

    private static VariableSave[] GetVariablesToAssignOnState(StateSave stateSave)
    {
        VariableSave[] variablesToConsider = stateSave.Variables
            // make "Parent" first
            .Where(item => item.GetRootName() != "Parent")
            .ToArray();
        return variablesToConsider;
    }

    private static void FillWithStateEnums(CodeGenerationContext context)
    {

        // for now we'll just do categories. We may need to get uncategorized at some point...
        foreach (var category in context.Element.Categories)
        {
            string enumName = category.Name;

            context.StringBuilder.AppendLine(ToTabs(context.TabCount) + $"public enum {category.Name}");
            context.StringBuilder.AppendLine(ToTabs(context.TabCount) + "{");
            context.TabCount++;

            foreach (var state in category.States)
            {
                context.StringBuilder.AppendLine(ToTabs(context.TabCount) + $"{state.Name},");
            }

            context.TabCount--;
            context.StringBuilder.AppendLine(ToTabs(context.TabCount) + "}");
        }
    }

    private static void FillWithStateProperties(CodeGenerationContext context)
    {
        var isXamarinForms = GetVisualApiForElement(context.Element) == VisualApi.XamarinForms;
        var containerClassName = GetClassNameForType(context.Element, GetVisualApiForElement(context.Element), context);


        foreach (var category in context.Element.Categories)
        {
            FillWithStatePropertiesForCategory(context.Element, context.StringBuilder, context.TabCount, context.CodeOutputProjectSettings, isXamarinForms, containerClassName, category);

        }
    }

    private static int FillWithStatePropertiesForCategory(ElementSave element, StringBuilder stringBuilder, int tabCount, CodeOutputProjectSettings codeProjectSettings, bool isXamarinForms, string containerClassName, StateSaveCategory category)
    {
        // If it's Xamarin Forms we want to have the states be bindable

        stringBuilder.AppendLine();

        // Enum types need to be nullable because there could be no category set:
        string enumName = category.Name + "?";

        if (codeProjectSettings.ObjectInstantiationType == ObjectInstantiationType.FullyInCode)
        {

            if (isXamarinForms)
            {

                stringBuilder.AppendLine($"{ToTabs(tabCount)}public static readonly BindableProperty {category.Name}StateProperty = " +
                    $"BindableProperty.Create(nameof({category.Name}State),typeof({enumName}),typeof({containerClassName}), defaultBindingMode: BindingMode.TwoWay, propertyChanged:Handle{category.Name}StatePropertyChanged);");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {enumName} {category.Name}State");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => ({enumName})GetValue({category.Name}StateProperty);");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set => SetValue({category.Name}StateProperty, value);");
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"private static void Handle{category.Name}StatePropertyChanged(BindableObject bindable, object oldValue, object newValue)");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"var casted = bindable as {containerClassName};");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"var value = ({enumName})newValue;");
                CodeGenerationContext context = new CodeGenerationContext();
                context.Element = element;
                context.ThisPrefix = "casted";
                context.TabCount = tabCount;
                context.CodeOutputProjectSettings = codeProjectSettings;

                AddAssignFromElement(context, stringBuilder);

                stringBuilder.AppendLine(context.Tabs + "if(!appliedDynamically)");
                stringBuilder.AppendLine(context.Tabs + "{");
                context.TabCount++;

                CreateStateVariableAssignmentSwitch(stringBuilder, category, context);

                context.TabCount--;
                stringBuilder.AppendLine(context.Tabs + "}");

                // We may need to invalidate surfaces here if any objects that have variables assigned are skia canvases
                // Update November 29, 2022
                // Currently we brute-force it by calling InvalidateSurface on all objects and their EffectiveManagers.
                // Could this be expensive? I think that this just flips a flag, and it will happen so fast that actual
                // redraws only occur 1 time. But if it's slow, we could hashset which manages have been invalidated and
                // make sure each one is only invalidated one time.
                // Update April 25, 2024
                // This is slow because it invalidates all surfaces, even if they aren't associated with the objects being
                // assigned. Therefore, we should only invalidate for instances which have variables assigned in this category.
                var instancesNamesWithVariablesAssigned = category.States.SelectMany(item => item.Variables).Select(item => item.SourceObject).Distinct().ToList();

                var instances = element.Instances.Where(item => instancesNamesWithVariablesAssigned.Contains(item.Name)).ToList();

                foreach (var item in instances)
                {
                    if (item.BaseType.EndsWith("/SkiaSharpCanvasView"))
                    {
                        stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{context.GetInstanceNameInCode(item)}.InvalidateSurface();");
                    }
                    else if (GetVisualApiForInstance(item, element) == VisualApi.Gum)
                    {
                        stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{context.GetInstanceNameInCode(item)}.EffectiveManagers?.InvalidateSurface();");
                    }
                }
                if (element.BaseType?.EndsWith("/SkiaGumCanvasView") == true)
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.InvalidateSurface();");
                }

                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");

            }
            else
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + $"{enumName} m{category.Name}State;");


                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {enumName} {category.Name}State");

                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => m{category.Name}State;");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set");

                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"m{category.Name}State = value;");
                CodeGenerationContext context = new CodeGenerationContext();
                context.Element = element;
                context.TabCount = tabCount;
                context.CodeOutputProjectSettings = codeProjectSettings;

                AddAssignFromElement(context, stringBuilder);

                stringBuilder.AppendLine(context.Tabs + "if(!appliedDynamically)");
                stringBuilder.AppendLine(context.Tabs + "{");
                context.TabCount++;

                CreateStateVariableAssignmentSwitch(stringBuilder, category, context);

                context.TabCount--;
                stringBuilder.AppendLine(context.Tabs + "}");


                context.TabCount--;
                stringBuilder.AppendLine(ToTabs(context.TabCount) + "}");

                context.TabCount--;
                stringBuilder.AppendLine(ToTabs(context.TabCount) + "}");
            }
        }
        else
        {
            var propertyName = $"{category.Name}State";

            var fieldName = "_" + char.ToLower(propertyName[0]) + propertyName.Substring(1);

            stringBuilder.AppendLine(ToTabs(tabCount) + $"{category.Name}? {fieldName};");


            stringBuilder.AppendLine(ToTabs(tabCount) + $"public {category.Name}? {propertyName}");

            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;

            stringBuilder.AppendLine(ToTabs(tabCount) + $"get => {fieldName};");

            stringBuilder.AppendLine(ToTabs(tabCount) + $"set");

            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;

            stringBuilder.AppendLine(ToTabs(tabCount) + $"{fieldName} = value;");


            var categories = "Categories";
            var thisOptionalVisual = "this";
            if (codeProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
            {
                categories = "Visual.Categories";
                thisOptionalVisual = "this.Visual";
            }

            stringBuilder.AppendLine(ToTabs(tabCount) + $"if(value != null)");
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;

            stringBuilder.AppendLine(ToTabs(tabCount) + $"if({categories}.ContainsKey(\"{category}\"))");
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;
            stringBuilder.AppendLine(ToTabs(tabCount) + $"var category = {categories}[\"{category}\"];");
            stringBuilder.AppendLine(ToTabs(tabCount) + $"var state = category.States.Find(item => item.Name == value.ToString());");
            stringBuilder.AppendLine(ToTabs(tabCount) + $"{thisOptionalVisual}.ApplyState(state);");
            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            stringBuilder.AppendLine(ToTabs(tabCount) + $"else");
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;

            string tagCategories = "((global::Gum.DataTypes.ElementSave)this.Tag).Categories";
            if (codeProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
            {
                tagCategories = "((global::Gum.DataTypes.ElementSave)this.Visual.Tag).Categories";
            }

            stringBuilder.AppendLine(ToTabs(tabCount) + $"var category = {tagCategories}.FirstOrDefault(item => item.Name == \"{category}\");");
            stringBuilder.AppendLine(ToTabs(tabCount) + $"var state = category.States.Find(item => item.Name == value.ToString());");
            stringBuilder.AppendLine(ToTabs(tabCount) + $"{thisOptionalVisual}.ApplyState(state);");
            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");


            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");

            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");


        }

        return tabCount;
    }

    private static void AddAssignFromElement(CodeGenerationContext context, StringBuilder stringBuilder)
    {

        stringBuilder.AppendLine(context.Tabs + "var appliedDynamically = false;");

        if (context.CodeOutputProjectSettings.GenerateGumDataTypes &&
            // todo - do we care about this? Maybe eventually?
            context.VisualApi == VisualApi.Gum)
        {
            stringBuilder.AppendLine(context.Tabs + "if (BindableGraphicalUiElement.ShouldApplyDynamicStates)");
            stringBuilder.AppendLine(context.Tabs + "{");
            context.TabCount++;

            var elementPropertyName = "ElementSave";
            if (context.VisualApi == VisualApi.XamarinForms)
            {
                elementPropertyName =
                    context.Element is ScreenSave ? "casted.ScreenSave"
                    : context.Element is ComponentSave ? "casted.ComponentSave"
                    : null;
            }


            stringBuilder.AppendLine(context.Tabs + "var qualifiedValueName = value.ToString();");
            stringBuilder.AppendLine(context.Tabs + "var name = qualifiedValueName;");
            stringBuilder.AppendLine(context.Tabs + "if (qualifiedValueName.Contains(\".\"))");
            stringBuilder.AppendLine(context.Tabs + "{");
            context.TabCount++;
            stringBuilder.AppendLine(context.Tabs + "var lastIndex = qualifiedValueName.LastIndexOf('.');");
            stringBuilder.AppendLine(context.Tabs + "name = qualifiedValueName.Substring(lastIndex + 1);");
            context.TabCount--;
            stringBuilder.AppendLine(context.Tabs + "}");
            stringBuilder.AppendLine(context.Tabs + "var foundState = ElementSave?.GetStateSaveRecursively(name);");

            //stringBuilder.AppendLine(context.Tabs + $"var foundState = {elementPropertyName}?.GetStateSaveRecursively(value.ToString());");
            stringBuilder.AppendLine(context.Tabs + "if (foundState != null)");
            stringBuilder.AppendLine(context.Tabs + "{");
            context.TabCount++;
            if (context.VisualApi == VisualApi.XamarinForms)
            {
                stringBuilder.AppendLine(context.Tabs + "// Need to apply the variables in the state");
            }
            else
            {
                stringBuilder.AppendLine(context.Tabs + "this.ApplyState(foundState);");
            }
            stringBuilder.AppendLine(context.Tabs + "appliedDynamically = true;");
            context.TabCount--;
            stringBuilder.AppendLine(context.Tabs + "}");
            context.TabCount--;
            stringBuilder.AppendLine(context.Tabs + "}");
        }
    }

    private static void CreateStateVariableAssignmentSwitch(StringBuilder stringBuilder, StateSaveCategory category, CodeGenerationContext context)
    {
        stringBuilder.AppendLine(ToTabs(context.TabCount) + $"switch (value)");
        stringBuilder.AppendLine(ToTabs(context.TabCount) + "{");
        context.TabCount++;

        foreach (var state in category.States)
        {
            stringBuilder.AppendLine(ToTabs(context.TabCount) + $"case {category.Name}.{state.Name}:");
            context.TabCount++;

            FillWithVariablesInState(state, stringBuilder, context.TabCount, context);

            stringBuilder.AppendLine(ToTabs(context.TabCount) + $"break;");
            context.TabCount--;
        }

        context.TabCount--;
        stringBuilder.AppendLine(ToTabs(context.TabCount) + "}");
    }

    private static void FillWithVariablesInState(StateSave stateSave, StringBuilder stringBuilder, int tabCount, CodeGenerationContext context)
    {
#if DEBUG
        if (context.CodeOutputProjectSettings == null)
        {
            throw new NullReferenceException("context.CodeOutputProjectSettings should not be null");
        }
#endif
        VariableSave[] variablesToConsider = GetVariablesToAssignOnState(stateSave);

        var variableGroups = variablesToConsider.GroupBy(item => item.SourceObject);

        foreach (var group in variableGroups)
        {
            InstanceSave instance = null;
            var instanceName = group.Key;

            if (instanceName != null)
            {
                instance = context.Element.GetInstance(instanceName);
            }
            context.Instance = instance;

            #region Determine visual API (Gum or Forms)

            VisualApi visualApi = VisualApi.Gum;

            var defaultState = context.Element.DefaultState;
            bool? isXamForms = false;
            if (instance == null)
            {
                isXamForms = defaultState.GetValueRecursive($"IsXamarinFormsControl") as bool?;
            }
            else
            {
                isXamForms = defaultState.GetValueRecursive($"{instance.Name}.IsXamarinFormsControl") as bool?;
            }
            if (isXamForms == true)
            {
                visualApi = VisualApi.XamarinForms;
            }

            #endregion

            ElementSave baseElement = null;
            if (instance == null)
            {
                baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(context.Element.BaseType) ?? context.Element;
            }
            else
            {
                baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance?.BaseType);
            }

            // could be null if the element references an element that doesn't exist.
            if (baseElement != null)
            {
                var baseDefaultState = baseElement?.DefaultState;
                RecursiveVariableFinder baseRecursiveVariableFinder = new RecursiveVariableFinder(baseDefaultState);


                List<VariableSave> variablesForThisInstance = group
                    .Where(item => GetIfVariableShouldBeIncludedForInstance(instance, item, baseRecursiveVariableFinder))
                    .ToList();


                ProcessVariableGroups(variablesForThisInstance, stateSave, visualApi, stringBuilder, context);

                // Now that they've been processed, we can process the remainder regularly
                foreach (var variable in variablesForThisInstance)
                {
                    var codeLine = GetCodeLine(variable, context.Element, visualApi, stateSave, context);
                    stringBuilder.AppendLine(ToTabs(tabCount) + codeLine);
                }
            }

        }
    }


    #endregion

    private static void GenerateGumSaveObjects(CodeGenerationContext context, StringBuilder stringBuilder)
    {
        var element = context.Element;
        if (element is ScreenSave)
        {
            stringBuilder.AppendLine(context.Tabs + "global::Gum.DataTypes.ScreenSave ScreenSave { get; set; }");
        }
        else if (element is ComponentSave)
        {
            if (context.VisualApi == VisualApi.XamarinForms)
            {
                stringBuilder.AppendLine(context.Tabs + "global::Gum.DataTypes.ComponentSave ComponentSave { get; set; }");
            }
            else
            {
                stringBuilder.AppendLine(context.Tabs + "global::Gum.DataTypes.ComponentSave ComponentSave");
                stringBuilder.AppendLine(context.Tabs + "{");
                context.TabCount++;
                stringBuilder.AppendLine(context.Tabs + "get => ElementSave as global::Gum.DataTypes.ComponentSave;");
                stringBuilder.AppendLine(context.Tabs + "set => ElementSave = value;");
                context.TabCount--;
                stringBuilder.AppendLine(context.Tabs + "}");
            }
        }
    }

    public static string GetCodeForInstance(InstanceSave instance, ElementSave element, CodeOutputProjectSettings codeOutputProjectSettings)
    {
        var stringBuilder = new StringBuilder();

        var context = new CodeGenerationContext();
        context.Instance = instance;
        context.Element = element;
        context.CodeOutputProjectSettings = codeOutputProjectSettings;
        context.StringBuilder = stringBuilder;

        FillWithInstanceDeclaration(context);

        FillWithInstanceInstantiation(context);

        FillWithNonParentVariableAssignments(context);

        FillWithParentAssignments(context);

        var code = stringBuilder.ToString();
        return code;
    }

    #region Variable Assignments

    private static void FillWithVariableAssignments(VisualApi visualApi, StringBuilder stringBuilder, CodeGenerationContext context)
    {
        var element = context.Element;

        #region Get variables to consider
        var defaultState = element.DefaultState;

        var baseElement = ObjectFinder.Self.GetElementSave(element.BaseType);
        RecursiveVariableFinder recursiveVariableFinder = null;

        // This is null if it's a screen, or there's some bad reference
        if (baseElement != null)
        {
            recursiveVariableFinder = new RecursiveVariableFinder(baseElement.DefaultState);
        }

        var variablesToConsider = defaultState.Variables
            .Where(item =>
            {
                var shouldInclude =
                    item.Value != null &&
                    item.SetsValue &&
                    string.IsNullOrEmpty(item.SourceObject);

                if (shouldInclude)
                {
                    if (recursiveVariableFinder != null)
                    {
                        // We want to make sure that the variable is defined in the base object. If it isn't, then
                        // it could be a leftover variable caused by having this object be of one type, using a variable
                        // specific to that type, then changing it to another type. Gum holds on to these varibles in case
                        // the type change was accidental, but it means we have to watch for these orphan variables when generating.
                        var foundVariable = recursiveVariableFinder.GetVariable(item.Name);
                        shouldInclude = foundVariable != null;
                    }
                    else
                    {
                        if (item.Name.EndsWith("State"))
                        {
                            var type = item.Type.Substring(item.Type.Length - 5);
                            var hasCategory = element.GetStateSaveCategoryRecursively(type) != null;

                            if (!hasCategory)
                            {
                                shouldInclude = false;
                            }
                        }
                    }

                }
                if (shouldInclude)
                {
                    var rootName = item.GetRootName();

                    // these are excluded from codegen for now:
                    if (rootName == "ContainedType")
                    {
                        shouldInclude = false;
                    }
                }

                return shouldInclude;
            })
            .ToList();

        #endregion

        var tabs = new String(' ', 4 * context.TabCount);

        ProcessVariableGroups(variablesToConsider, defaultState, visualApi, stringBuilder, context);

        foreach (var variable in variablesToConsider)
        {
            var codeLine = GetCodeLine(variable, element, visualApi, defaultState, context);
            stringBuilder.AppendLine(tabs + codeLine);
        }

    }

    private static void FillWithNonParentVariableAssignments(CodeGenerationContext context)
    {
        #region Get variables to consider

        var variablesToAssignValues = GetVariablesForValueAssignmentCode(context.Instance, context.Element)
            .Where(item => item.GetRootName() != "Parent")
            .ToList();

        #endregion

        FillWithVariableAssignments(context, context.StringBuilder, variablesToAssignValues);

        var variableListsToAssign = context.Element.DefaultState.VariableLists.Where(item => item.SourceObject == context.Instance.Name)
            .ToArray();

        FillWithVariableListAssignments(context, context.StringBuilder, variableListsToAssign);
    }

    private static void FillWithVariableListAssignments(CodeGenerationContext context, StringBuilder stringBuilder, VariableListSave[] variableListsToAssign)
    {
        VisualApi visualApi = GetVisualApiForInstance(context.Instance, context.Element);

        foreach (var variableList in variableListsToAssign)
        {
            AddCodeLine(variableList, context, visualApi);
        }
    }

    /// <summary>
    /// Returns a no-tabbed line of code for the argument variable
    /// </summary>
    private static string GetCodeLine(VariableSave variable, ElementSave container, VisualApi visualApi, StateSave state, CodeGenerationContext context)
    {
        if (visualApi == VisualApi.Gum)
        {
            var fullLineReplacement = TryGetFullGumLineReplacement(variable, context);

            if (fullLineReplacement != null)
            {
                return fullLineReplacement;
            }
            else
            {
                var variableName = GetGumVariableName(variable, context);

                var forceSetDirectlyOnInstance = false;

                ElementSave? instanceElement = null;

                if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms && context.Instance != null)
                {
                    // if the variable is an exposed variable on the instance, then we don't want to do a .Visual., because the
                    // exposed variable lives on the main generated object.
                    instanceElement = ObjectFinder.Self.GetElementSave(context.Instance);

                    var defaultState = instanceElement?.DefaultState;

                    var matchingExposedVariable = defaultState?.Variables.FirstOrDefault(item => item.ExposedAsName == variableName);

                    if (matchingExposedVariable != null)
                    {
                        forceSetDirectlyOnInstance = true;
                    }
                }
                if (!forceSetDirectlyOnInstance && variable.IsState(container) && context.Instance != null && context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
                {
                    // If it's a state set on an instance, set it directly on the instance and not on 
                    forceSetDirectlyOnInstance = true;
                }

                if(!forceSetDirectlyOnInstance && context.Instance != null && context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
                {
                    // this could be a variable like assigning "Text" on a label. Since this label inherits directly from the standard Text type, then the
                    // value Text exists right on it. This is a little dangerous because it means users could assign variables that don't exist on Label. To
                    // fix that, we would either have to add those variables to the generated Label type in code gen, or we suppress these variables from being
                    // assigned in Gum.
                    // This is a tricky situation, but either way we should support setting Text:
                    instanceElement = instanceElement ?? ObjectFinder.Self.GetElementSave(context.Instance);

                    string? formsType = null;

                    GetGumFormsTypeFromBehaviors(instanceElement, out formsType, out _);

                    // special case for now, need to handle this in a more generalized manner:
                    if(formsType == BehaviorGumFormsTypes["LabelBehavior"])
                    {
                        switch(variableName)
                        {
                            case "Text":
                                forceSetDirectlyOnInstance = true;
                                break;
                        }
                    }
                }

                if (forceSetDirectlyOnInstance)
                {
                    return $"this.{context.InstanceNameInCode}.{variableName} = {VariableValueToGumCodeValue(variable, context)};";
                }
                else
                {

                    return $"{context.CodePrefixNoTabs}.{variableName} = {VariableValueToGumCodeValue(variable, context)};";
                }
            }

        }
        else // xamarin forms
        {
            var fullLineReplacement = TryGetFullXamarinFormsLineReplacement(context.Instance, container, variable, state, context);
            if (fullLineReplacement != null)
            {
                return fullLineReplacement;
            }
            else
            {
                return $"{context.CodePrefixNoTabs}.{GetXamarinFormsVariableName(variable)} = {VariableValueToXamarinFormsCodeValue(variable, container, context)};";
            }

        }
    }

    private static string VariableValueToXamarinFormsCodeValue(VariableSave variable, ElementSave container, CodeGenerationContext context)
    {
        var value = variable.Value;
        var rootName = variable.GetRootName();
        var isState = variable.IsState(container, out ElementSave categoryContainer, out StateSaveCategory category);
        return VariableValueToXamarinFormsCodeValue(value, rootName, isState, categoryContainer, category, context);
    }

    private static string VariableValueToXamarinFormsCodeValue(VariableListSave variable, ElementSave container, CodeGenerationContext context)
    {
        var value = variable.ValueAsIList;
        var rootName = variable.GetRootName();
        var isState = false;
        return VariableValueToXamarinFormsCodeValue(value, rootName, isState, null, null, context);
    }



    private static void AddCodeLine(VariableListSave variable, CodeGenerationContext context, VisualApi visualApi)
    {
        // for now we actually don't do anything with this - I used to think we would, but the variable lists are part of the Gum save objects, not rutnime.

        // actually polygon points are so we need those:
        var instance = context.Instance;
        if (variable.GetRootName() == "Points")
        {
            bool isPolygon = false;
            if (instance != null)
            {
                var instanceType = ObjectFinder.Self.GetElementSave(instance.BaseType);
                isPolygon = (instanceType is StandardElementSave && instanceType.Name == "Polygon") ||
                    ObjectFinder.Self.GetBaseElements(instanceType).Any(item => item is StandardElementSave && item.Name == "Polygon");

                if (isPolygon)
                {
                    context.StringBuilder.AppendLine(context.Tabs + $"this.{context.InstanceNameInCode}.SetPoints(new System.Numerics.Vector2[]{{");
                    context.TabCount++;
                    foreach (System.Numerics.Vector2 point in variable.ValueAsIList)
                    {
                        context.StringBuilder.AppendLine(context.Tabs + $"new System.Numerics.Vector2(" +
                            $"{point.X.ToString(CultureInfo.InvariantCulture)}f, {point.Y.ToString(CultureInfo.InvariantCulture)}f),");
                    }
                    context.TabCount--;
                    context.StringBuilder.AppendLine(context.Tabs + "});");

                }
            }
        }
    }

    private static string VariableValueToGumCodeValue(VariableSave variable, CodeGenerationContext context, object forcedValue = null)
    {
        var value = forcedValue ?? variable.Value;
        var rootName = variable.GetRootName();
        var isState = variable.IsState(context.Element, out ElementSave categoryContainer, out StateSaveCategory category);

        return VariableValueToGumCode(value, rootName, isState, categoryContainer, category, context.CodeOutputProjectSettings);
    }

    private static string VariableValueToGumCodeValue(VariableListSave variable, ElementSave container, CodeOutputProjectSettings codeOutputProjectSettings, object forcedValue = null)
    {
        var value = forcedValue ?? variable.ValueAsIList;
        var rootName = variable.GetRootName();
        var isState = false;

        return VariableValueToGumCode(value, rootName, isState, null, null, codeOutputProjectSettings);
    }

    private static string? VariableValueToGumCode(object value, string rootName, bool isState, ElementSave categoryContainer, StateSaveCategory category, CodeOutputProjectSettings settings)
    {
        if (value is float asFloat)
        {
            return asFloat.ToString(CultureInfo.InvariantCulture) + "f";
        }
        else if (value is string asString)
        {
            if (rootName == "Parent")
            {
                return asString;
            }
            else if (isState)
            {
                if (categoryContainer != null && category != null)
                {
                    if (categoryContainer is StandardElementSave)
                    {
                        // If it's a standard element save, this won't have the category generated as an enum, so we have to rely
                        // on the state itself having been set on the element...
                        return $"ObjectFinder.Self.GetStandardElement(\"{categoryContainer.Name}\").GetStateSaveRecursively(\"{asString}\")";
                    }
                    else
                    {
                        string containerClassName = "VariableState";
                        if (categoryContainer != null)
                        {
                            var context = new CodeGenerationContext();
                            context.CodeOutputProjectSettings = settings;

                            // We're going to make it easier for the user by getting the fully-qualified type name for their component:
                            //containerClassName = GetClassNameForType(categoryContainer.Name, VisualApi.Gum, context);
                            containerClassName = GetClassNameForType(categoryContainer, VisualApi.Gum, context, isFullyQualified:true) ?? "UnknownType";
                        }
                        return $"{containerClassName}.{category.Name}.{asString}";
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return $"@\"{asString}\"";
            }
        }
        else if (value is bool)
        {
            return value.ToString().ToLowerInvariant();
        }
        else if (value?.GetType().IsEnum == true)
        {
            var type = value.GetType();
            if (type == typeof(PositionUnitType))
            {
                var converted = UnitConverter.ConvertToGeneralUnit(value);
                return $"global::Gum.Converters.GeneralUnitType.{converted}";
            }
            else if (type == typeof(DimensionUnitType))
            {
                var valueAsString = value;
                // handle the deprecated type:
#pragma warning disable CS0618 // Type or member is obsolete
                if (value is DimensionUnitType dimensionUnitType)
                {
                    if (dimensionUnitType == DimensionUnitType.RelativeToParent)
                    {
                        valueAsString = nameof(DimensionUnitType.RelativeToParent);
                    }
                    else if (dimensionUnitType == DimensionUnitType.PercentageOfParent)
                    {
                        valueAsString = nameof(DimensionUnitType.PercentageOfParent);
                    }
                }
#pragma warning restore CS0618 // Type or member is obsolete
                return $"global::{value.GetType().FullName}.{valueAsString}";
            }
            else
            {
                return $"global::{value.GetType().FullName}.{value}";
            }
        }
        else
        {
            return value?.ToString();
        }
    }

    private static string VariableValueToXamarinFormsCodeValue(object value, string rootName, bool isState, ElementSave categoryContainer, StateSaveCategory category, CodeGenerationContext context)
    {
        if (value is float asFloat)
        {
            // X and Y go to PixelX and PixelY
            if (rootName == "X" || rootName == "Y")
            {
                return asFloat.ToString(CultureInfo.InvariantCulture) + "f";
            }
            else if (rootName == "CornerRadius")
            {
                if (AdjustPixelValuesForDensity)
                {
                    return $"(int)({asFloat.ToString(CultureInfo.InvariantCulture)} / Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density)";
                }
                else
                {
                    return $"(int){asFloat.ToString(CultureInfo.InvariantCulture)}";
                }
            }
            else
            {
                if (AdjustPixelValuesForDensity)
                {
                    return $"{asFloat.ToString(CultureInfo.InvariantCulture)} / Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
                else
                {
                    return asFloat.ToString(CultureInfo.InvariantCulture);
                }
            }
        }
        else if (value is int asInt)
        {
            if (rootName == "FontSize")
            {
                if (AdjustPixelValuesForDensity)
                {
                    return $"(int)({asInt} / Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density)";
                }
                else
                {
                    return asInt.ToString();
                }
            }
        }
        else if (value is string asString)
        {
            if (rootName == "Parent")
            {
                return value.ToString();
            }
            else if (rootName == "Font")
            {
                return $"CustomFont.{value.ToString()?.Replace(" ", "_")}";
            }
            else if (isState)
            {
                var containerClassName = GetClassNameForType(categoryContainer, VisualApi.XamarinForms, context);
                if (category == null)
                {
                    return $"{containerClassName}.VariableState.{value}";

                }
                else
                {
                    return $"{containerClassName}.{category.Name}.{value}";
                }
            }
            else
            {
                return "\"" + asString.Replace("\n", "\\n").Replace("\"", "\\\"") + "\"";
            }
        }
        else if (value is bool)
        {
            return value.ToString().ToLowerInvariant();
        }
        else if (value.GetType().IsEnum)
        {
            var textAlignmentPrefix = "Xamarin.Forms";

            if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
            {
                textAlignmentPrefix = "Microsoft.Maui";
            }

            var type = value.GetType();
            if (type == typeof(PositionUnitType))
            {
                var converted = UnitConverter.ConvertToGeneralUnit(value);
                return $"global::Gum.Converters.GeneralUnitType.{converted}";
            }
            else if (type == typeof(HorizontalAlignment))
            {
                switch ((HorizontalAlignment)value)
                {
                    case HorizontalAlignment.Left:
                        return $"{textAlignmentPrefix}.TextAlignment.Start";
                    case HorizontalAlignment.Center:
                        return $"{textAlignmentPrefix}.TextAlignment.Center";
                    case HorizontalAlignment.Right:
                        return $"{textAlignmentPrefix}.TextAlignment.End";
                    default:
                        return "";
                }
            }
            else if (type == typeof(VerticalAlignment))
            {
                switch ((VerticalAlignment)value)
                {
                    case VerticalAlignment.Top:
                        return $"{textAlignmentPrefix}.TextAlignment.Start";
                    case VerticalAlignment.Center:
                        return $"{textAlignmentPrefix}.TextAlignment.Center";
                    case VerticalAlignment.Bottom:
                        return $"{textAlignmentPrefix}.TextAlignment.End";
                    default:
                        return "";
                }
            }
            else
            {
                return value.GetType().Name + "." + value.ToString();
            }
        }

        return value?.ToString();
    }


    private static string? TryGetFullXamarinFormsLineReplacement(InstanceSave instance, ElementSave container, VariableSave variable, StateSave state, CodeGenerationContext context)
    {
        var rootVariableName = variable.GetRootName();

        #region Handle all variables that have no direct translation in Xamarin forms

        if (
            rootVariableName == "ClipsChildren" ||
            rootVariableName == "ExposeChildrenEvents" ||
            rootVariableName == "FlipHorizontal" ||
            rootVariableName == "HasEvents" ||

            rootVariableName == "IsXamarinFormsControl" ||
            rootVariableName == "IsOverrideInCodeGen" ||
            rootVariableName == "Name" ||
            rootVariableName == "WrapsChildren" ||
            rootVariableName == "XOrigin" ||
            rootVariableName == "YOrigin"
            )
        {
            return " "; // Don't do anything with these variables::
        }

        #endregion

        #region Parent

        else if (rootVariableName == "Parent")
        {
            var parentName = variable.Value as string;

            var hasContent = false;


            InstanceSave? parentInstance = null;
            if(parentName != null)
            {
                parentInstance = container.GetInstance(parentName);
            }
            if (parentName?.Contains(".") == true)
            {
                var parentNameBeforeDot = parentName.Substring(0, parentName.IndexOf("."));
                parentInstance = container.GetInstance(parentNameBeforeDot);
            }

            if (parentInstance != null)
            {
                // traverse the inheritance chain - we don't want to go to the very base because 
                // Glue has base types like Container for all components, and that's not what we want.
                // Actually we should go one above the inheritance:

                var instanceElement = ObjectFinder.Self.GetElementSave(parentInstance.BaseType);

                if (instanceElement != null)
                {
                    var baseElements = ObjectFinder.Self.GetBaseElements(instanceElement);
                    string? componentType = null;
                    if (baseElements.Count > 1)
                    {
                        // don't do the "Last" because that will be container, so take all but the last:
                        var baseBeforeContainer = baseElements.Take(baseElements.Count - 1).LastOrDefault();
                        componentType = baseBeforeContainer?.Name;
                    }
                    else if (baseElements.Count == 1)
                    {
                        // this inherits from Container, so just use it's own base type:
                        componentType = instanceElement.Name;
                    }
                    else
                    {
                        // All XamForms objects are components, so all must inherit from something. This should never happen...
                    }


                    hasContent = DoesTypeHaveContent(componentType);
                }

                // Certain types of views don't support Children.Add - they only have
                // a single content. In the future we may want to formalize the way we
                // handle standard XamarinForms controls, but for now we'll hardcode some
                // checks:

                var contextInstance = context.Instance;

                if (contextInstance != null)
                {

                    if (IsTabControl(parentInstance))
                    {
                        var stringBuilder = new StringBuilder();
                        var tabs = "";
                        stringBuilder.AppendLine($"{tabs}{{");
                        tabs += new String(' ', 4);

                        var tabViewType = "Xamarin.CommunityToolkit.UI.Views.TabViewItem";
                        var textProperty = "Text";
                        var tabItemsProperty = "TabItems";

                        if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
                        {
                            // There's no community toolkit tabview, so we'll assume it is using a TabViewItem, maybe from DevExpress:
                            tabViewType = "TabViewItem";
                            textProperty = "HeaderText";
                            tabItemsProperty = "Items";
                        }



                        stringBuilder.AppendLine($"{tabs}var tabItem = new {tabViewType}();");
                        stringBuilder.AppendLine($"{tabs}tabItem.{textProperty} = \"Tab Text\";");
                        stringBuilder.AppendLine($"{tabs}tabItem.Content = {context.GetInstanceNameInCode(contextInstance)};");
                        stringBuilder.AppendLine($"{tabs}{context.GetInstanceNameInCode(parentInstance)}.{tabItemsProperty}.Add(tabItem);");
                        tabs = tabs.Substring(4);
                        stringBuilder.AppendLine($"{tabs}}}");
                        return stringBuilder.ToString();
                    }
                    else if (hasContent)
                    {
                        return $"{parentName}.Content = {context.GetInstanceNameInCode(contextInstance)};";
                    }
                    else
                    {
                        return $"{parentName}.Children.Add({context.GetInstanceNameInCode(contextInstance)});";
                    }
                }

            }
            // parent instance is null, so attach to "this" top level object
            else
            {
                // Couldn't find anything, so don't return anything

            }

        }

        #endregion

        #region Font

        else if (rootVariableName == "Font")
        {
            return $"{context.CodePrefixNoTabs}.SetFont(CustomFont.{variable.Value?.ToString()?.Replace(" ", "_")});";
        }

        #endregion

        #region ChildrenLayout

        else if (rootVariableName == "ChildrenLayout" && variable.Value is ChildrenLayout valueAsChildrenLayout)
        {
            var isInstanceStackLayout = instance != null && IsOfXamarinFormsType(instance, "StackLayout");

            if (isInstanceStackLayout)
            {
                if (valueAsChildrenLayout == ChildrenLayout.LeftToRightStack)
                {
                    return $"{context.CodePrefix}.Orientation = StackOrientation.Horizontal;";
                }
                else
                {
                    return $"{context.CodePrefix}.Orientation = StackOrientation.Vertical;";
                }
            }
            else if (instance == null && IsOfXamarinFormsType(container, "StackLayout"))
            {
                if (valueAsChildrenLayout == ChildrenLayout.LeftToRightStack)
                {
                    return $"{context.CodePrefix}.Orientation = StackOrientation.Horizontal;";
                }
                else
                {
                    return $"{context.CodePrefix}.Orientation = StackOrientation.Vertical;";
                }
            }
            else if (valueAsChildrenLayout != ChildrenLayout.Regular)
            {
                var message = $"Error: The object {instance?.Name ?? container.Name} cannot have a layout of {valueAsChildrenLayout}.";

                if (instance != null && instance.BaseType?.EndsWith("/SkiaGumCanvasView") == true)
                {
                    message += $"\nTo stack objects in a Skia canvas, add a Container which has its ChildrenLayout set to {valueAsChildrenLayout}";
                }
                else
                {
                    message += $"\nIt should probably inherit from StackLayout to be a top-to-bottom stack";
                }

                return message;
            }
            else
            {
                // it's regular, so we just ignore it
                return string.Empty;
            }
        }

        #endregion

        else if (GetIsShouldBeLocalized(variable, context.Element.DefaultState, LocalizationManager))
        {
            string assignment = GetLocalizedLine(variable, context);

            return assignment;
        }

        return null;
    }


    private static void FillWithVariableAssignments(CodeGenerationContext context, StringBuilder stringBuilder, List<VariableSave> variablesToAssignValues)
    {
        var container = context.Element;
        var instance = context.Instance;
        if (instance == null)
        {
            throw new InvalidOperationException("Instance cannot be null");
        }
        var defaultState = context.Element.DefaultState;
        VisualApi visualApi = GetVisualApiForInstance(instance, context.Element);

        // We used to do this, but now spacing is supported in FRB:
        //if (visualApi == VisualApi.XamarinForms && instance.BaseType?.EndsWith("/StackLayout") == true)
        //{
        //    stringBuilder.AppendLine($"{tabs}{instance.Name}.Spacing = 0;");
        //}

        // States come before anything, so run those first
        foreach (var variable in variablesToAssignValues.Where(item => item.IsState(container)))
        {
            var codeLine = GetCodeLine(variable, container, visualApi, defaultState, context);

            // the line of code could be " ", a string with a space. This happens
            // if we want to skip a variable so we dont return null or empty.
            // But we also don't want a ton of spaces generated.
            if (!string.IsNullOrWhiteSpace(codeLine))
            {
                stringBuilder.AppendLine(context.Tabs + codeLine);
            }
        }
        variablesToAssignValues.RemoveAll(item => item.IsState(container));

        // sometimes variables have to be processed in groups. For example, RGB values
        // have to be assigned all at once in a Color value in XamForms;
        ProcessVariableGroups(variablesToAssignValues, container.DefaultState, visualApi, stringBuilder, context);

        foreach (var variable in variablesToAssignValues)
        {
            var innerVisualApi = GetVisualApiForVariable(variable, context) ?? visualApi;
            var codeLine = GetCodeLine(variable, container, innerVisualApi, defaultState, context);

            // the line of code could be " ", a string with a space. This happens
            // if we want to skip a variable so we dont return null or empty.
            // But we also don't want a ton of spaces generated.
            if (!string.IsNullOrWhiteSpace(codeLine))
            {
                stringBuilder.AppendLine(context.Tabs + codeLine);
            }
        }
    }

    private static VisualApi? GetVisualApiForVariable(VariableSave variable, CodeGenerationContext context)
    {
        if (context.Instance != null)
        {
            // If this element is ignored in codegen, then we don't go inside to find the variable:
            var isIgnoredInCodeGen = context.CodeOutputProjectSettings?.BaseTypesNotCodeGenerated?.Contains(context.Instance.BaseType) == true;

            if (!isIgnoredInCodeGen)
            {
                var instanceElement = ObjectFinder.Self.GetElementSave(context.Instance);


                var variableRoot = variable.GetRootName();

                var matchingExposed = instanceElement?.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == variableRoot);
                if (matchingExposed != null)
                {
                    var instanceInInstanceElement = instanceElement!.GetInstance(matchingExposed.SourceObject);

                    if (instanceInInstanceElement != null)
                    {
                        return GetVisualApiForInstance(instanceInInstanceElement, instanceElement);
                    }
                }
            }
        }
        return null;
    }

    private static void GenerateApplyDefaultVariables(CodeGenerationContext context)
    {
        var line = "private void ApplyDefaultVariables()";
        context.StringBuilder.AppendLine(context.Tabs + line);
        context.StringBuilder.AppendLine(context.Tabs + "{");
        context.TabCount++;

        foreach (var variable in context.Element.DefaultState.Variables)
        {
            if (variable.IsCustomVariable)
            {
                // assign it:
                context.StringBuilder.AppendLine($"{context.CodePrefix}.{variable.Name} = {VariableValueToGumCodeValue(variable, context)};");

            }
        }

        foreach (var instance in context.Element.Instances)
        {
            context.Instance = instance;

            FillWithNonParentVariableAssignments(context);

            TryGenerateApplyLocalizationForInstance(context, context.StringBuilder, instance);

            var instanceApi = GetVisualApiForInstance(instance, context.Element);
            var screenOrComponent = context.Element is ScreenSave
                ? "ScreenSave"
                : "ComponentSave";
            if (instanceApi == VisualApi.Gum && context.CodeOutputProjectSettings.GenerateGumDataTypes)
            {
                context.StringBuilder.AppendLine(context.Tabs + $"if({screenOrComponent}?.DefaultState != null);");
                context.TabCount++;
                context.StringBuilder.AppendLine(context.Tabs +
                    $"GumRuntime.ElementSaveExtensions.ApplyVariableReferences({context.GetInstanceNameInCode(instance)}, {screenOrComponent}.DefaultState);");
                context.TabCount--;

            }

            context.StringBuilder.AppendLine();
        }

        context.TabCount--;
        context.StringBuilder.AppendLine(context.Tabs + "}");
    }

    private static void ProcessVariableGroups(List<VariableSave> variablesToConsider, StateSave defaultState, VisualApi visualApi, StringBuilder stringBuilder, CodeGenerationContext context)
    {
        if (visualApi == VisualApi.XamarinForms)
        {
            string? baseType = null;
            if (context.Instance != null)
            {
                var standardElement = ObjectFinder.Self.GetRootStandardElementSave(context.Instance);
                baseType = standardElement?.Name;
            }
            else
            {
                baseType = context.Element.BaseType;
            }
            switch (baseType)
            {
                case "Text":
                    ProcessColorForLabel(variablesToConsider, defaultState, stringBuilder, context);
                    ProcessXamarinFormsPositionAndSize(variablesToConsider, defaultState, context.Instance, context.Element, stringBuilder, context);
                    ProcessXamarinFormsLabelBold(variablesToConsider, defaultState, context.Element, stringBuilder, context);
                    break;
                default:
                    ProcessXamarinFormsPositionAndSize(variablesToConsider, defaultState, context.Instance, context.Element, stringBuilder, context);
                    break;
            }

            // January 31, 2022
            // Does it matter if
            // we do this on all objects?
            // Does it cause performance issues?
            // Update - not all controls support this, so we need to check:
            if (context.Instance != null)
            {
                var canClipToBounds =
                    IsOfXamarinFormsType(context.Instance, "StackLayout") ||
                    IsOfXamarinFormsType(context.Instance, "AbsoluteLayout") ||
                    IsOfXamarinFormsType(context.Instance, "Frame");
                ;

                if (canClipToBounds &&
                    IsOfXamarinFormsType(context.Instance, "TabView"))
                {
                    canClipToBounds = false;
                }

                if (canClipToBounds)
                {
                    var rfv = new RecursiveVariableFinder(defaultState);

                    var clips = rfv.GetValue<bool>(context.GumVariablePrefix + "ClipsChildren");

                    stringBuilder.AppendLine($"{context.CodePrefix}.IsClippedToBounds = {clips.ToString().ToLowerInvariant()};");
                }
            }
        }
    }


    #endregion

    private static void ProcessColorForLabel(List<VariableSave> variablesToConsider, StateSave defaultState, StringBuilder stringBuilder, CodeGenerationContext context)
    {
        var rfv = new RecursiveVariableFinder(defaultState);

        var gumPrefix = context.GumVariablePrefix;

        var red = rfv.GetValue<int>(gumPrefix + "Red");
        var green = rfv.GetValue<int>(gumPrefix + "Green");
        var blue = rfv.GetValue<int>(gumPrefix + "Blue");
        var alpha = rfv.GetValue<int>(gumPrefix + "Alpha");

        var isExplicitlySet = variablesToConsider.Any(item =>
            item.Name == gumPrefix + "Red" ||
            item.Name == gumPrefix + "Green" ||
            item.Name == gumPrefix + "Blue" ||
            item.Name == gumPrefix + "Alpha");

        if (isExplicitlySet)
        {
            variablesToConsider.RemoveAll(item => item.Name == gumPrefix + "Red");
            variablesToConsider.RemoveAll(item => item.Name == gumPrefix + "Green");
            variablesToConsider.RemoveAll(item => item.Name == gumPrefix + "Blue");
            variablesToConsider.RemoveAll(item => item.Name == gumPrefix + "Alpha");

            stringBuilder.AppendLine($"{context.CodePrefix}.TextColor = Color.FromRgba({red}, {green}, {blue}, {alpha});");
        }
    }

    private static bool GetIfStateSetsAnyPositionValues(StateSave state, string prefix, List<VariableSave> variablesToConsider)
    {
        return variablesToConsider.Any(item =>
                item.Name == prefix + "X" ||
                item.Name == prefix + "Y" ||
                item.Name == prefix + "Width" ||
                item.Name == prefix + "Height" ||

                item.Name == prefix + "XUnits" ||
                item.Name == prefix + "YUnits" ||
                item.Name == prefix + "WidthUnits" ||
                item.Name == prefix + "HeightUnits" ||
                item.Name == prefix + "XOrigin" ||
                item.Name == prefix + "YOrigin"

                );
    }


    public static bool DoesTypeHaveContent(string? type)
    {
        return type?.EndsWith("/ScrollView") == true ||
                type?.EndsWith("/StickyScrollView") == true ||
                type?.EndsWith("/RefreshView") == true ||
                type?.EndsWith("/View") == true ||
                type?.EndsWith("/Frame") == true;
    }

    private static string? TryGetFullGumLineReplacement(VariableSave variable, CodeGenerationContext context)
    {
        InstanceSave? instance = context.Instance;
        var rootName = variable.GetRootName();
        #region Parent

        if (rootName == "Parent" && instance != null)
        {
            var owner = string.IsNullOrEmpty(variable.Value as string)
                ? "this"
                : variable.Value;

            if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
            {
                return $"{owner}.AddChild({context.GetInstanceNameInCode(instance)});";

            }
            else
            {
                return $"{owner}.Children.Add({context.GetInstanceNameInCode(instance)});";
            }
        }
        #endregion
        // ignored variables:
        else if (rootName == "IsXamarinFormsControl" ||
            rootName == "ExposeChildrenEvents" ||
            rootName == "IsOverrideInCodeGen")
        {
            return " ";
        }
        else if (rootName == "HasEvents")
        {
            // if this is a MonoGame project, do not return " ";
            if (context.CodeOutputProjectSettings.OutputLibrary != OutputLibrary.MonoGame)
            {
                return " ";
            }
        }
        else if (variable.IsState(context.Element))
        {
            VariableSave? rootVariable = null;
            if (instance != null)
            {
                rootVariable = ObjectFinder.Self.GetRootVariable(variable.GetRootName(), instance);
            }
            else
            {
                rootVariable = ObjectFinder.Self.GetRootVariable(variable.GetRootName(), context.Element);
            }
            
            var isVariableDefinedByStandardElement = false;
            if (rootVariable != null)
            {
                var element = ObjectFinder.Self.GetContainerOf(rootVariable);

                isVariableDefinedByStandardElement = element is StandardElementSave;
            }

            // If the element type is of type standard element, we can't assign states because there is no codegen for it that has
            // the state enums. Instead, we have to do it through the SetState method:

            //var isInstanceStandardElement = ObjectFinder.Self.GetElementSave(instance.BaseType) is StandardElementSave;

            if (isVariableDefinedByStandardElement)
            {
                return $"{context.CodePrefixNoTabs}.SetProperty(\"{variable.GetRootName()}\", \"{variable.Value}\");";
            }
        }
        else if (GetIsShouldBeLocalized(variable, context.Element.DefaultState, LocalizationManager))
        {
            string assignment = GetLocalizedLine(variable, context);

            return assignment;
        }

        return null;
    }

    private static string GetGumVariableName(VariableSave variable, CodeGenerationContext context)
    {
#if DEBUG
        if (variable == null)
        {
            throw new ArgumentNullException(nameof(variable));
        }
        if (context.CodeOutputProjectSettings == null)
        {
            throw new ArgumentNullException(nameof(context) + "." + nameof(context.CodeOutputProjectSettings));
        }
#endif

        if (variable.IsState(context.Element))
        {
            return variable.GetRootName().Replace(" ", "");
        }
        else if (variable.GetRootName() == "SourceFile")
        {
            if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGame ||
                context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.MonoGameForms)
            {
                return "SourceFileName";
            }
        }


        return variable.GetRootName().Replace(" ", "");
    }


    private static VariableSave[] GetVariablesForValueAssignmentCode(InstanceSave instance, ElementSave currentElement)
    {
        var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);
        if (baseElement == null)
        {
            // this could happen if the project references an object that has a missing type. Tolerate it, return an empty l ist
            return new VariableSave[0];
        }
        else
        {
            var baseDefaultState = baseElement?.DefaultState;
            if (baseDefaultState != null)
            {

                RecursiveVariableFinder baseRecursiveVariableFinder = new RecursiveVariableFinder(baseDefaultState);

                var defaultState = currentElement.DefaultState;
                var variablesToConsider = defaultState.Variables
                    .Where(item =>
                    {
                        return GetIfVariableShouldBeIncludedForInstance(instance, item, baseRecursiveVariableFinder);
                    })
                    .ToArray();
                return variablesToConsider;
            }
        }
        return new VariableSave[0];
    }

    private static bool GetIfVariableShouldBeIncludedForInstance(InstanceSave instance, VariableSave item, RecursiveVariableFinder baseRecursiveVariableFinder)
    {
        var shouldInclude =
                                item.Value != null &&
                                item.SetsValue &&
                                item.SourceObject == instance?.Name &&
                                item.GetRootName() != "ContainedType";

        if (shouldInclude)
        {
            var foundVariable = baseRecursiveVariableFinder.GetVariable(item.GetRootName());
            shouldInclude = foundVariable != null;
        }

        return shouldInclude;
    }

    #region Localization


    private static string GetLocalizedLine(VariableSave variable, CodeGenerationContext context)
    {
        var valueAsString = variable.Value as string;
        var formattedStringIdAssignment = string.Format(FormattedLocalizationCode, valueAsString);
        var assignment = $"{context.CodePrefixNoTabs}.{variable.GetRootName()} = {formattedStringIdAssignment};";
        return assignment;
    }

    private static bool GetIsShouldBeLocalized(VariableSave variable, StateSave defaultState, LocalizationManager localizationManager)
    {
        var toReturn = localizationManager.HasDatabase &&
            // This could be exposed of exposed, so the name wouldn't be "Text"
            //variable.GetRootName() == "Text" && 
            variable.Value is string valueAsString &&
            valueAsString?.StartsWith(StringIdPrefix) == true &&
            // This could be a leftover variable
            ObjectFinder.Self.IsVariableOrphaned(variable, defaultState) == false;

        return toReturn;
    }
    public static string StringIdPrefix = "T_";
    public static string FormattedLocalizationCode = "Strings.Get(\"{0}\")";

    private static void TryGenerateApplyLocalizationForInstance(CodeGenerationContext context, StringBuilder stringBuilder, InstanceSave instance)
    {
        var component = ObjectFinder.Self.GetComponent(instance);

        if (component != null)
        {
            var instanceComponentSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(component);

            if (instanceComponentSettings?.LocalizeElement == true)
            {
                stringBuilder.AppendLine(context.Tabs + $"{context.GetInstanceNameInCode(instance)}.ApplyLocalization();");

            }
        }
    }

    private static void GenerateApplyLocalizationMethod(ElementSave element, int tabCount, StringBuilder stringBuilder)
    {
        if (LocalizationManager.HasDatabase)
        {
            // Vic says - we may want this to be recursive eventually, but that introduces
            // some complexity. How do we know which views have a call available? 
            var line = "public void ApplyLocalization()";
            stringBuilder.AppendLine(ToTabs(tabCount) + line);
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;
            var context = new CodeGenerationContext();
            context.TabCount = tabCount;
            context.Element = element;
            foreach (var variable in element.DefaultState.Variables)
            {
                InstanceSave? instance = null;
                if (!string.IsNullOrEmpty(variable.SourceObject))
                {
                    instance = element.GetInstance(variable.SourceObject);
                }

                context.Instance = instance;
                if (instance != null)
                {
                    if (GetIsShouldBeLocalized(variable, context.Element.DefaultState, LocalizationManager))
                    {
                        string assignment = GetLocalizedLine(variable, context);
                        stringBuilder.AppendLine(ToTabs(tabCount) + assignment);
                    }
                    //else if(!string.IsNullOrEmpty(instance.BaseType))
                    //{
                    //    var instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

                    //    var isComponent = instanceBase is ComponentSave;

                    //    var shouldCallLocalize = !isComponent;

                    //    if(shouldCallLocalize)
                    //    {
                    //        stringBuilder.AppendLine(ToTabs(tabCount) + $"{instance.Name}.ApplyLocalization();");
                    //    }
                    //}
                }

                // if a component is a subcomponent which can be localized, call it:

            }
            // Why don't we call base.ApplyLocalization?
            //stringBuilder.AppendLine(ToTabs(tabCount) + "base.ApplyLocalization();");

            foreach (var instance in element.Instances)
            {
                context.Instance = instance;

                TryGenerateApplyLocalizationForInstance(context, stringBuilder, instance);
            }


            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
        }
    }
    #endregion

    #region MAUI-specific

    private static void FillWithVariableBinding(ElementSave element, StringBuilder stringBuilder, int tabCount)
    {
        var context = new CodeGenerationContext();
        context.Element = element;
        context.StringBuilder = stringBuilder;
        context.TabCount = tabCount;

        var boundVariables = new List<VariableSave>();

        foreach (var variable in element.DefaultState.Variables)
        {
            if (!string.IsNullOrEmpty(variable.ExposedAsName))
            {
                var instanceName = variable.SourceObject;
                // make sure this instance is a XamForms object otherwise we don't need to set the binding
                var isXamForms = (element.DefaultState.GetValueRecursive($"{instanceName}.IsXamarinFormsControl") as bool?) ?? false;

                if (isXamForms)
                {
                    var instance = element.GetInstance(instanceName);
                    var instanceType = GetClassNameForType(instance, VisualApi.XamarinForms, context);
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"{instanceName}.SetBinding({instanceType}.{variable.GetRootName()}Property, nameof({variable.ExposedAsName}));");
                }
            }

        }
    }

    private static string GetXamarinFormsVariableName(VariableSave variable)
    {
        var rootName = variable.GetRootName();

        switch (rootName)
        {
            case "Height": return "HeightRequest";
            case "Width": return "WidthRequest";
            case "X": return "PixelX";
            case "Y": return "PixelY";
            case "Visible": return "IsVisible";
            case "HorizontalAlignment": return "HorizontalTextAlignment";
            case "VerticalAlignment": return "VerticalTextAlignment";
            case "StackSpacing": return "Spacing";

            default: return rootName;
        }
    }

    private static void ProcessXamarinFormsLabelBold(List<VariableSave> variablesToConsider, StateSave state, ElementSave container, StringBuilder stringBuilder, CodeGenerationContext context)
    {
        var boldName = context.GumVariablePrefix + "IsBold";

        var isBold = state.GetValueOrDefault<bool>(boldName);

        variablesToConsider.RemoveAll(item => item.Name == boldName);

        if (isBold)
        {
            string prefix = "";
            if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.XamarinForms)
            {
                prefix = "Xamarin.Forms";
            }
            else if (context.CodeOutputProjectSettings.OutputLibrary == OutputLibrary.Maui)
            {
                prefix = "Microsoft.Maui.Controls";
            }
            stringBuilder.AppendLine($"{context.CodePrefix}.FontAttributes = {prefix}.FontAttributes.Bold;");

        }

    }

    private static void AddAbsoluteLayoutIfNecessary(ElementSave element, int tabCount, StringBuilder stringBuilder, CodeOutputProjectSettings? projectSettings)
    {

        var shouldAddMainLayout =
            GetIfShouldAddMainLayout(element, projectSettings);

        if (shouldAddMainLayout)
        {
            ElementSave? baseElement = null;
            if (!string.IsNullOrEmpty(element.BaseType))
            {
                baseElement = ObjectFinder.Self.GetElementSave(element.BaseType);
            }

            var baseHasMain = baseElement != null &&
                projectSettings?.BaseTypesNotCodeGenerated?.Contains(element.BaseType) != true &&
                GetIfShouldAddMainLayout(baseElement, projectSettings);
            if (!baseHasMain)
            {
                stringBuilder.Append(ToTabs(tabCount) + "protected AbsoluteLayout MainLayout{get; set;}");
            }
        }
    }

    private static BindingBehavior GetBindingBehavior(ElementSave container, string instanceName)
    {
        var isContainerXamarinForms = (container.DefaultState.GetValueRecursive("IsXamarinFormsControl") as bool?) ?? false;
        var isInstanceXamarinForms = (container.DefaultState.GetValueRecursive($"{instanceName}.IsXamarinFormsControl") as bool?) ?? false;

        if (isContainerXamarinForms && isInstanceXamarinForms)
        {
            return BindingBehavior.BindablePropertyWithBoundInstance;
        }
        else if (isContainerXamarinForms) // container xamforms, child is SkiaGum
        {
            return BindingBehavior.BindablePropertyWithEventAssignment;
        }
        else
        {
            return BindingBehavior.NoBinding;
        }
    }

    #endregion

    #region Utilities

    private static bool IsTabControl(InstanceSave instance)
    {
        var baseType = instance.BaseType;
        return
            baseType?.EndsWith("/StyledTabView") == true ||
            baseType?.EndsWith("/TabView") == true;
    }

    private static string ToTabs(int tabCount) => new string(' ', tabCount * 4);


    public static VisualApi GetVisualApiForInstance(InstanceSave instance, ElementSave elementContainingInstance, bool considerDefaultContainer = false)
    {
        var defaultState = elementContainingInstance.DefaultState;

        var isXamarinFormsControlVariable =
            $"{instance.Name}.IsXamarinFormsControl";

        if (considerDefaultContainer)
        {
            var instanceElement = ObjectFinder.Self.GetElementSave(instance);
            var defaultParent = instanceElement?.DefaultState.GetValueOrDefault<string>("DefaultChildContainer");

            if (!string.IsNullOrEmpty(defaultParent))
            {
                isXamarinFormsControlVariable = $"{instance.Name}.{defaultParent}.IsXamarinFormsControl";
            }
        }

        var isXamForms = (defaultState.GetValueRecursive(isXamarinFormsControlVariable) as bool?) ?? false;
        var visualApi = VisualApi.Gum;
        if (isXamForms)
        {
            visualApi = VisualApi.XamarinForms;
        }

        return visualApi;
    }


    public static VisualApi GetVisualApiForElement(ElementSave element)
    {
        VisualApi visualApi;
        //if(element is ScreenSave)
        //{
        // screens are always XamarinForms
        //visualApi = VisualApi.XamarinForms;
        // Update August 23, 2022
        // No, the code gen may be 
        // be used for entirely Skia
        // pages such as PDF generation.
        // Therefore, we should always look
        // to the IsXamarinFormsControl value:
        //visualApi = VisualApi.XamarinForms;
        //}
        //else
        {
            var defaultState = element.DefaultState;
            var rvf = new RecursiveVariableFinder(defaultState);
            var isXamForms = rvf.GetValue<bool>("IsXamarinFormsControl");
            if (isXamForms == true)
            {
                visualApi = VisualApi.XamarinForms;
            }
            else
            {
                visualApi = VisualApi.Gum;
            }
        }
        return visualApi;

    }

    static bool IsOfXamarinFormsType(InstanceSave instance, string xamarinFormsType)
    {
        var element = ObjectFinder.Self.GetElementSave(instance);
        if (element == null)
        {
            return false;
        }
        else
        {
            return IsOfXamarinFormsType(element, xamarinFormsType);
        }
    }

    private static bool IsOfXamarinFormsType(ElementSave? element, string xamarinFormsType)
    {
        if (element == null)
        {
            return false;
        }
        bool isRightType = element.Name.EndsWith("/" + xamarinFormsType) == true;
        if (!isRightType)
        {
            var elementBaseType = element.BaseType;

            isRightType = elementBaseType?.EndsWith("/" + xamarinFormsType) == true;
        }

        if (!isRightType)
        {
            var baseElements = ObjectFinder.Self.GetBaseElements(element);
            isRightType = baseElements.Any(item => item.BaseType?.EndsWith("/" + xamarinFormsType) == true);
        }

        return isRightType;
    }

    #endregion

}
