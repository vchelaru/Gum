from RenderingLibrary.Graphics import IRenderableIpso
from Gum import BlendState

class PySolidRectangle(IRenderableIpso):
    __namespace__ = "PythonRuntime.Graphics"
    def __init__(self):
        # backing fields
        self._blend_state = BlendState.NonPremultiplied
        self._wrap = False
        self._visible = True
        self._absolute_visible = True
        self._x = 0.0
        self._y = 0.0
        self._z = 0.0
        self._rotation = 0.0
        self._flip_horizontal = False
        self._width = 0.0
        self._height = 0.0
        self._name = ""
        self._tag = None
        self._parent = None
        self._children = []  # should hold IRenderableIpso children
        self._color_operation = 0  # whatever default ColorOperation enum value

    # ----- IRenderable -----
    def get_BlendState(self) -> BlendState: return self._blend_state
    def get_Wrap(self): return self._wrap
    def Render(self, managers): pass
    def PreRender(self): pass

    # ----- IVisible -----
    def get_Visible(self): return self._visible
    def set_Visible(self, value): self._visible = value
    def get_AbsoluteVisible(self): return self._absolute_visible
    def get_Parent(self): return self._parent  # also used by IRenderableIpso below

    # ----- IPositionedSizedObject -----
    def get_X(self): return self._x
    def set_X(self, value): self._x = value
    def get_Y(self): return self._y
    def set_Y(self, value): self._y = value
    def get_Z(self): return self._z
    def set_Z(self, value): self._z = value
    def get_Rotation(self): return self._rotation
    def set_Rotation(self, value): self._rotation = value
    def get_FlipHorizontal(self): return self._flip_horizontal
    def set_FlipHorizontal(self, value): self._flip_horizontal = value
    def get_Width(self): return self._width
    def set_Width(self, value): self._width = value
    def get_Height(self): return self._height
    def set_Height(self, value): self._height = value
    def get_Name(self): return self._name
    def set_Name(self, value): self._name = value
    def get_Tag(self): return self._tag
    def set_Tag(self, value): self._tag = value

    # ----- IRenderableIpso -----
    def get_IsRenderTarget(self): return False
    def get_Alpha(self): return 255
    def get_ClipsChildren(self): return False
    # Parent (new IRenderableIpso)
    def set_Parent(self, value): self._parent = value
    # Children
    def get_Children(self): return self._children
    # ColorOperation
    def get_ColorOperation(self): return self._color_operation
    # SetParentDirect
    def SetParentDirect(self, newParent): self._parent = newParent
