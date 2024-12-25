---
title: IDataUi
---

# IDataUi

## Introduction

&#x20;IDataUi is an interface used by [WpfDataUi.DataUiGrid](datauigrid/) to display data. This interface must be implemented by any controls which are used in the [WpfDataUi.DataUiGrid](datauigrid/). The WpfDataUi library contains a number of built-in controls which implement the IDataUi interface, but it can be extended to support more controls.

## Creating IDataUi Controls

To create a custom display:

1. Add a new User Control (WPF)
2. In the code behind file, implement the IDataUi interface
3. This newly-created control can be used in a WpfDataUi grid by assigning the WpfDataUi.DataTypes.InstanceMember.PreferredDisplayer property.
