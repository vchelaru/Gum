using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.Errors
{
    public class ErrorChecker
    {
        public ErrorViewModel[] GetErrorsFor(ElementSave element, GumProjectSave project)
        {
            var list = new List<ErrorViewModel>();

            if(element != null)
            {
                var asComponent = element as ComponentSave;

                list.AddRange(GetBehaviorErrorsFor(asComponent, project));
            }

            return list.ToArray();
        }

        private List<ErrorViewModel> GetBehaviorErrorsFor(ComponentSave component, GumProjectSave project)
        {
            List<ErrorViewModel> toReturn = new List<ErrorViewModel>();

            if(component != null)
            {
                foreach(var behaviorReference in component.Behaviors)
                {
                    var behavior = project.Behaviors.FirstOrDefault(item => item.Name == behaviorReference.BehaviorName);

                    if(behavior == null)
                    {
                        toReturn.Add(new ErrorViewModel
                        {
                            Message = $"Missing reference to behavior {behaviorReference.BehaviorName}"
                        });
                    }
                    else
                    {
                        AddBehaviorErrors(component, toReturn, behavior);
                    }
                }
            }

            return toReturn;
        }

        private static void AddBehaviorErrors(ComponentSave component, List<ErrorViewModel> toReturn, DataTypes.Behaviors.BehaviorSave behavior)
        {
            foreach (var behaviorInstance in behavior.RequiredInstances)
            {
                var candidateInstances = component.Instances.Where(item => item.Name == behaviorInstance.Name).ToList();
                if (!string.IsNullOrEmpty(behaviorInstance.BaseType))
                {
                    candidateInstances = candidateInstances.Where(item => item.IsOfType(behaviorInstance.BaseType)).ToList();
                }

                if (behaviorInstance.Behaviors.Any())
                {
                    var requiredBehaviorNames = behaviorInstance.Behaviors.Select(item => item.Name);
                    candidateInstances = candidateInstances.Where(item =>
                    {
                        bool fulfillsRequirements = false;
                        var element = ObjectFinder.Self.GetComponent(item.BaseType);
                        if (element != null)
                        {
                            var implementedBehaviorNames = element.Behaviors.Select(implementedBehavior => implementedBehavior.BehaviorName);

                            fulfillsRequirements = requiredBehaviorNames.All(required => implementedBehaviorNames.Contains(required));

                        }
                        return fulfillsRequirements;
                    }).ToList();

                }

                if (!candidateInstances.Any())
                {
                    string message = $"Missing instance with name {behaviorInstance.Name}";
                    if (!string.IsNullOrEmpty(behaviorInstance.BaseType))
                    {
                        message += $" of type {behaviorInstance.BaseType}";
                    }
                    if(behaviorInstance.Behaviors.Any())
                    {
                        if(behaviorInstance.Behaviors.Count == 1)
                        {
                            message += " with behavior type ";
                        }
                        else
                        {
                            message += " with behavior types ";
                        }
                        var behaviorsJoined = string.Join(", ", behaviorInstance.Behaviors.Select(item => item.Name).ToArray());
                        message += behaviorsJoined;
                    }

                    message += $" needed by behavior {behavior.Name}";

                    toReturn.Add(new ErrorViewModel
                    {
                        Message = message
                    });
                }
            }
        }
    }
}
