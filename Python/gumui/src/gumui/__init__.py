import pythonnet
pythonnet.load("coreclr")
import sys, clr, os

# Load runtime helper
from .runtime import GumUI

# Set up CLR path
here = os.path.dirname(os.path.abspath(__file__))
clr_path = os.path.join(here, "_clr", "net6.0")
sys.path.append(os.path.abspath(clr_path))

# Load C# DLLs
try:
    clr.AddReference("GumCommon")
    clr.AddReference("GumToPythonHelpers")
except Exception as e:
    print("Error loading GumCommon:", e)

# Re-exported C# symbols
from Gum import Converters, BlendState, DataTypes
from Gum.DataTypes import DimensionUnitType
from Gum.Managers import ChildrenLayout
from Gum.Wireframe import GraphicalUiElement, InteractiveGue
from RenderingLibrary import IPositionedSizedObject
from RenderingLibrary.Graphics import (
    InvisibleRenderable,
    HorizontalAlignment,
    VerticalAlignment,
    IRenderableIpso,
    IRenderable,
    IVisible,
)
from GumToPython import GumToPythonHelpers

__all__ = [
    "GumUI",
    "Converters",
    "BlendState",
    "ChildrenLayout",
    "DataTypes",
    "DimensionUnitType",
    "GraphicalUiElement",
    "InteractiveGue",
    "IPositionedSizedObject",
    "InvisibleRenderable",
    "HorizontalAlignment",
    "VerticalAlignment",
    "IRenderableIpso",
    "IRenderable",
    "IVisible",
    "GumToPythonHelpers",
]
