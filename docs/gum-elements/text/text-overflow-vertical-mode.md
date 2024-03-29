# Text Overflow Vertical Mode

### Introduction

Text Overflow Vertical Mode controls whether lines of text can draw outside of the bounds of the Text object vertically.

### Spill

Spill enables the drawing of text lines outside of the vertical bounds of a Text instance. The following image shows a Text instance with wrapped text using Spill.

<figure><img src="../../.gitbook/assets/29_11 30 54 (1).png" alt=""><figcaption><p>Text with Text Overflow Vertical Mode set to spill</p></figcaption></figure>

### Truncate Line

Truncate Line removes lines which fall outside of the bounds of the Text instance. The following animation shows lines of text truncating in response to changing the Text instance's height.

<figure><img src="../../.gitbook/assets/29_11 36 09.gif" alt=""><figcaption><p>Text with Text Overflow Vertical mode set to Truncate Line</p></figcaption></figure>

Note that if the Text instance's height becomes too small then all text disappears.
