from __future__ import annotations
import typing
from typing import Any
from enum import IntEnum, IntFlag


class Blend(IntEnum):
    Normal = 0
    Additive = 1
    Replace = 2
    SubtractAlpha = 3
    ReplaceAlpha = 4
    MinAlpha = 5
