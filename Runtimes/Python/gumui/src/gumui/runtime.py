import pythonnet; pythonnet.load("coreclr")
import sys, clr, os
import pygame
from pygame.locals import *

# get directory of THIS script so we can find the clr files
here = os.path.dirname(os.path.abspath(__file__))
clr_path = os.path.join(here, "_clr", "net6.0")
clr_path = os.path.abspath(clr_path)  # normalize to absolute
sys.path.append(clr_path)

# Load the C# DLLs
try:
    clr.AddReference("GumCommon") # type: ignore
    clr.AddReference("GumToPythonHelpers") # type: ignore
    #clr.AddReference("Gum") # type: ignore
    print("GumCommon reference loaded successfully.")
except Exception as e:
    print("Error loading GumCommon:", e)

# Import the Classes from the C# DLLs
from Gum import Converters, BlendState, DataTypes
from Gum.DataTypes import DimensionUnitType
from Gum.Managers import ChildrenLayout
from Gum.Wireframe import GraphicalUiElement, InteractiveGue
from RenderingLibrary import IPositionedSizedObject
from RenderingLibrary.Graphics import InvisibleRenderable, HorizontalAlignment, VerticalAlignment, IRenderableIpso, IRenderable, IVisible
from GumToPython import GumToPythonHelpers


import clr
from System import Type


class GumUI:

    def __init__(self, width=1280, height=960):
        self.width = width
        self.height = height

        self.RootElement = GraphicalUiElement(InvisibleRenderable())
        self.RootElement.X = 0
        self.RootElement.Y = 0

    def initialize(self, screen: pygame.Surface):
        self.screen = screen
        self.RootElement.Width = screen.get_width()
        self.RootElement.Height = screen.get_height()

    def update(self, datetime=None):
        self.width = self.width
        # Dummy update function, need to expand on this later

    def draw(self):
        self._draw_depth_first(self.RootElement, 20)
        
    def _draw_rect_placeholder(self, gue: GraphicalUiElement, color):
        draw_rect = pygame.Rect(gue.AbsoluteX, gue.AbsoluteY, gue.Width, gue.Height)
        pygame.draw.rect(self.screen, color, draw_rect)

        
    def _get_bounding_rectangle(self, gue: GraphicalUiElement):
        return pygame.Rect(gue.AbsoluteX, gue.AbsoluteY, gue.Width, gue.Height)

    def _draw_depth_first(self, parent: GraphicalUiElement, color):
        color = (color + 20) % 256

        if parent.ClipsChildren:
            self.screen.set_clip(self._get_bounding_rectangle(parent))
        
        self._draw_rect_placeholder(parent, (color, color, color))
        
        for c in parent.Children:
            gue = GumToPythonHelpers.AsGue(c)

            if not isinstance(gue, GraphicalUiElement):
                continue

            if not gue.Visible:
                continue
            
            self._draw_depth_first(gue, color)
        
        if parent.ClipsChildren:
            self.screen.set_clip(None) 