from __future__ import annotations
import typing
from typing import Any
from enum import IntEnum, IntFlag


class IVariableFinder(typing.Any):
    def GetValue(self, *args: Any, **kwargs: Any) -> Any: ...
