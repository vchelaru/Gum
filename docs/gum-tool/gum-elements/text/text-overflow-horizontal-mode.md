# Text Overflow Horizontal Mode

### Introduction

Text Overflow Horizontal Mode controls how truncated words are treated. Horizontal truncation is only performed if the Text instance is using a Text Overflow Vertical Mode of Truncate Line. For information more see the [Text Overflow Vertical Mode](text-overflow-vertical-mode.md) page.

### Truncate Word

Truncate Word results in words which do not fit in the Text instance's bounds being completely removed.

The following animation shows words (and lines) truncated in response to changing a Text instance's size.

<figure><img src="../../../.gitbook/assets/29_12 07 46.gif" alt=""><figcaption><p>Truncate Word results in entire words being removed from the Text instance</p></figcaption></figure>

### Ellipsis Letter

Ellipsis Letter results in letters which do not fit in the Text instance's bounds being replaced by an ellipsis (...) which fits in the bounds of the Text instance.&#x20;

The following animation shows words (and lines) replaced by ellipsis in response to changing a Text instance's size.

<figure><img src="../../../.gitbook/assets/29_12 13 12.gif" alt=""><figcaption><p>Ellipsis Letter results in any spillover letters being replaced by an ellipsis</p></figcaption></figure>

Note that additional letters must be removed from the Text instance so the added ellipsis fits in the Text instance's bounds.
