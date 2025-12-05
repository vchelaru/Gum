# MaxLettersToShow

## Introduction

This value controls the number of letters that the Text object will show. This value is used when displaying the text, but is not used when calculating the Text size or line wrapping.

If the value is **\<NULL>**, then there is no maximum - all letters are displayed.

## Example

Max Letters To Show limits the number of characters (including spaces). By default this value is **\<NULL>**, which means a Text object will display its full string. Setting this value will adjust the display of the text, but it will not impact any layout values.

For example, by default a Text object displays all of its letters. Note that the Width is fixed, and the Height depends on the contained text - the Height is automatically set on the Text object according to the contents of the text.

![](<../../../.gitbook/assets/NoMaxLettersToShow (1).png>)

Setting Max Letters To Show value to 30 restricts the Text object to displaying its first 30 characters, but the size and line wrapping do not change.

![](<../../../.gitbook/assets/MaxLettersToShow30 (1).png>)

Max Letters To Show applies after all layout and text positioning has been applied. Therefore, centered text may appear off-center. The following text would appear centered if Max Letters To Show allowed the entire text to be displayed, but since it is cut-off, it appears off-center.

![](<../../../.gitbook/assets/MaxLettersToShowCentered (1).png>)
