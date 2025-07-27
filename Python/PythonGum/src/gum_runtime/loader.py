import pythonnet; pythonnet.load("coreclr")
import sys, clr, os
import pygame
from pygame.locals import *

# get directory of THIS script so we can find the clr files
here = os.path.dirname(os.path.abspath(__file__))
clr_path = os.path.join(here, "_clr", "net6.0")
clr_path = os.path.abspath(clr_path)  # normalize to absolute
sys.path.append(clr_path)

try:
    clr.AddReference("GumCommon") # type: ignore
    clr.AddReference("GumToPythonHelpers") # type: ignore
    #clr.AddReference("Gum") # type: ignore
    print("GumCommon reference loaded successfully.")
except Exception as e:
    print("Error loading GumCommon:", e)

from Gum import Converters
from Gum.Wireframe import GraphicalUiElement, InteractiveGue
from RenderingLibrary.Graphics import InvisibleRenderable
from RenderingLibrary.Graphics import HorizontalAlignment, VerticalAlignment
from Gum.Managers import ChildrenLayout

from GumToPython import GumToPythonHelpers


try:
    topLevel = GraphicalUiElement(InvisibleRenderable())
except Exception as e:
    print("Error creating GraphicalUiElement:", e)


def draw_rect_placeholder(gue: GraphicalUiElement, color):
    draw_rect = pygame.Rect(gue.AbsoluteX, gue.AbsoluteY, gue.Width, gue.Height)
    #print(gue.Name, gue.AbsoluteX, gue.AbsoluteY, gue.Width, gue.Height)
    pygame.draw.rect(screen, color, draw_rect)


topLevel.X = 50
topLevel.Y = 100
topLevel.Width = 200
topLevel.Height = 200



def create_and_add_child(parent: GraphicalUiElement):
    child = GraphicalUiElement(InvisibleRenderable())
    parent.Children.Add(child)

    child.X = 0
    child.XUnits = Converters.GeneralUnitType.PixelsFromSmall
    #child.XOrigin = HorizontalAlignment.Center
    child.Width = 32
    child.Y = 0
    child.YUnits = Converters.GeneralUnitType.PixelsFromSmall
    #child.YOrigin = VerticalAlignment.Center
    child.Height = 32

    add_grandchild(child)

def add_grandchild(parent: GraphicalUiElement):
    grandChild = GraphicalUiElement(InvisibleRenderable())
    parent.Children.Add(grandChild)
    grandChild.Width = 16
    grandChild.Height = 16
    grandChild.X = 0
    grandChild.Y = 0
    grandChild.XUnits = Converters.GeneralUnitType.PixelsFromMiddle
    grandChild.YUnits = Converters.GeneralUnitType.PixelsFromMiddle
    grandChild.XOrigin = HorizontalAlignment.Center
    grandChild.YOrigin = VerticalAlignment.Center


topLevel.ChildrenLayout = ChildrenLayout.AutoGridHorizontal
for i in range(20):
    create_and_add_child(topLevel)

def get_bounding_rectangle(gue: GraphicalUiElement):
    return pygame.Rect(gue.AbsoluteX, gue.AbsoluteY, gue.Width, gue.Height)

def draw_depth_first(parent: GraphicalUiElement, color):
    color = (color + 20) % 256

    if parent.ClipsChildren:
        screen.set_clip(get_bounding_rectangle(parent))
    
    draw_rect_placeholder(parent, (color, color, color))
    
    for c in parent.Children:
        gue = GumToPythonHelpers.AsGue(c)

        if not isinstance(gue, GraphicalUiElement):
            continue

        if not gue.Visible:
            continue
        
        draw_depth_first(gue, color)
    
    if parent.ClipsChildren:
        screen.set_clip(None) 

# use absoluteX and absoluteY to draw a Python sprite. 

WHITE = (255, 255, 255)
BLACK = (0, 0, 0)
GREEN = (0, 255, 0)
RED = (255, 0, 0)
BLUE = (0, 0, 255)
GRAY = (128, 128, 128)

# initialize everything for drawing
SCREEN_WIDTH = 1280
SCREEN_HEIGHT = 960
fps = 60.0
pygame.init()
screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
fps_clock = pygame.time.Clock()
time_elapsed_since_last_action = 0




# loop
while True:
    # Check for directional input
    # Go through events that are passed to the script by the window.
    for event in pygame.event.get():
        if event.type == QUIT:
            pygame.quit()
            sys.exit()
    
        # perform action on mouse click
        if event.type == MOUSEBUTTONDOWN:
            if event.button == 1:  # Left mouse button
                topLevel.ClipsChildren = not topLevel.ClipsChildren

        # change Auto grid width of toplevel with g key
        if event.type == KEYDOWN:
            if event.key == K_g:
                topLevel.AutoGridHorizontalCells += 1
                topLevel.AutoGridHorizontalCells = topLevel.AutoGridHorizontalCells % 5
                if topLevel.AutoGridHorizontalCells == 0:
                    topLevel.AutoGridHorizontalCells = 1
                print(f"AutoGridHorizontalCells: {topLevel.AutoGridHorizontalCells}")

        
        # move the top level element when pressing the arrow keys

    keys = pygame.key.get_pressed()
    for key in keys:
        if key:
            if keys[K_LEFT]:
                topLevel.X -= 10
            elif keys[K_RIGHT]:
                topLevel.X += 10
            elif keys[K_UP]:
                topLevel.Y -= 10
            elif keys[K_DOWN]:
                topLevel.Y += 10
            # Increase size of topLevel with plus key
            elif keys[K_PLUS] or keys[K_EQUALS]:
                topLevel.Width += 10
                topLevel.Height += 10
            # Decrease size of topLevel with minus key
            elif keys[K_MINUS]:
                topLevel.Width -= 10
                topLevel.Height -= 10

    # Clear screen
    screen.fill(BLACK)  # Fill the screen with black.

    
    draw_depth_first(topLevel, 20)


    pygame.display.flip()

    # Force to 60 FPS
    dt = fps_clock.tick(fps)
    time_elapsed_since_last_action += dt
