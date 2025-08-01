using CodeOutputPlugin.Models;
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager
{
    public static class ParentSetLogic
    {
        private static readonly ISelectedState _selectedState = Locator.GetRequiredService<ISelectedState>();
        private static readonly IDialogService _dialogService = Locator.GetRequiredService<IDialogService>();
        private static readonly FileCommands _fileCommands = Locator.GetRequiredService<FileCommands>();
        
        public static void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue, CodeOutputProjectSettings codeOutputProjectSettings)
        {
            var currentState = _selectedState.SelectedStateSave;
            ///////////////////////Early Out//////////////////
            if(variableName != "Parent" || instance == null || currentState == null)
            {
                return;
            }
            /////////////////////End Early Out////////////////

            var rfv = new RecursiveVariableFinder(currentState);

            var newParentName = rfv.GetValue<string>($"{instance.Name}.Parent");
            InstanceSave newParent = null;
            if (!string.IsNullOrEmpty(newParentName))
            {
                // 5/18/2023
                // newParentName
                // could have a dot
                // which means instance
                // is being attached to a
                // subparent (default child
                // container). Therefore, we
                // need to find the instance recursively.

                newParent = ObjectFinder.Self.GetInstanceRecursively(element, newParentName);
                    //element.GetInstance(newParentName);
            }

            var response = CanInstanceRemainAsAChildOf(instance, newParent, element);


            if (!response.Succeeded)
            {
                currentState.SetValue($"{instance.Name}.Parent", oldValue, "string");

                // Maybe an output message is not obvious enough?
                //GumCommands.Self.GuiCommands.PrintOutput(childResponse.Message);
                _dialogService.ShowMessage(response.Message);
            }
            else if(!string.IsNullOrEmpty(response.Message))
            {
                // useful for warnings:
                _dialogService.ShowMessage(response.Message);
            }
        }

        internal static void HandleNewCreatedInstance(ElementSave element, InstanceSave instance,  CodeOutputProjectSettings codeOutputProjectSettings)
        {
           
            var rfv = new RecursiveVariableFinder(element.DefaultState);
            var newParentName = rfv.GetValue<string>($"{instance.Name}.Parent");

            InstanceSave newParent = null;

            // continue here:

            if (!string.IsNullOrEmpty(newParentName))
            {
                newParent = ObjectFinder.Self.GetInstanceRecursively(element, newParentName);
                    //element.GetInstance(newParentName);
            }
            var childResponse = CanInstanceRemainAsAChildOf(instance, newParent, element);

            if(!childResponse.Succeeded)
            {
                element.Instances.Remove(instance);

                // Since this can't be here, remove all leftover variables too
                foreach(var state in element.AllStates)
                {
                    state.Variables.RemoveAll(item => item.SourceObject == instance.Name);
                }

                _dialogService.ShowMessage(childResponse.Message);

                _fileCommands.TryAutoSaveElement(element);
            }
            else if (!string.IsNullOrEmpty(childResponse.Message))
            {
                // useful for warnings:
                _dialogService.ShowMessage(childResponse.Message);
            }
        }

        static int CountInstancesWithParent(ElementSave element, string name)
        {
            int count = 0;
            var defaultVariables = element.DefaultState.Variables;

            foreach(var variable in defaultVariables)
            {
                var isParent = variable.GetRootName() == "Parent";

                if(isParent && variable.SourceObject != null && (variable.Value as string) == name)
                {
                    count++;
                }
            }
            return count;
        }

        private static GeneralResponse CanInstanceRemainAsAChildOf(InstanceSave instance, InstanceSave newParent, ElementSave element)
        {
            var toReturn = CanInstanceBeChildBasedOnXamarinFormsSkiaRestrictions(instance, newParent, element);

            if(toReturn.Succeeded)
            {
                // even if it's okay, it could be that the parent only supports Contents and doesn't have .Children.
                // In that case, we should only allow 1 child:
                var parentType = newParent?.BaseType ?? element.BaseType;

                var hasContent = CodeGenerator.DoesTypeHaveContent(parentType);

                if (hasContent)
                {
                    var childrenCount = 
                        CountInstancesWithParent(element, newParent?.Name);

                    if (childrenCount > 1)
                    {
                        var parentName = newParent?.Name ?? element.Name;
                        var message =
                            $"Warning: {instance.Name} is being added as a child of {parentName}, but this will cause errors because {parentName} is a Xamarin Forms object which has Content. Be sure to fix this!";
                        toReturn.Succeeded = true;
                        toReturn.Message = message;
                    }
                }
            }

            return toReturn;
        }

        private static GeneralResponse CanInstanceBeChildBasedOnXamarinFormsSkiaRestrictions(InstanceSave instance, InstanceSave newParent, ElementSave element)
        {
            VisualApi parentVisualApi;
            VisualApi childVisualApi = CodeGenerator.GetVisualApiForInstance(instance, element);
            if (newParent != null)
            {
                var elementContainingInstance = element;
                if(element.Instances.Contains(newParent) == false)
                {
                    elementContainingInstance = newParent.ParentContainer;
                }

                parentVisualApi = CodeGenerator.GetVisualApiForInstance(newParent, elementContainingInstance, considerDefaultContainer:true);
            }
            else
            {
                parentVisualApi = CodeGenerator.GetVisualApiForElement(element);
            }

            var parentType = newParent?.BaseType ?? element.BaseType;
            var isParentSkiaCanvas = false;
            //parentType?.EndsWith("/SkiaGumCanvasView") == true;
            if(newParent != null)
            {
                isParentSkiaCanvas = IsSkiaCanvasRecursively(newParent);
            }
            else
            {
                isParentSkiaCanvas = IsSkiaCanvasRecursively(element);
            }

            var childName = instance.Name;
            var parentName = newParent?.Name ?? element.Name;

            if (parentVisualApi == childVisualApi)
            {
                if (isParentSkiaCanvas && childVisualApi == VisualApi.XamarinForms)
                {
                    return GeneralResponse.UnsuccessfulWith(
                        $"Can't add {childName} to parent {parentName} because the parent is a a SkiaGumCanvasView which can only contain non-XamarinForms objects");
                }
                else
                {
                    // all good!
                    return GeneralResponse.SuccessfulResponse;
                }
            }
            else
            {

                // they don't match, but we can have a special case where children can be added to a parent that is a SkiaGumCanvasView
                if (childVisualApi == VisualApi.Gum && isParentSkiaCanvas)
                {
                    // Gum child added to parent skia canvas, so that's okay:
                    return GeneralResponse.SuccessfulResponse;
                }
                else
                {

                    // they don't match, and it's not a Gum object in skia canvas:
                    var message = childVisualApi == VisualApi.Gum
                        ? $"Can't add {childName} to parent {parentName} because the {parentName} needs to either be a SkiaGumCanvasView, or contained in a SkiaGumCanvasView"
                        : $"Can't add {childName} to parent {parentName} because the {parentName} is in a Skia canvas and the {childName} is a Xamarin Forms object.";
                    return GeneralResponse.UnsuccessfulWith(message);
                }
            }
        }

        private static bool IsSkiaCanvasRecursively(InstanceSave instance)
        {
            if(instance.BaseType?.EndsWith("/SkiaGumCanvasView") == true)
            {
                return true;
            }
            else
            {
                var element = ObjectFinder.Self.GetElementSave(instance.BaseType);

                return IsSkiaCanvasRecursively(element);
            }
        }

        private static bool IsSkiaCanvasRecursively(ElementSave element)
        {
            if(element.BaseType?.EndsWith("/SkiaGumCanvasView") == true)
            {
                return true;
            }
            else
            {
                var baseElements = ObjectFinder.Self.GetBaseElements(element);

                return baseElements.Any(item => item.BaseType?.EndsWith("/SkiaGumCanvasView") == true);
            }
        }

    }
}
