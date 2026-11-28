using Gum.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.ProjectServices.CodeGeneration
{
    public class CustomCodeGenerator
    {
        public CustomCodeGenerator(CodeGenerator codeGenerator, CodeGenerationNameVerifier nameVerifier)
        {
            _codeGenerator = codeGenerator;
            _nameVerifier = nameVerifier;
        }

        private CodeGenerator _codeGenerator;
        private readonly CodeGenerationNameVerifier _nameVerifier;

        public string GetCustomCodeForElement(ElementSave element, 
            CodeOutputElementSettings elementSettings, 
            CodeOutputProjectSettings projectSettings)
        {
            var stringBuilder = new StringBuilder();
            int tabCount = 0;

            #region Using Statements

            int resolvedSyntaxVersion = _codeGenerator.ResolveSyntaxVersion(projectSettings);

            if (!string.IsNullOrWhiteSpace(projectSettings.CommonUsingStatements))
            {
                stringBuilder.AppendLine(CodeGenerator.TranslateLegacyUsingStatements(
                    projectSettings.CommonUsingStatements, resolvedSyntaxVersion));
            }

            if (!string.IsNullOrEmpty(elementSettings?.UsingStatements))
            {
                stringBuilder.AppendLine(CodeGenerator.TranslateLegacyUsingStatements(
                    elementSettings.UsingStatements, resolvedSyntaxVersion));
            }

            bool declaresGumUI = HasGumService(projectSettings.OutputLibrary);
            if (declaresGumUI)
            {
                bool addedUsing = AppendUsingIfMissing(stringBuilder, CodeGenerator.GetGumServiceNamespace(resolvedSyntaxVersion));
                if (projectSettings.OutputLibrary == OutputLibrary.GumForms)
                {
                    // AddToRoot on a Forms control is an extension method in this namespace.
                    addedUsing |= AppendUsingIfMissing(stringBuilder, "Gum.Forms.Controls");
                }
                if (addedUsing)
                {
                    stringBuilder.AppendLine();
                }
            }
            #endregion

            #region Namespace Header/Opening {

            string namespaceName = _codeGenerator.GetElementNamespace(element, elementSettings, projectSettings);

            // Same form as the generated half of the class.
            bool hasBlockNamespace = !string.IsNullOrEmpty(namespaceName) &&
                !_codeGenerator.ResolveUseFileScopedNamespace(projectSettings);

            if (hasBlockNamespace)
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + $"namespace {namespaceName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
            }
            else if (!string.IsNullOrEmpty(namespaceName))
            {
                stringBuilder.AppendLine($"namespace {namespaceName};");
                stringBuilder.AppendLine();
            }

            #endregion

            #region Class Header/Opening {

            string classHeader = GetClassHeader(element, projectSettings);

            stringBuilder.AppendLine(ToTabs(tabCount) + classHeader);
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;
            #endregion

            if (declaresGumUI)
            {
                // Lets custom code use GumUI the way the docs do; otherwise only Game1 declares it.
                stringBuilder.AppendLine(ToTabs(tabCount) + GumUIPropertyLine);
                stringBuilder.AppendLine();
            }

            stringBuilder.AppendLine(ToTabs(tabCount) + "partial void CustomInitialize()");
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            stringBuilder.AppendLine(ToTabs(tabCount));
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");

            #region Class Closing }
            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            #endregion

            if (hasBlockNamespace)
            {
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }

            return stringBuilder.ToString();
        }

        public string GetClassHeader(ElementSave element, CodeOutputProjectSettings projectSettings)
        {
            var visualApi = _codeGenerator.GetVisualApiForElement(element);
            string inheritance = string.Empty;
            
            
            int resolvedSyntaxVersion = _codeGenerator.ResolveSyntaxVersion(projectSettings);
            if(projectSettings.InheritanceLocation == InheritanceLocation.InCustomCode)
            {
                inheritance = GetBaseClass(element, projectSettings) ?? string.Empty;
            }

            var context = new CodeGenerationContext(_nameVerifier, element);
            context.CodeOutputProjectSettings = projectSettings;
            context.ResolvedSyntaxVersion = resolvedSyntaxVersion;

            // intentionally do not include "public" or "internal" so the user can customize this as desired
            // https://github.com/vchelaru/Gum/issues/581
            var classHeader = $"partial class {_codeGenerator.GetClassNameForType(element, visualApi, context)}";
            if(!string.IsNullOrEmpty(inheritance))
            {
                classHeader += $" : {inheritance}";
            }
            return classHeader;
        }

        /// <summary>
        /// The base class the element's partial class declares, in whichever half
        /// <see cref="CodeOutputProjectSettings.InheritanceLocation"/> puts it; null when there is none.
        /// </summary>
        public string? GetBaseClass(ElementSave element, CodeOutputProjectSettings projectSettings) =>
            CodeGenerator.GetInheritance(element, projectSettings, _codeGenerator.ResolveSyntaxVersion(projectSettings), _nameVerifier,
                _codeGenerator.AreBehaviorsDisabledByEngineProfile);

        /// <summary>
        /// The property a new custom code stub declares when its output library's runtimes have a
        /// <c>GumService</c>. <see cref="CustomCodeStubDetector"/> treats it as part of the stub.
        /// </summary>
        public const string GumUIPropertyLine = "GumService GumUI => GumService.Default;";

        // The legacy Skia, Xamarin.Forms, WPF and MAUI outputs target hosts that may have no GumService.
        private static bool HasGumService(OutputLibrary outputLibrary) =>
            outputLibrary == OutputLibrary.GumForms || CodeGenerator.UsesUnifiedGumRuntime(outputLibrary);

        private static bool AppendUsingIfMissing(StringBuilder stringBuilder, string namespaceName)
        {
            string usingLine = $"using {namespaceName};";
            bool alreadyPresent = stringBuilder.ToString()
                .Split('\n')
                .Any(line => line.Trim() == usingLine);
            if (!alreadyPresent)
            {
                stringBuilder.AppendLine(usingLine);
            }
            return !alreadyPresent;
        }

        private static string ToTabs(int tabCount) => new string(' ', tabCount * 4);

    }
}
