# Introduction to Gum Layout

Gum's layout engine can be used to create layouts to dock, anchor, size, and position objects responsively. This section provides an introduction to the layout engine which is used for all types of controls.

## Layout in Code and Gum Tool

The same layout engine is used in the Gum tool and all Gum runtimes. Therefore, if you are just learning to use Gum, you can learn about how Gum layout works in either environment.

Even if you intend to use Gum in a code-only environment, using the Gum tool while learning the layout engine is recommended since it is easy to experiment and get a feel for the syntax.

For example, we can create a screen and add a Container instance.

<figure><img src="../../.gitbook/assets/17_04 56 11.png" alt=""><figcaption><p>Drag+drop a Container onto a screen to add a container instance</p></figcaption></figure>

{% hint style="info" %}
A Container is used since it is the simplest type of Gum object, allowing us to focus purely on layout concepts without worrying about considerations.
{% endhint %}

Once this has been added, it can be edited in either the Variables tab or in the Editor window to immediately see how changes apply. To see the relevant code, select the Code tab.

<figure><img src="../../.gitbook/assets/17_05 06 13.png" alt=""><figcaption><p>Code tab in Gum</p></figcaption></figure>

The code tab displays the code necessary to create and perform the layout for the selected object. The code tab updates in real-time a well, so feel free to experiment.

<figure><img src="../../.gitbook/assets/17_05 09 11.gif" alt=""><figcaption><p>Changes apply in the Code tab immediately as they are made</p></figcaption></figure>

