using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Input;

public static class CursorExtensions 
{
    /// <summary>
    /// Returns information about why events may not be happening for the argument FrameworkElement.
    /// </summary>
    /// <param name="cursor">A reference to the cursor, such as GumService.Default.Cursor</param>
    /// <param name="frameworkElement">The FrameworkElement such as a Button.</param>
    /// <returns>A string explaining why events are not being raised, or null if events should be happening.</returns>
    public static string? GetEventFailureReason(this ICursor cursor, FrameworkElement frameworkElement)
    {
        if(frameworkElement == null)
        {
            return "The argument framework element is null, so it cannot raise events";
        }
        else if(frameworkElement.Visual == null)
        {
            return $"The {frameworkElement.GetType()} has a null Visual, so it will not be able to raise events";
        }
        else
        {
            return GetEventFailureReason(cursor, frameworkElement.Visual);
        }
    }

    /// <summary>
    /// Returns information about why events may not be happening for the argument InteractiveGue.
    /// </summary>
    /// <param name="cursor">A reference to the cursor, such as GumService.Default.Cursor</param>
    /// <param name="interactiveGue">The InteractiveGue, which is usually a Visual for a Forms control.</param>
    /// <returns>A string explaining why events are not being raised, or null if events should be happening.</returns>
    public static string? GetEventFailureReason(this ICursor cursor, InteractiveGue interactiveGue)
    {
        if(interactiveGue == null)
        {
            return "No InteractiveGue was passed, so events cannot be detected";
        }

        if(interactiveGue.Visible == false)
        {
            return $"The argument {NameOrType(interactiveGue)} is invisible so it will not raise events";
        }
        if (GetInvisibleParent(interactiveGue) is GraphicalUiElement invisibleParent)
        {
            return $"The parent {invisibleParent} is invisible, preventing {interactiveGue} from raising events";
        }
        if (interactiveGue.IsEnabled == false)
        {
            return "The argument InteractiveGue is disaled so it will not raise events";
        }
        if(GetDisabledParent(interactiveGue) is GraphicalUiElement disabledParent)
        {
            return $"The parent {disabledParent} is disabled, preventing {interactiveGue} from raising events";
        }
        if(interactiveGue.HasEvents == false)
        {
            return $"The argument InteractiveGue does not have its HasEvents set to true";
        }
        if(GetNotExposedChildrenEventsParent(interactiveGue) is GraphicalUiElement)
        {
            return $"The parent {interactiveGue} does not raise its children's events, preventing {interactiveGue} from raising events";
        }
        if (interactiveGue.GetAbsoluteWidth() == 0)
        {
            return "The argument InteractiveGue has an AbsoluteWidth of 0";
        }
        if(interactiveGue.GetAbsoluteHeight () == 0)
        {
            return "The argument InteractiveGue has an AbsoluteHeight of 0";
        }
        if(Get0WidthOrHeightParent(interactiveGue) is GraphicalUiElement parentWith0WidthOrHeight)
        {
            if(parentWith0WidthOrHeight.GetAbsoluteHeight() == 0)
            {
                return $"The parent {parentWith0WidthOrHeight} has an AbsoluteHeight of 0";
            }
            else if(parentWith0WidthOrHeight.GetAbsoluteWidth() == 0)
            {
                return $"The parent {parentWith0WidthOrHeight} has an AbsoluteWidth of 0";
            }
        }

        if(interactiveGue.EffectiveManagers == null)
        {
            return $"The {NameOrType(interactiveGue)} does not have EffectiveManagers, which means it was not added to a root object, and it was not added as a descendant of a root object.";
        }


        List<GraphicalUiElement> inheritanceChain = new List<GraphicalUiElement>();

        GraphicalUiElement? current = interactiveGue;
        while(current != null)
        {
            inheritanceChain.Insert(0, current);

            current = current.Parent as GraphicalUiElement;
        }

        for(int i = 0; i < inheritanceChain.Count; i++)
        {
            var item = inheritanceChain[i];

            if(item is InteractiveGue itemAsInteractiveGue)
            {
                if(!itemAsInteractiveGue.HasCursorOver(cursor))
                {
                    return $"Item {item} does not have the cursor over it. " + GetNotOverBoundsInformation(itemAsInteractiveGue) + "\n" + GetStack(itemAsInteractiveGue);
                }
            }
            else
            {
                if(!item.HasCursorOver(cursor.XRespectingGumZoomAndBounds(), cursor.YRespectingGumZoomAndBounds()))
                {
                    return $"Item {item} does not have the cursor over it. " + GetNotOverBoundsInformation(item) + "\n" + GetStack(item);
                }
            }


            string GetNotOverBoundsInformation(GraphicalUiElement gue)
            {
                var absoluteX = gue.GetAbsoluteX();
                var absoluteY = gue.GetAbsoluteY();
                var absoluteWidth = gue.GetAbsoluteWidth();
                var absoluteHeight = gue.GetAbsoluteHeight();
                if(cursor.XRespectingGumZoomAndBounds() < absoluteX)
                {
                    return $"The cursor X={cursor.XRespectingGumZoomAndBounds()} is to the left of the element which starts at X={absoluteX}";
                }
                else if(cursor.XRespectingGumZoomAndBounds() > absoluteX + absoluteWidth)
                {
                    return $"The cursor X={cursor.XRespectingGumZoomAndBounds()} is to the right of the element which ends at X={absoluteX + absoluteWidth}";
                }
                else if(cursor.YRespectingGumZoomAndBounds() < absoluteY)
                {
                    return $"The cursor Y={cursor.YRespectingGumZoomAndBounds()} is above the element which starts at Y={absoluteY}";
                }
                else // cursor.Y > absoluteY + absoluteHeight
                {
                    return $"The cursor Y={cursor.YRespectingGumZoomAndBounds()} is below the element which ends at Y={absoluteY + absoluteHeight}";
                }
            }

        }

        if(cursor.WindowOver != null && cursor.WindowOver != interactiveGue)
        {
            return $"The cursor is over {NameOrType(cursor.WindowOver)} instead of {interactiveGue}";
        }

        var rootParent = interactiveGue.GetTopParent();
        var gumUI = GumService.Default;
        if(rootParent != gumUI.Root && rootParent != gumUI.PopupRoot && rootParent != gumUI.ModalRoot)
        {
            return $"The object must ultimately be added to one of the root objects, but it is not. The top parent {rootParent} is an orphan object";
        }

        if(rootParent != gumUI.ModalRoot && gumUI.ModalRoot.Children.Count(item => item.Visible) > 0)
        {
            var firstVisible = gumUI.ModalRoot.Children.First(item => item.Visible);

            return $"There is a modal that is blocking clicks to {interactiveGue}: {firstVisible}";
        }

        return null;

        string GetStack(GraphicalUiElement itemInStack)
        {
            List<GraphicalUiElement> inheritanceChain = new List<GraphicalUiElement>();

            GraphicalUiElement? current = interactiveGue;
            while (current != null)
            {
                inheritanceChain.Insert(0, current);

                current = current.Parent as GraphicalUiElement;
            }

            string toReturn = "";

            for(int i = 0; i < inheritanceChain.Count; i++)
            {
                var item = inheritanceChain[i];
                if(item == itemInStack)
                {
                    toReturn += new string(' ', i * 2) + "└-" + NameOrType(item) + " <---- THIS";
                }
                else
                {
                    toReturn += new string(' ', i * 2) + "└-" + NameOrType(item);
                }
                toReturn += "\n";
            }

            return toReturn;
        }
    }

    static string NameOrType(GraphicalUiElement gue)
    {
        if(string.IsNullOrEmpty(gue.Name))
        {
            return gue.GetType().Name;
        }
        else
        {
            return $"{gue.GetType().Name} named {gue.Name}";
        }
    }


    private static GraphicalUiElement Get0WidthOrHeightParent(GraphicalUiElement visual)
    {
        if(visual.Parent as GraphicalUiElement == null)
        {
            return null;
        }
        else if(visual.Parent is GraphicalUiElement parentGue && 
            (parentGue.GetAbsoluteWidth() == 0 || parentGue.GetAbsoluteHeight() == 0))
        {
            return visual.Parent as GraphicalUiElement;
        }
        else
        {
            return Get0WidthOrHeightParent(visual.Parent as GraphicalUiElement);
        }
    }

    private static GraphicalUiElement? GetInvisibleParent(GraphicalUiElement visual)
    {
        if(visual.Parent as GraphicalUiElement == null)
        {
            return null;
        }
        else if(visual.Visible == false)
        {
            return visual;
        }
        else
        {
            return GetInvisibleParent(visual.Parent as GraphicalUiElement);
        }
    }

    private static GraphicalUiElement? GetDisabledParent(GraphicalUiElement visual)
    {
        if(visual.Parent == null)
        {
            return null;
        }
        else if(visual is InteractiveGue { IsEnabled: false }  interactiveGue)
        {
            return interactiveGue;
        }
        else
        {
            return GetDisabledParent(visual.Parent as GraphicalUiElement);
        }
    }

    private static GraphicalUiElement? GetNotExposedChildrenEventsParent(GraphicalUiElement visual)
    {
        if (visual.Parent as InteractiveGue == null)
        {
            if(visual.Parent is GraphicalUiElement)
            {
                return GetNotExposedChildrenEventsParent(visual.Parent as GraphicalUiElement);
            }
            else
            {
                return null;
            }
        }
        else if (visual is InteractiveGue { ExposeChildrenEvents: false } interactiveGue)
        {
            return interactiveGue;
        }
        else
        {
            return GetNotExposedChildrenEventsParent(visual.Parent as GraphicalUiElement);
        }
    }
}
