# Ignored By Parent Size

### Introduction

`Ignored By Parent Size` determines whether the parent of an instance considers the instance when performing its sizing. This value is only used if the parent has a [Height Units](height-units.md#relative-to-children) or [Width Units](width-units.md#relative-to-children) of `Relative To Children`. If a parent has a `Height Units` or `Width Units` of any other value, then this value has no impact on the parent's size.

<figure><img src="../../../.gitbook/assets/08_07 42 34.gif" alt=""><figcaption><p><code>Ignored By Parent Size</code> controls whether the parent ignores the child when performing a layout</p></figcaption></figure>

`Ignored By Parent Size` does not change whether a stacking or grid parent places the instance. To take an instance out of the parent's stack or grid, see [Ignored By Parent Layout](ignored-by-parent-layout.md).

