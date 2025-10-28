using Gum.Forms;
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
    public static string? GetEventFailureReason(this ICursor cursor, InteractiveGue interactiveGue)
    {
        /*
         * 
            Recursive visibility
            Recursive enabled
            Recursive ExposeChildrenEvents
            Recursive bounds checks
            Size (is anything 0 width or height)
            Is covered by other UI
            Is not added to a root
            Modal popup exists
         */
        if(interactiveGue == null)
        {
            return "No InteractiveGue was passed, so events cannot be detected";
        }

        if(interactiveGue.Visible == false)
        {
            return "The argument InteractiveGue is invisible so it will not raise events";
        }
        if (GetInvisibleParent(interactiveGue) is GraphicalUiElement invisibleParent)
        {
            return $"The parent {invisibleParent} is invisible, preventing this InteractiveGue from raising events";
        }
        if (interactiveGue.IsEnabled == false)
        {
            return "The argument InteractiveGue is disaled so it will not raise events";
        }
        if(GetDisabledParent(interactiveGue) is GraphicalUiElement disabledParent)
        {
            return $"The parent {disabledParent} is disabled, preventing this InteractiveGue from raising events";
        }
        if(interactiveGue.HasEvents == false)
        {
            return $"The argument InteractiveGue does not have its HasEvents set to true";
        }
        if(GetNotExposedChildrenEventsParent(interactiveGue) is GraphicalUiElement)
        {
            return $"The parent {interactiveGue} does not raise its children's events, preventing this InteractiveGue from raising events";
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

        if(interactiveGue.HasCursorOver(cursor) == false)
        {
            List<GraphicalUiElement> inheritanceChain = new List<GraphicalUiElement>();

            GraphicalUiElement current = interactiveGue;
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
                        return $"Item {item} does not have the cursor over it";
                    }
                }
                else
                {
                    if(!item.HasCursorOver(cursor.XRespectingGumZoomAndBounds(), cursor.YRespectingGumZoomAndBounds()))
                    {
                        return $"Item {item} does not have the cursor over it";
                    }
                }
            }
        }

        if(cursor.WindowOver != interactiveGue)
        {
            return $"The cursor is over {cursor.WindowOver} instead of {interactiveGue}";
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
