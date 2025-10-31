# NineSlice

## Introduction

NineSlice is a standard component which can be used to create visual objects which can stretch to any size without creating distortion on the source image. For example, consider the following image:

![](<../../../.gitbook/assets/metalPanel_blue (1).png>)

This image could be used to create nine slices of various sizes without any distortion:

![](<../../../.gitbook/assets/NineSliceScreenShot (1).png>)

The NineSlice achieves this effect by splitting the texture into nine pieces, and scales each one differently to prevent distortion. Highlighting a nine slice shows how it is split:

![](<../../../.gitbook/assets/NineSliceSplit (1).png>)

This is achieved by splitting the texture into 1/3 sections wide and tall. The following image shows how the original image will be split:

![](<../../../.gitbook/assets/NineSliceImageSplit (1) (1).png>)

## NineSlice Texture

The simplest way to assign a texture to a NineSlice is to use a single file. Setting the _SourceFile_ to a single PNG will result in the NineSlice using that one texture, where each section of the NineSlice displays 1/3 of the width of the file and 1/3 of the height of the file.

A NineSlice's **Texture Address** property can be used to change the portion of the source texture that it uses. More info can be found in the Texture Address subpage.

Alternatively, nine files can be used to specify each section of the NineSlice independently. To use nine individual files, each file must be given a specific suffix.

The following suffixes can be added to create nine slice graphics. For example, assuming your NineSlice image is called "Image" and you are using the .png file format:

* Image\_BottomCenter.png
* Image\_BottomLeft.png
* Image\_BottomRight.png
* Image\_Center.png
* Image\_Left.png
* Image\_Right.png
* Image\_TopCenter.png
* Image\_TopLeft.png
* Image\_TopRight.png
