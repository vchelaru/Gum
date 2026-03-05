# Font Scale

## Introduction

The `Font Scale` property allows you to zoom a font in or out, effectively making the text larger or smaller. Unlike using the font size property, font scale does not create new Font files (when using standard fonts). Font scale is also necessary for resizing custom fonts.

![](<../../../.gitbook/assets/GumFontScaleTexts (1).png>)

## Details

By default Text objects `Font Scale` value of 1.

![](<../../../.gitbook/assets/FontScale1 (1).png>)

`Font Scale` can be increased, but doing so can make fonts pixelated. For example, setting the `Font Scale` value to 5 makes each pixel on the font 5x as large, resulting in a pixelated text object.

![](<../../../.gitbook/assets/FontScale5 (1).png>)

Similarly, `Font Scale` can be set to a value smaller than 1, resulting in a shrunk font.

![](<../../../.gitbook/assets/FontPoint7 (1).png>)

`Font Scale` is also multiplied by the global Font Scale value, which can be assigned at runtime or [previewed in the editor](../../editor-tab.md#font-scale).&#x20;
