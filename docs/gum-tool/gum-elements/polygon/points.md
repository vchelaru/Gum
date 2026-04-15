# Points

## Introduction

Points are an ordered set of X,Y values defining the shape of the polygon. All points are relative to the Polygon's position. Typically the last point is the same as the first point creating a _closed_ polygon.

By default polygons have four sides. Since the first and last point is repeated, a four-sided polygon has five points.

<figure><img src="../../../.gitbook/assets/image (125).png" alt=""><figcaption><p>A default four-sided polygon</p></figcaption></figure>

Each point is relative to the polygon's X and Y (position). Points use pixel coordinates. The following image shows a polygon with the following points:

* -32, -32
* 32, -32
* 32, 32
* 32, -32
* -32, -32 (repeat of first point)

<figure><img src="../../../.gitbook/assets/image (126).png" alt=""><figcaption><p>Polygon with points 32 units from the origin on both X and Y</p></figcaption></figure>

Notice that the image above has points which appear above and to the left of the polygon's origin.

## Adding Points in the Editor

The easiest way to add points is by selecting a Polygon, then clicking on the + icon that appears in the center of the line where you would like to add a point. The following animation shows how to add points to a square to create an octagon:

<figure><img src="../../../.gitbook/assets/16_17 11 31.gif" alt=""><figcaption><p>Points can be added by clicking in the center of lines</p></figcaption></figure>

## Moving Points

Points can be moved by clicking on them and dragging them in the editor. Note that points can be positioned anywhere, even if lines cross or if a polygon is concave.

<figure><img src="../../../.gitbook/assets/16_17 15 21.gif" alt=""><figcaption><p>Polygons can be concave and even have crossing lines.</p></figcaption></figure>

## Removing Points

A point can be removed by clicking on it and pressing the delete key.

<figure><img src="../../../.gitbook/assets/16_17 16 54.gif" alt=""><figcaption><p>Press the delete key to remove points</p></figcaption></figure>

## Editing in Variables

Each point can be edited in the Variables tab. To edit a point, double-click the desired point and type in the new X,Y value.

<figure><img src="../../../.gitbook/assets/29_04 52 19.gif" alt=""><figcaption></figcaption></figure>

{% hint style="info" %}
Currently editing individual points does not automatically keep the first and last points equal. If editing the first or last point, the matching point must also be manually edted to match.
{% endhint %}

## Advanced Point Editing

Points can also be edited manually in the Screen or Component which contains the Polygon instance. You can open the file in a text editor to see a list of points.

For example, consider the following polygon:

<figure><img src="../../../.gitbook/assets/image (127).png" alt=""><figcaption><p>PolygonInstance in MainMenu</p></figcaption></figure>

The points for this polygon defined in the MainMenu XML file might look like this:

```markup
<VariableList xsi:type="VariableListSaveOfVector2">
  <Type>Vector2</Type>
  <Name>PolygonInstance.Points</Name>
  <IsFile>false</IsFile>
  <IsHiddenInPropertyGrid>false</IsHiddenInPropertyGrid>
  <Value>
    <Vector2>
      <X>0</X>
      <Y>46</Y>
    </Vector2>
    <Vector2>
      <X>32</X>
      <Y>32</Y>
    </Vector2>
    <Vector2>
      <X>10</X>
      <Y>0</Y>
    </Vector2>
    <Vector2>
      <X>0</X>
      <Y>46</Y>
    </Vector2>
  </Value>
</VariableList>
```

These points can be changed in the XML file. If the file is changed then Gum automatically reloads this file.

Remember that the first and last points should have the same values if you want your polygon to be closed. You can make edits in the XML file to separate the start and end if you would like to draw a segmented line rather than a closed polygon.
