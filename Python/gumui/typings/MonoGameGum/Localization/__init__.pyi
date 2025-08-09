from __future__ import annotations
import typing
from typing import Any
from enum import IntEnum, IntFlag


class ILocalizationService(typing.Any):
    @property
    def CurrentLanguage(self) -> Any: ...
    @CurrentLanguage.setter
    def CurrentLanguage(self, value: Any) -> None: ...
    def Translate(self, *args: Any, **kwargs: Any) -> Any: ...
