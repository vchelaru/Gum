# Min Width

## Introduction

The `Min Width` variable sets the minimum width in pixels. The value is applied after all other layout so it can be used to overwrite automatically-assigned width.

By default this value is `<NULL>` which means there is no `Min Width`.

<figure><img src="../../../.gitbook/assets/15_07 27 03.gif" alt=""><figcaption><p>A ColoredRectangle with no Min Width</p></figcaption></figure>

If `Min Width` is assigned (not `<NULL>`), then the effective width cannot be smaller than the `Min Width` value. The following animation shows that the width is limited to a `Min Width` of `100`.

<figure><img src="../../../.gitbook/assets/15_07 28 16.gif" alt=""><figcaption><p>Min Width set to 100 limits the ColoredRectangle's width</p></figcaption></figure>

Notice that the `Width` variable can still be set to a value smaller than `Min Width`, but it does not apply visually.

`Min Width` can also be used if `Width Units` is set to values other than `Absolute`. For example, `Min Width` can be used to limit the width of a ColoredRectangle when `Width Units` is `Relative to Parent`.

<figure><img src="../../../.gitbook/assets/15_07 30 20.gif" alt=""><figcaption><p>ColoredRectangle's width limited by its <code>Min Width</code> of 100</p></figcaption></figure>
