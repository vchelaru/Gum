# SkiaGum Platform Support

## Introduction

SkiaGum refers to Skia support in Gum. SkiaGum is used in two ways:

1. Directly in a Skia canvas - In this setup, a Skia canvas is used to render Gum. This approach is used on platforms which natively support Skia. These platforms are:
   1. Silk.NET
   2. WPF
   3. .NET Maui
   4. Any other platform that supports SkiaSharp can also use SkiaGum, but you may need to create your own container platforms. At the time of this writing this includes Avalonia.
2. Rendered to a Skia canvas that is created internally, but displayed using the native rendering API. These platforms are:
   1. FlatRedBall (excluding web and console)

XNA-likes (MonoGame, KNI, and FNA) are currently not supported although support for SkiaGum in these platforms is possible with a little manual setup. NuGet packages will be provided in the future.
