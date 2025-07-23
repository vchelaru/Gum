import pythonnet; pythonnet.load("coreclr")
import sys, clr


# sys.path.append(r"C:\git\Gum\GumCommon\bin\Debug\net6.0")
sys.path.append(r"C:\git\Gum\python\PythonGum\src\gum_runtime\_clr\net6.0")

try:
    clr.AddReference("GumCommon") # type: ignore
    print("GumCommon reference loaded successfully.")
except Exception as e:
    print("Error loading GumCommon:", e)


from Gum.Wireframe import GraphicalUiElement
from RenderingLibrary.Graphics import InvisibleRenderable

try:
    b = InvisibleRenderable()
    a = GraphicalUiElement(b)
except Exception as e:
    print("Error creating GraphicalUiElement:", e)

