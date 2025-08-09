import pygame, sys
from gumui import GumUI, ChildrenLayout, GraphicalUiElement, DimensionUnitType, Converters, InvisibleRenderable, HorizontalAlignment, VerticalAlignment
from pygame.locals import *

pygame.init()

def create_and_add_child(parent: GraphicalUiElement):
    child = GraphicalUiElement(InvisibleRenderable())
    parent.Children.Add(child)
    child.XUnits = Converters.GeneralUnitType.PixelsFromSmall
    child.Width = 32
    child.YUnits = Converters.GeneralUnitType.PixelsFromSmall
    child.Height = 32
    add_grandchild(child)

def add_grandchild(parent: GraphicalUiElement):
    grandChild = GraphicalUiElement(InvisibleRenderable())
    parent.Children.Add(grandChild)
    grandChild.Width = 16
    grandChild.Height = 16
    grandChild.XUnits = Converters.GeneralUnitType.PixelsFromMiddle
    grandChild.YUnits = Converters.GeneralUnitType.PixelsFromMiddle

# use absoluteX and absoluteY to draw a Python sprite. 
BLACK = (0, 0, 0)
SCREEN_WIDTH = 1280
SCREEN_HEIGHT = 960
screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
fps_clock = pygame.time.Clock()
fps = 60.0
dt = fps_clock.tick(fps)

# Initialize GumUI
myUi = GumUI()
myUi.initialize(screen)

# Set the root to be Auto Grid, and add 20 children each with 1 grandchild
myUi.RootElement.ChildrenLayout = ChildrenLayout.AutoGridHorizontal
for i in range(20):
    create_and_add_child(myUi.RootElement)

while True:
    # Go through events that are passed to the script by the window.
    for event in pygame.event.get():
        if event.type == QUIT:
            pygame.quit()
            sys.exit()
    
        # perform action on mouse click
        if event.type == MOUSEBUTTONDOWN:
            if event.button == 1:  # Left mouse button
                myUi.RootElement.ClipsChildren = not myUi.RootElement.ClipsChildren

        # change Auto grid width of myUi.RootElement with g key
        if event.type == KEYDOWN:
            if event.key == K_g:
                myUi.RootElement.AutoGridHorizontalCells += 1
                myUi.RootElement.AutoGridHorizontalCells = myUi.RootElement.AutoGridHorizontalCells % 5
                if myUi.RootElement.AutoGridHorizontalCells == 0:
                    myUi.RootElement.AutoGridHorizontalCells = 1
                print(f"AutoGridHorizontalCells: {myUi.RootElement.AutoGridHorizontalCells}")

    keys = pygame.key.get_pressed()
    for key in keys:
        if key:
            if keys[K_LEFT]:
                myUi.RootElement.X -= 10
            elif keys[K_RIGHT]:
                myUi.RootElement.X += 10
            elif keys[K_UP]:
                myUi.RootElement.Y -= 10
            elif keys[K_DOWN]:
                myUi.RootElement.Y += 10
            # Increase size of myUi.RootElement with plus key
            elif keys[K_PLUS] or keys[K_EQUALS]:
                myUi.RootElement.Width += 10
                myUi.RootElement.Height += 10
            # Decrease size of myUi.RootElement with minus key
            elif keys[K_MINUS]:
                myUi.RootElement.Width -= 10
                myUi.RootElement.Height -= 10

    # Clear screen
    myUi.update(dt)
    screen.fill(BLACK)  # Fill the screen with black.
    myUi.draw()
    pygame.display.flip()

    # Force to 60 FPS
    dt = fps_clock.tick(fps)
