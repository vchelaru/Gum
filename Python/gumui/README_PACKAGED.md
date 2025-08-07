# Gum UI
Gum is not an acronym, or a backronym.  It is just the name of the User Interface engine.

This project utilizes the GUM Layout engine from the Flat Red Ball C# Open Source project here https://docs.flatredball.com/gum

If you want to contribute to the Layout engine, or this Python Rendering Engine visit us on github: https://github.com/vchelaru/Gum

# Experimental Note

> **_WARNING:_** This project is an experiment and proof of concept.  The goal was to see if PythonNET could be used to export the C# GUM UI backend Layout Engine to be used in other tools like Python with PyGame.

Currently it "works" but there are many missing features and bugs.

Right now it only draws a RECTANGLE the encompasses the entire size of the GUM UI's GraphicalUiElement.

You can then add other objects to this root object (that are currently only drawing rectangles).

You can't control the color, it starts at (20, 20, 20) and increases in color intensity by 20 and wraps around at 256.

If this ends up being something that others want, please join the [discord](https://discord.gg/EvqwmSQuBz) or put tickets into [github](https://github.com/vchelaru/Gum).

# Setup

Install gumui and pygame 
- pip install gumui
- pip install pygame

# Getting started with

There are really only 3 important things after you import.  Create an Instance of `GumUI`, call Update, and call `Draw()`

1. First you need to import the `GumUI` and `PyGame`
```python
from gumui import GumUI, GraphicalUiElement, InvisibleRenderable, Converters
import pygame
from pygame.locals import *
```
2. Next you need to initialize and configure pygame, might as well do it now
```python
pygame.init()
BLACK = (0, 0, 0)
SCREEN_WIDTH = 680
SCREEN_HEIGHT = 480
screen = pygame.display.set_mode((SCREEN_WIDTH, SCREEN_HEIGHT))
```
3. Setup timing to force to 60 FPS (Frames Per Second)
```python
fps_clock = pygame.time.Clock()
fps = 60.0
dt = fps_clock.tick(fps)
```
4. Once PyGame is setup, you need to create the GumUI and initialize it
```python
# Initialize GumUI
myUi = GumUI()
myUi.initialize(screen) # `screen` is a Surface you wish GUM to draw to https://www.pygame.org/docs/ref/surface.html
```
5. Lets add a single item to the UI for now it will be simple 32x32 square
```python
child = GraphicalUiElement(InvisibleRenderable())
myUi.RootElement.Children.Add(child)
child.XUnits = Converters.GeneralUnitType.PixelsFromSmall
child.Width = 32
child.YUnits = Converters.GeneralUnitType.PixelsFromSmall
child.Height = 32
```
6. Now that you've defined the UI (1 square inside the root element) you can create the standard pygame loop
```python
while True:
    for event in pygame.event.get():
        if event.type == QUIT:
            pygame.quit()
            sys.exit()
```
> **_NOTE:_** This will run forever, or until you hit ESC or press the X
7. Update and then Draw the UI elements
```python
    myUi.update()
    screen.fill(BLACK)  # Fill the screen with black.
    myUi.draw()
    pygame.display.flip()
    dt = fps_clock.tick(fps)
```

# Adding more UI elements

Currently this is a work in progress, as such, the only thing that is drawn are squares.

To add another UI element "square" you create an instance of GraphicalUiElement, set it's properties, and add it to a parent.
```python
child = GraphicalUiElement(InvisibleRenderable())
myUi.RootElement.Children.Add(child)

child.X = 100
child.Y = 100
child.Width = 200
child.YUnits = Converters.GeneralUnitType.PixelsFromSmall
child.Height = 200
```

# Adding grandchildren and great grandchildren

Here another child (100x100) is added to the larger (200x200) child from above.

```python
grandChild = GraphicalUiElement(InvisibleRenderable())
child.Children.Add(grandChild)

grandChild.XUnits = Converters.GeneralUnitType.PixelsFromSmall
grandChild.X = 10
grandChild.Y = 10
grandChild.Width = 100
grandChild.YUnits = Converters.GeneralUnitType.PixelsFromSmall
grandChild.Height = 100
```

Notice how the (100x100) rectangle is drawn at (10, 10), but that position is relative to the parent so it's actually drawn at (110, 110)

To see the full list of properties available for a GraphicalUiElement see this documentation
https://docs.flatredball.com/gum/code/gum-code-reference/graphicaluielement

# Contributing

This is a FOSS (Free and Open Source Software) project.

- Github: https://github.com/vchelaru/Gum
- Discord: https://discord.gg/EvqwmSQuBz
- Documentation: https://docs.flatredball.com/gum

# Change Log

## 0.1.3

* Added this changelog

## 0.1.2

* Added missing import of Converts to the from line
* Added a grandchild example

## 0.1.1

* First proof of concept working release

## 0.1.0

* Deleted first push attempt