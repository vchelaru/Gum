using Gum.DataTypes;
using Gum.Wireframe;
#if !NO_XNA
using RenderingLibrary;
using RenderingLibrary.Graphics;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using DynamicExpresso;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace GumRuntime
{
    public static class ElementSaveExtensions
    {
        static Dictionary<string, Type> mElementToGueTypes = new Dictionary<string, Type>();

        public static void RegisterGueInstantiationType(string elementName, Type gueInheritingType)
        {
            mElementToGueTypes[elementName] = gueInheritingType;
        }

#if !NO_XNA 
        public static GraphicalUiElement CreateGueForElement(ElementSave elementSave, bool fullInstantiation = false, string genericType = null)
        {
            GraphicalUiElement toReturn = null;

            var elementName = elementSave.Name;
            if (!string.IsNullOrEmpty(genericType))
            {
                elementName = elementName + "<T>";
            }
            if (mElementToGueTypes.ContainsKey(elementName))
            {
                // This code allows sytems (like games that use Gum) to assign types
                // to their GraphicalUiElements so that users of the code can work with
                // strongly-typed Gum objects.
                var type = mElementToGueTypes[elementName];

                if (!string.IsNullOrEmpty(genericType))
                {
                    type = type.MakeGenericType(mElementToGueTypes[genericType]);
                }
                var constructor = type.GetConstructor(new Type[] { typeof(bool), typeof(bool) });


                toReturn = constructor.Invoke(new object[] { fullInstantiation, true }) as GraphicalUiElement;
            }
            else
            {
                toReturn = new GraphicalUiElement();
            }
            toReturn.ElementSave = elementSave;
            return toReturn;
        }

        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave)
        {
            var toReturn = ToGraphicalUiElement(elementSave, SystemManagers.Default, addToManagers: true);

#if DEBUG
            if (GraphicalUiElement.MissingFileBehavior == MissingFileBehavior.ThrowException)
            {
                ThrowMissingFileExceptionsRecursively(toReturn);
            }
#endif

            return toReturn;
        }

        private static void ThrowMissingFileExceptionsRecursively(GraphicalUiElement graphicalUiElement)
        {
#if MONOGAME
            // We can't throw exceptions when assigning values on fonts because the font values get set one-by-one
            // and the end result of all values determines which file to load. For example, an object may set the following
            // variables one-by-one:
            // * FontSize
            // * Font
            // * OutlineThickness
            // Let's say the Font gets set to Arial. The FontSize may not have been set yet, so whatever value happens
            // to be there will be used to load the font (like 12). But the user may not have Arial12 in their project,
            // and if we threw an exception on-the-spot, the user would see a message about missing Arial12, even though
            // the project doesn't actually use Arial12.
            // We need to wait until the graphical UI element is fully created before we try to throw an exception, so
            // that's what we're going to do here:
            if (graphicalUiElement != null && graphicalUiElement.RenderableComponent is Text)
            {
                // check it
                var asText = graphicalUiElement.RenderableComponent as Text;
                if (asText.BitmapFont == null)
                {
                    if (graphicalUiElement.UseCustomFont)
                    {
                        var fontName = ToolsUtilities.FileManager.Standardize(graphicalUiElement.CustomFontFile, preserveCase: true, makeAbsolute: true);

                        throw new System.IO.FileNotFoundException($"Missing:{fontName}");
                    }
                    else
                    {
                        if (graphicalUiElement.FontSize > 0 && !string.IsNullOrEmpty(graphicalUiElement.Font))
                        {
                            string fontName = global::RenderingLibrary.Graphics.Fonts.BmfcSave.GetFontCacheFileNameFor(
                                graphicalUiElement.FontSize,
                                graphicalUiElement.Font,
                                graphicalUiElement.OutlineThickness,
                                graphicalUiElement.UseFontSmoothing,
                                graphicalUiElement.IsItalic,
                                graphicalUiElement.IsBold);

                            var standardized = ToolsUtilities.FileManager.Standardize(fontName, preserveCase: true, makeAbsolute: true);

                            throw new System.IO.FileNotFoundException($"Missing:{standardized}");
                        }

                    }

                }
            }
#endif

            foreach (var element in graphicalUiElement.ContainedElements)
            {
                ThrowMissingFileExceptionsRecursively(element);
            }
        }

        public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers systemManagers,
            bool addToManagers)
        {
            GraphicalUiElement toReturn = CreateGueForElement(elementSave);

            elementSave.SetGraphicalUiElement(toReturn, systemManagers);

            //no layering support yet
            if (addToManagers)
            {
                toReturn.AddToManagers(systemManagers, null);
            }

            return toReturn;
        }

        public static void SetStatesAndCategoriesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            if (graphicalElement == null)
            {
                throw new ArgumentNullException(nameof(graphicalElement));
            }
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {
                    graphicalElement.SetStatesAndCategoriesRecursively(baseElementSave);
                }
            }

            // We need to set categories and states before calling SetGraphicalUiElement so that the states can be used
            foreach (var category in elementSave.Categories)
            {
                graphicalElement.AddCategory(category);
            }

            graphicalElement.AddStates(elementSave.States);
        }

        public static void CreateGraphicalComponent(this GraphicalUiElement graphicalElement, ElementSave elementSave, SystemManagers systemManagers)
        {
            IRenderable containedObject = null;

            bool handled = InstanceSaveExtensionMethods.TryHandleAsBaseType(elementSave.Name, systemManagers, out containedObject);

#if GUM
            if (!handled)
            {
                string type = (elementSave is StandardElementSave) 
                    ? elementSave.Name 
                    : elementSave.BaseType;

                containedObject =
                    Gum.Plugins.PluginManager.Self.CreateRenderableForType(type);

                handled = containedObject != null;
            }
#endif

            if (handled)
            {
                graphicalElement.SetContainedObject(containedObject);
            }
            else
            {
                if (elementSave != null && elementSave is ComponentSave)
                {
                    var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);

                    if (baseElement != null)
                    {
                        graphicalElement.CreateGraphicalComponent(baseElement, systemManagers);
                    }
                }
            }
        }
        public static void AddExposedVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        {
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {
                    graphicalElement.AddExposedVariablesRecursively(baseElementSave);
                }
            }


            if (elementSave != null)
            {
                foreach (var variable in elementSave.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
                {
                    graphicalElement.AddExposedVariable(variable.ExposedAsName, variable.Name);
                }
            }

        }


        // Replaced with ApplyDefaultState
        //static void SetVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave)
        //{
        //    graphicalElement.SetVariablesRecursively(elementSave, elementSave.DefaultState);
        //}

        public static void SetVariablesRecursively(this GraphicalUiElement graphicalElement, ElementSave elementSave, Gum.DataTypes.Variables.StateSave stateSave)
        {
            if (!string.IsNullOrEmpty(elementSave.BaseType))
            {
                var baseElementSave = Gum.Managers.ObjectFinder.Self.GetElementSave(elementSave.BaseType);
                if (baseElementSave != null)
                {

                    graphicalElement.SetVariablesRecursively(baseElementSave, baseElementSave.DefaultState);
                }
            }

            graphicalElement.ApplyState(stateSave);

            ApplyVariableReferences(graphicalElement, stateSave);
        }

        public static void ApplyVariableReferences(this ElementSave element, StateSave stateSave)
        {
            foreach (var variableList in stateSave.VariableLists)
            {
                if (variableList.GetRootName() == "VariableReferences" && variableList.ValueAsIList.Count > 0)
                {
                    if (variableList.SourceObject == null)
                    {
                        foreach (string referenceString in variableList.ValueAsIList)
                        {
                            ApplyVariableReferencesOnSpecificOwner((InstanceSave)null, referenceString, stateSave);
                        }
                    }
                    else
                    {
                        InstanceSave instance = element.GetInstance(variableList.SourceObject);
                        if(instance != null)
                        {
                            foreach(string referenceString in variableList.ValueAsIList)
                            {
                                ApplyVariableReferencesOnSpecificOwner(instance, referenceString, stateSave);
                            }
                        }
                    }
                }
            }
        }

        // TODO: If result is null, variable not found, so add to some error list
        private static LiteralExpressionSyntax GetExpressionSyntaxForVariable(StateSave state, string variableName) {
            var idVar = state?.GetVariableRecursive(variableName);

            if (idVar == null) return null;

            var varType = idVar.GetRuntimeType();

            switch (Type.GetTypeCode(varType)) {
                case TypeCode.Boolean: return SyntaxFactory.LiteralExpression((bool)idVar.Value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
                case TypeCode.Empty: return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                case TypeCode.Byte: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((byte)idVar.Value));
                case TypeCode.SByte: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((sbyte)idVar.Value));
                case TypeCode.Char: return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal((char)idVar.Value));
                case TypeCode.Int16: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((short)idVar.Value));
                case TypeCode.UInt16: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((ushort)idVar.Value));
                case TypeCode.Int32: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)idVar.Value));
                case TypeCode.UInt32: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((uint)idVar.Value));
                case TypeCode.Int64: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((long)idVar.Value));
                case TypeCode.UInt64: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((ulong)idVar.Value));
                case TypeCode.Single: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((float)idVar.Value));
                case TypeCode.Double: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((double)idVar.Value));
                case TypeCode.Decimal: return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((decimal)idVar.Value));
                case TypeCode.String: return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal((string)idVar.Value));
                default: throw new Exception("Unknown type");
            }
        }

        public static void ApplyVariableReferences(this GraphicalUiElement graphicalElement, StateSave stateSave)
        {
            foreach (var variableList in stateSave.VariableLists)
            {
                if (variableList.GetRootName() == "VariableReferences" && variableList.ValueAsIList.Count > 0)
                {
                    if (variableList.SourceObject == null)
                    {
                        foreach (string referenceString in variableList.ValueAsIList)
                        {
                            ApplyVariableReferencesOnSpecificOwner(graphicalElement, referenceString, stateSave);
                        }
                    }
                    else
                    {
                        GraphicalUiElement instance = null;

                        if (graphicalElement.Tag is InstanceSave asInstanceSave && asInstanceSave.Name == variableList.SourceObject)
                        {
                            instance = graphicalElement;
                        }
                        else
                        {
                            // Give preferential treatment to the children of graphicalElement. If none are found, then go to the managers
                            // 
                            instance = graphicalElement.GetGraphicalUiElementByName(variableList.SourceObject);
                        }

                        if (instance != null)
                        {
                            foreach (string referenceString in variableList.ValueAsIList)
                            {
                                ApplyVariableReferencesOnSpecificOwner(instance, referenceString, stateSave);
                            }
                        }
                    }
                }
            }
        }

        static char[] equalsArray = new char[] { '=' };

        private static void FindReplacement(SyntaxNode sourceNode, ICollection<CSharpSyntaxNode> results) {
            if (sourceNode is MemberAccessExpressionSyntax nodeMember) {
                results.Add(nodeMember);
            } else if (sourceNode is IdentifierNameSyntax nodeId) {
                results.Add(nodeId);
            } else {
                foreach (var syntaxNode in sourceNode.ChildNodes()) {
                    FindReplacement(syntaxNode, results);
                }
            }
        }

        private static LiteralExpressionSyntax ParseExpressionAliasId(AliasQualifiedNameSyntax aliasLeft, IdentifierNameSyntax idAliasRight, string extraMembers) {
            var varNameLeft = idAliasRight.Identifier.Text.Trim();
            var variableName = extraMembers != null ? varNameLeft + "." + extraMembers : varNameLeft;

            switch (aliasLeft.Alias.ToString()) {
                case "Screens": {
                    var screen = ObjectFinder.Self.GetScreen(aliasLeft.Name.ToString());

                    return GetExpressionSyntaxForVariable(screen?.DefaultState, variableName);
                }
                case "Components": {
                    var comp = ObjectFinder.Self.GetComponent(aliasLeft.Name.ToString());

                    return GetExpressionSyntaxForVariable(comp?.DefaultState, variableName);
                }
            }

            return null;
        }

        private static bool ApplyExpression(InstanceSave instanceLeft, string left, string expression, out object result) {
            var currentScreenOrComponent = ObjectFinder.Self.GetContainerOf(instanceLeft);

            var sourceNode = CSharpSyntaxTree.ParseText(expression).GetRoot() as CSharpSyntaxNode;

            var toReplace = new List<CSharpSyntaxNode>();
            FindReplacement(sourceNode, toReplace);

            try {
                sourceNode = sourceNode.ReplaceNodes(toReplace, (node, nodeWithSubst) => {
                    switch (node) {
                        case IdentifierNameSyntax nodeId: {
                            var variableName = instanceLeft.Name + "." + nodeId.Identifier.Text;

                            return GetExpressionSyntaxForVariable(currentScreenOrComponent.DefaultState, variableName);
                        }

                        case MemberAccessExpressionSyntax nodeMember: {
                            var children = nodeMember.ChildNodes().ToArray();

                            if (children.Length != 2) {
                                // ERROR
                                return node;
                            }

                            if (children[0] is IdentifierNameSyntax idLeft && children[1] is IdentifierNameSyntax idRight) {
                                return GetExpressionSyntaxForVariable(currentScreenOrComponent.DefaultState, idLeft + "." + idRight);
                            } else if (children[0] is AliasQualifiedNameSyntax aliasLeft0 && children[1] is IdentifierNameSyntax idAliasRight0) {
                                return ParseExpressionAliasId(aliasLeft0, idAliasRight0, null);
                            } else if (children[0] is MemberAccessExpressionSyntax memberLeft && children[1] is IdentifierNameSyntax idRightMember) {
                                var memberChildren = memberLeft.ChildNodes().ToArray();
                                if (memberChildren.Length != 2) {
                                    // ERROR
                                    return node;
                                }

                                if (memberChildren[0] is AliasQualifiedNameSyntax aliasLeft && memberChildren[1] is IdentifierNameSyntax idAliasRight) {
                                    return ParseExpressionAliasId(aliasLeft, idAliasRight, idRightMember.Identifier.Text.Trim());
                                } else {
                                    // Unsupported!
                                }
                            } else {
                                // Unsupported!
                            }

                            break;
                        }

                        default:
                            // ERROR
                            return node;
                    }
                    return node;
                    // return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(42));
                });

                var interpreter = new Interpreter(InterpreterOptions.PrimitiveTypes | InterpreterOptions.SystemKeywords);
                var parsedExpression = interpreter.Parse(sourceNode.ToString());
                result = parsedExpression.Invoke();

                var variableLeft = currentScreenOrComponent.DefaultState.GetVariableRecursive(instanceLeft.Name + "." + left);
                var variableLeftType = variableLeft.GetRuntimeType();

                try {
                    result = Convert.ChangeType(result, variableLeftType);
                }
                catch (Exception ex) {
                    // TODO: Show error
                    return false;
                }

                return true;
            }
            catch (Exception ex) {
                // TODO: ERROR!!
                Console.Error.Write(ex);
                result = null;

                return false;
            }
        }

        public static void ApplyVariableReferencesOnSpecificOwner(GraphicalUiElement referenceOwner, string referenceString, StateSave stateSave)
        {
            var split = referenceString
                .Split(equalsArray, 2, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim()).ToArray();
            object value = null;
            string left = "";

            if(split.Length > 1)
            {
                left = split[0];

                if (!ApplyExpression(referenceOwner.Tag as InstanceSave, left, split[1], out value)) {
                    return;
                }
            }


            if (value != null)
            {
                referenceOwner.SetProperty(left, value);
            }
        }

        private static void ApplyVariableReferencesOnSpecificOwner(InstanceSave instance, string referenceString, StateSave stateSave)
        {
            var split = referenceString
                .Split(equalsArray, 2, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim()).ToArray();

            if(split.Length != 2)
            {
                return;
            }

            var left = split[0];
            var right = split[1];

            if (!ApplyExpression(instance, left, right, out var value)) {
                return;
            }

            if (value != null)
            {
                if(instance == null)
                {
                    stateSave.SetValue(left, value, instance);
                }
                else
                {
                    stateSave.SetValue($"{instance.Name}.{left}", value, instance);
                }
            }
        }



        private static void GetRightSideAndState(InstanceSave instanceSave, ref string right, ref StateSave stateSave)
        {
            var isExternalElement = right.Contains("/");

            if (isExternalElement)
            {
                var lastDot = right.LastIndexOf('.');
                var firstDot = right.IndexOf('.');

                var elementNameToFind = right.Substring(0, firstDot);

                if (elementNameToFind.StartsWith("Components/"))
                {
                    var stripped = elementNameToFind.Substring("Components/".Length);

                    var element = ObjectFinder.Self.GetComponent(stripped);

                    if(element != null)
                    {
                        stateSave = GetRightSide(ref right, firstDot, element);
                    }
                }
                else if(elementNameToFind.StartsWith("Screens/"))
                {
                    var stripped = elementNameToFind.Substring("Screens/".Length);

                    var element = ObjectFinder.Self.GetScreen(stripped);

                    if(element != null)
                    { 
                        stateSave = GetRightSide(ref right, firstDot, element);
                    }
                }
            }
            else
            {
                var isQualified = right.Contains('.');
                if (!isQualified && instanceSave != null)
                {
                    right = instanceSave.Name + "." + right;
                }
            }

        }

        private static StateSave GetRightSide(ref string right, int firstDot, ElementSave element)
        {
            StateSave stateSave = element.DefaultState;
            right = right.Substring(firstDot + 1);

            if (right.Contains("."))
            {
                var dotAfterInstance = right.IndexOf(".");
                var instanceName = right.Substring(0, dotAfterInstance);
                var instance = element.GetInstance(instanceName);
                GetRightSideAndState(instance, ref right, ref stateSave);
            }

            return stateSave;
        }

        public static void SetGraphicalUiElement(this ElementSave elementSave, GraphicalUiElement toReturn, SystemManagers systemManagers)
        {
            // We need to set categories and states first since those are used below;
            toReturn.SetStatesAndCategoriesRecursively(elementSave);

            toReturn.CreateGraphicalComponent(elementSave, systemManagers);

            toReturn.AddExposedVariablesRecursively(elementSave);

            toReturn.CreateChildrenRecursively(elementSave, systemManagers);

            toReturn.Tag = elementSave;

            toReturn.SetInitialState();
        }
#endif


    }
}
