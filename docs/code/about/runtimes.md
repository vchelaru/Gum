# Runtimes

## Introduction

Gum runs on several engines and UI frameworks. Runtimes that share rendering code are grouped, and other pages refer to a group (such as SkiaSharp-based runtimes) instead of listing every engine. This page owns which runtime belongs to which group.

## Groups

| Group | Runtimes | Rendering |
| --- | --- | --- |
| XNA-family | [MonoGame, KNI, and FNA](../getting-started/setup/adding-initializing-gum/monogame-kni-fna/README.md) | `SpriteBatch` |
| raylib | [raylib (raylib-cs)](../getting-started/setup/adding-initializing-gum/raylib-raylib-cs.md) | raylib |
| SkiaSharp-based | [.NET MAUI](../getting-started/setup/adding-initializing-gum/.net-maui.md), [WPF](../getting-started/setup/adding-initializing-gum/wpf.md), [SkiaSharp (General Canvas)](../getting-started/setup/adding-initializing-gum/skiasharp-general-canvas.md), [Silk.NET](../getting-started/setup/adding-initializing-gum/silk.net.md), [KNI with Skia](../getting-started/setup/adding-initializing-gum/kni-skia.md), [BRUTAL](../getting-started/setup/adding-initializing-gum/brutal.md), [Stride](../getting-started/setup/adding-initializing-gum/stride.md), and [Unity](../getting-started/setup/adding-initializing-gum/unity.md) (experimental) | SkiaSharp |

Each SkiaSharp-based runtime is a thin host around the same SkiaSharp rendering code, so a page that says "SkiaSharp-based runtimes" applies to all of them. KNI with Skia is in this group, not the XNA-family group, because it draws through SkiaSharp.

For the file formats each group loads, see [Supported Files](../files-and-fonts/supported-files.md).

## Other Platforms

Gum can also be used in a [Meadow](../getting-started/setup/empty-project-before-adding-gum/meadow.md) project.
