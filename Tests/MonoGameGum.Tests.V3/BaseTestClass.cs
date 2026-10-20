using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Gum.Input;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Tests.V3;

public class BaseTestClass : IDisposable
{
    public BaseTestClass()
    {
        // The shared roots are laid out against the canvas, so restore it before laying them out.
        ResetCanvasAndRoots();

        Gum.GumService.Default.InitializeForTesting();
        CreateMockCursor();
    }

    /// <summary>
    /// Restores the canvas and the shared Root, PopupRoot and ModalRoot to their startup state.
    /// GumService.Update sizes the roots to the canvas, and Root is centered, so a test that grows
    /// the canvas, calls Update and restores the canvas leaves Root wider than the canvas. The next
    /// layout of Root then puts it at a negative position, and every element added under it lands
    /// off-screen (issue #6199).
    /// </summary>
    private static void ResetCanvasAndRoots()
    {
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;

        Gum.GumService gumService = Gum.GumService.Default;
        gumService.Root.X = 0;
        gumService.Root.Y = 0;
        gumService.Root.Width = 0;
        gumService.Root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        gumService.Root.Height = 0;
        gumService.Root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        gumService.Root.UpdateLayout();

        // Popup and modal roots are left-origin and sized to the canvas by Update.
        foreach (InteractiveGue? popupOrModalRoot in new InteractiveGue?[] { gumService.PopupRoot, gumService.ModalRoot })
        {
            if (popupOrModalRoot != null)
            {
                FormsUtilities.SetDimensionsToCanvas(popupOrModalRoot);
                popupOrModalRoot.UpdateLayout();
            }
        }
    }


    private void CreateMockCursor()
    {
        Mock<ICursor> cursor = new();
        cursor.Setup(x => x.PrimaryClick).Returns(true);
        FormsUtilities.SetCursor(cursor.Object);
        cursor.SetupProperty(x => x.VisualOver);
        cursor.SetupProperty(x => x.WindowPushed);
        cursor.Setup(x => x.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(x => x.PrimaryPush).Returns(true);
    }

    public void Dispose()
    {
        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.ClickCombos.Clear();
        FrameworkElement.ClickCombos.Add(new KeyCombo
        {
            PushedKey = Gum.Forms.Input.Keys.Enter,
            HeldKey = null,
            IsTriggeredOnRepeat = false
        });

        FrameworkElement.TabKeyCombos.Clear();
        FrameworkElement.TabKeyCombos.Add(new KeyCombo
        {
            PushedKey = Gum.Forms.Input.Keys.Tab,
            HeldKey = null,
            IsTriggeredOnRepeat = true
        });

        FrameworkElement.TabReverseKeyCombos.Clear();
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo
        {
            PushedKey = Gum.Forms.Input.Keys.Tab,
            HeldKey = Gum.Forms.Input.Keys.LeftShift,
            IsTriggeredOnRepeat = true
        });
        FrameworkElement.TabReverseKeyCombos.Add(new KeyCombo
        {
            PushedKey = Gum.Forms.Input.Keys.Tab,
            HeldKey = Gum.Forms.Input.Keys.RightShift,
            IsTriggeredOnRepeat = true
        });

        // just to remove any mocks:
        FrameworkElement.MainCursor = new Cursor(null);

        InteractiveGue.CurrentInputReceiver = null;

        Gum.GumService.Default.Root.Children.Clear();
        Gum.GumService.Default.ModalRoot.Children.Clear();
        Gum.GumService.Default.PopupRoot.Children.Clear();
        FrameworkElement.AdditionalPopupRootPairs.Clear();

        // Clear any per-capability factories a test registered into the static
        // RenderableRegistry so they don't leak into the next test.
        RenderableRegistry.Reset();

        // Last: the roots' layout depends on the canvas, and clearing their children above can lay them out.
        ResetCanvasAndRoots();
    }
}
