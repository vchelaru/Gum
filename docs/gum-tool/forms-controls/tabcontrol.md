# TabControl

## Introduction

A TabControl has a tab strip and a content area. You build it by placing `TabItem` instances in the strip and `TabPage` instances in the content area. Tab 1 shows page 1, tab 2 shows page 2, and so on, so the tabs and pages must be in the same order.

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

For using a TabControl in code, see [TabControl](../../code/controls/tabcontrol.md).

## Adding Tabs and Pages

The TabControl component has two slots: `InnerPanelInstance` is the tab strip and `ContentPanelInstance` is the content area. See [Is Slot](../gum-elements/general-properties/is-slot.md) for how slots work.

1. Add a TabControl instance to a screen or component.
2. Drag a `TabItem` onto the TabControl. The TabControl's [Default Slot](../gum-elements/component/default-child-container.md) is the tab strip, so the TabItem's `Parent` becomes `TabControlInstance.InnerPanelInstance`.
3. Set `Header` on the TabItem to the text of the tab. Each new TabItem reads `Tab` until you change it.
4. Drag a `TabPage` onto the TabControl. Change its `Parent` to `TabControlInstance.ContentPanelInstance`, since the default slot would put it in the tab strip.
5. Drag the content of the page onto the TabPage. A TabPage's default slot is its own content container.
6. Repeat steps 2 to 5 for each tab, adding the TabItems and the TabPages in the same order.

Replace `TabControlInstance` above with the name of your instance.

## Selected Tab

`SelectedIndex` on the TabControl instance picks the selected tab, starting at `0`. The tool shows the selected tab and its page, and hides the other pages. Changing `SelectedIndex` is a quick way to check each page while you edit it.
