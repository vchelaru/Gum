# Dock

## Introduction

The Dock section of the Alignment tab is used to easily adjust the variables on an instance for common layouts. All Dock options adjust the dimensions of the current instance, and most also adjust the position.

<figure><img src="../../../.gitbook/assets/01_08 28 12.png" alt=""><figcaption><p>Dock buttons in the Alignment tab</p></figcaption></figure>

Keep in mind that docking an instance does not toggle a dock state, but rather modifies variables on the instance so that it has docking behavior. This distinction has the following consequences:

* Docking behavior can be achieved by manually modifying variable - this tab does not add any additional behavior which is not already available through the Variables tab
* Docking cannot be undone by toggling the dock button, and pressing multiple dock buttons results in multiple dock variables being assigned which may result in undesirable behavior. To undo docking, use the CTRL+Z shortcut for undo.

## Dock types

This section lists all of the dock types, provides a visual examples, and lists which variables are modified.

### Top

<figure><img src="../../../.gitbook/assets/01_08 29 30.png" alt=""><figcaption></figcaption></figure>

<table data-full-width="false"><thead><tr><th>Variable</th><th>Value</th></tr></thead><tbody><tr><td>X</td><td>0</td></tr><tr><td>X Units</td><td>Pixels From Center</td></tr><tr><td>Y</td><td>0</td></tr><tr><td>Y Units</td><td>Pixels From Top</td></tr><tr><td>X Origin</td><td>Center</td></tr><tr><td>Y Origin</td><td>Top</td></tr><tr><td>Width</td><td>0</td></tr><tr><td>Width Units</td><td>Relative to Parent</td></tr></tbody></table>

### Left

<figure><img src="../../../.gitbook/assets/01_08 30 19.png" alt=""><figcaption></figcaption></figure>

|              |                    |
| ------------ | ------------------ |
| X            | 0                  |
| X Units      | Pixels From Left   |
| Y            | 0                  |
| Y Units      | Pixels From Center |
| X Origin     | Left               |
| Y Origin     | Center             |
| Height       | 0                  |
| Height Units | Relative to Parent |

### Fill

<figure><img src="../../../.gitbook/assets/01_08 30 54.png" alt=""><figcaption></figcaption></figure>

|              |                    |
| ------------ | ------------------ |
| X            | 0                  |
| X Units      | Pixels From Center |
| Y            | 0                  |
| Y Units      | Pixels From Center |
| X Origin     | Center             |
| Y Origin     | Center             |
| Width        | 0                  |
| Width Units  | Relative to Parent |
| Height       | 0                  |
| Height Units | Relative to Parent |

### Fill Vertically

<figure><img src="../../../.gitbook/assets/01_08 31 31.png" alt=""><figcaption></figcaption></figure>

|              |                    |
| ------------ | ------------------ |
| Y            | 0                  |
| Y Units      | Pixels From Center |
| Y Origin     | Center             |
| Height       | 0                  |
| Height Units | Relative to Parent |



### Right

<figure><img src="../../../.gitbook/assets/01_08 32 19.png" alt=""><figcaption></figcaption></figure>

|              |                    |
| ------------ | ------------------ |
| X            | 0                  |
| X Units      | Pixels From Right  |
| Y            | 0                  |
| Y Units      | Pixels From Center |
| X Origin     | Right              |
| Y Origin     | Center             |
| Height       | 0                  |
| Height Units | Relative to Parent |

### Fill Horizontally

<figure><img src="../../../.gitbook/assets/01_08 31 50.png" alt=""><figcaption></figcaption></figure>

|             |                    |
| ----------- | ------------------ |
| X           | 0                  |
| X Units     | Pixels From Center |
| X Origin    | Center             |
| Width       | 0                  |
| Width Units | Relative to Parent |

### Bottom

<figure><img src="../../../.gitbook/assets/01_08 32 37.png" alt=""><figcaption></figcaption></figure>

|             |                    |
| ----------- | ------------------ |
| X           | 0                  |
| X Units     | Pixels From Center |
| Y           | 0                  |
| Y Units     | Pixels From Bottom |
| X Origin    | Center             |
| Y Origin    | Bottom             |
| Width       | 0                  |
| Width Units | Relative to Parent |

### Size to Children

<figure><img src="../../../.gitbook/assets/01_08 40 13.png" alt=""><figcaption></figcaption></figure>

|              |                      |
| ------------ | -------------------- |
| Width        | 0                    |
| Width Units  | Relative to Children |
| Height       | 0                    |
| Height Units | Relative to Children |

