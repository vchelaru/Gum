from __future__ import annotations
import typing
from typing import Any


class IVariableFinder(typing.Any):
    def GetValue(self, *args: typing.Any, **kwargs: typing.Any) -> typing.Any: ...
