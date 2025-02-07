# States

## Introduction

States are collection of variables which can be used to represent a component or screen configuration. States can be used for a variety of situations including:

* Defining the appearance of a UI element such as Enabled, Disabled, Highlighted, and Pushed
* Defining positions for interpolation and animation such as OffScreen and OnScreen
* Defining start/end or empty/full states for interpolation such as AmmoFull and AmmoEmpty
* Defining appearance in response to game-specific status such as NotJoined and PlayerJoined.

Every element automatically includes a Default state which cannot be removed. This state is automatically selected and any changes made to a component happen on the Default state unless a different state is selected.

<figure><img src="../../../.gitbook/assets/07_04 03 32.png" alt=""><figcaption><p>Default state in a component named DefaultComponent</p></figcaption></figure>

