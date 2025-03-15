# Min Height

## Introduction

The `Min Height` variable sets the minimum height in pixels. This value is applied after all other layout so it can be used to overwrite automatically-assigned height.

By default this value is `<NULL>` which means there is no `Min Height`.

<figure><img src="../../../.gitbook/assets/14_21 45 47.gif" alt=""><figcaption><p>A ColoredRectangle with no <code>Min Height</code></p></figcaption></figure>

If `Min Height` is assigned (not `<NULL>`), then the effective height cannot be less than the `Min Height` value. The following animation shows that height is limited to a `Min Height` of 100.

<figure><img src="../../../.gitbook/assets/14_21 49 29.gif" alt=""><figcaption><p><code>Min Height</code> set to <code>100</code> limits the ColoredRectangle' height</p></figcaption></figure>

Notice that the `Height` variable can still be set to a value smaller than `Min Height`, but it does not apply visually.

`Min Height` can also be used if `Height Units` is set to values other than `Absolute`. For example, `Min Height` can be used to limit the height of a ColoredRectangle when `Height Units` is `Relative to Parent`.

<figure><img src="../../../.gitbook/assets/14_21 58 04.gif" alt=""><figcaption><p>Rectangle's height limited by its <code>Min Height</code> of <code>100</code></p></figcaption></figure>
