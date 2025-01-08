# Text

## Introduction

The text object can be used to display written information. The text object supports:

* Horizontal and Vertical alignment
* Text wrapping
* Fonts from the installed font library on your machine
* Custom fonts using Bitmap Font Generator: [http://www.angelcode.com/products/bmfont/](http://www.angelcode.com/products/bmfont/)
* Scaling independent of source font
* BBCode-style inline formatting

### Text Wrapping

Texts wrap their contained words based on their dimensions. Whether a text wraps ultimately depends on whether the Text's `Width Units` is `Relative To Children`.

The following animation shows text wrapping on a text which is using an `Absolute` `Width Units`.

<figure><img src="../../../.gitbook/assets/08_14 21 31.gif" alt=""><figcaption><p>Resizing a Text causes it to wrap</p></figcaption></figure>

