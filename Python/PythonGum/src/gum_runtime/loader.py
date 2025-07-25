import pythonnet; pythonnet.load("coreclr")
import sys, clr
import pygame
from pygame.locals import *


sys.path.append(r"C:\git\Gum\python\PythonGum\src\gum_runtime\_clr\net6.0")

try:
    clr.AddReference("GumCommon") # type: ignore
    print("GumCommon reference loaded successfully.")
except Exception as e:
    print("Error loading GumCommon:", e)

from Gum import Converters
from Gum.Wireframe import GraphicalUiElement, InteractiveGue
from RenderingLibrary.Graphics import InvisibleRenderable
from RenderingLibrary.Graphics import HorizontalAlignment, VerticalAlignment

try:
    topLevel = GraphicalUiElement(InvisibleRenderable())
except Exception as e:
    print("Error creating GraphicalUiElement:", e)


def draw_rect_placeholder(gue: GraphicalUiElement, color):
    draw_rect = pygame.Rect(gue.AbsoluteX, gue.AbsoluteY, gue.Width, gue.Height)
    print(gue.Name, gue.AbsoluteX, gue.AbsoluteY, gue.Width, gue.Height)
    pygame.draw.rect(screen, color, draw_rect)


topLevel.X = 50
topLevel.Y = 100
topLevel.Width = 200
topLevel.Height = 200

child = GraphicalUiElement(InvisibleRenderable())
topLevel.Children.Add(child)
child.X = 0
child.XUnits = Converters.GeneralUnitType.PixelsFromSmall
child.XOrigin = HorizontalAlignment.Center
child.Y = 0
child.YUnits = Converters.GeneralUnitType.PixelsFromSmall
child.YOrigin = VerticalAlignment.Center

absoluteX = child.AbsoluteX
absoluteY = child.AbsoluteY

# from RenderingLibrary import ISystemManagers
# from RenderingLibrary import SystemManagers

# ISystemManagers.Default = SystemManagers()

# child.AddToManagers(ISystemManagers.Default)


# use absoluteX and absoluteY to draw a Python sprite. 

WHITE = (255, 255, 255)
BLACK = (0, 0, 0)
GREEN = (0, 255, 0)
RED = (255, 0, 0)
BLUE = (0, 0, 255)
GRAY = (128, 128, 128)

# initialize everything for drawing
SCREEN_WIDTH = 640
SCREEN_HEIGHT = 480
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

    # Clear screen
    screen.fill(BLACK)  # Fill the screen with black.

    
    draw_rect_placeholder(topLevel, RED)
    draw_rect_placeholder(child, WHITE)


    pygame.display.flip()

    # Force to 60 FPS
    dt = fps_clock.tick(fps)
    time_elapsed_since_last_action += dt
