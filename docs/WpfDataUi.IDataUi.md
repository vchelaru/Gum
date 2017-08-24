---
title: IDataUi
---

# Introduction

The IDataUi is an interface that is used by the [WpfDataUi.DataUiGrid](WpfDataUi.DataUiGrid)(WpfDataUi.DataUiGrid) class to display data. This interface must be implemented by any controls which are used in the [WpfDataUi.DataUiGrid](WpfDataUi.DataUiGrid)(WpfDataUi.DataUiGrid). The WpfDataUi library contains a number of built-in controls which implement the IDataUi interface, but it can easily be extended to support more controls.

# Creating IDataUi Controls

To create a custom display:

1. Add a new User Control (WPF)
1. In the codebehind file, implement the IDataUi interface
1. This newly-created control can be used in a WpfDataUi grid by assigning the [WpfDataUi.DataTypes.InstanceMember.PreferredDisplayer](WpfDataUi.DataTypes.InstanceMember.PreferredDisplayer) property.