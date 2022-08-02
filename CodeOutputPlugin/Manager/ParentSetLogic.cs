using Gum;
using Gum.DataTypes;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace CodeOutputPlugin.Manager
{
    public static class ParentSetLogic
    {
        public static void HandleVariableSet(ElementSave element, InstanceSave instance, string variableName, object oldValue)
        {
            ///////////////////////Early Out//////////////////
            if(variableName != "Parent" || instance == null)
            {
                return;
            }
            /////////////////////End Early Out////////////////

            var currentState = SelectedState.Self.SelectedStateSave;
            var rfv = new RecursiveVariableFinder(currentState);

            var newParentName = rfv.GetValue<string>($"{instance.Name}.Parent");
            InstanceSave newParent = null;
            if (!string.IsNullOrEmpty(newParentName))
            {
                newParent = element.GetInstance(newParentName);
            }

            var childResponse = CanInstanceBeChildOf(instance, newParent, element);

            if(!childResponse.Succeeded)
            {
                currentState.SetValue($"{instance.Name}.Parent", oldValue, "string");

                // Maybe an output message is not obvious enough?
                //GumCommands.Self.GuiCommands.PrintOutput(childResponse.Message);
                GumCommands.Self.GuiCommands.ShowMessage(childResponse.Message);
            }
        }

        private static GeneralResponse CanInstanceBeChildOf(InstanceSave instance, InstanceSave newParent, ElementSave element)
        {
            VisualApi parentVisualApi;
            VisualApi childVisualApi = CodeGenerator.GetVisualApiForInstance(instance, element);
            if(newParent != null)
            {
                parentVisualApi = CodeGenerator.GetVisualApiForInstance(newParent, element);
            }
            else
            {
                parentVisualApi = CodeGenerator.GetVisualApiForElement(element);
            }

            var parentType = newParent?.BaseType ?? element.BaseType;
            var isParentSkiaCanvas = parentType.EndsWith("/SkiaGumCanvasView");

            var childName = instance.Name;
            var parentName = newParent?.Name ?? element.Name;

            if (parentVisualApi == childVisualApi)
            {
                if(isParentSkiaCanvas && childVisualApi == VisualApi.XamarinForms)
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
                if(childVisualApi == VisualApi.Gum && isParentSkiaCanvas)
                {
                    // Gum child added to parent skia canvas, so that's okay:
                    return GeneralResponse.SuccessfulResponse;
                }
                else
                {

                    // they don't match, and it's not a Gum object in skia canvas:
                    var message = childVisualApi == VisualApi.Gum
                        ? $"Can't add {childName} to parent {parentName} because the parent needs to either be a SkiaGumCanvasView, or contained in a SkiaGumCanvasView"
                        : $"Can't add {childName} to parent {parentName} because the parent is in a Skia canvas and the child is a Xamarin Forms object.";
                    return GeneralResponse.UnsuccessfulWith(message);
                }
            }
        }

        internal static void HandleNewCreatedInstance(ElementSave element, InstanceSave instance)
        {
            var rfv = new RecursiveVariableFinder(element.DefaultState);
            var newParentName = rfv.GetValue<string>($"{instance.Name}.Parent");

            InstanceSave newParent = null;
            if (!string.IsNullOrEmpty(newParentName))
            {
                newParent = element.GetInstance(newParentName);
            }

            var childResponse = CanInstanceBeChildOf(instance, newParent, element);

            if(!childResponse.Succeeded)
            {
                element.Instances.Remove(instance);

                GumCommands.Self.GuiCommands.ShowMessage(childResponse.Message);

            }
        }
    }
}
