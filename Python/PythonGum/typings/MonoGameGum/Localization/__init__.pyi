from __future__ import annotations
import typing
from typing import Any


class ILocalizationService(typing.Any):
    CurrentLanguage: typing.Any
    def Translate(self, *args: typing.Any, **kwargs: typing.Any) -> typing.Any: ...
