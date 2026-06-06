# Troubleshooting

## Introduction

This page provides troubleshooting steps you can perform when content is not loading properly.

## Delete Bin/Obj

By default, Gum projects copy content to the bin folder if the content is newer in source. If a file somehow becomes newer (such as by copy/pate, or reverting) in the bin folder, then the source fill will not get copied over.

You can delete bin/obj folders and re-build your project to verify that content is loading correctly. Alternatively you can try loading the .gumx file that is in the bin folder to see if it visually matches the content in source.

## Clearing Browser Cache (Web)

On web targets (KNI BlazorGL and other WebAssembly hosts), the browser may serve a cached copy of your `.gumx` project and its files. If your content looks out of date even though the source has changed, the browser cache is a likely culprit. Performing a hard refresh or clearing the browser cache makes sure you are loading the current `.gumx` file rather than a stale cached version.

