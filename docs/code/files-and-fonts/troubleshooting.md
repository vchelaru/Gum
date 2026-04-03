# Troubleshooting

## Introduction

This page provides troubleshooting steps you can perform when content is not loading properly.

## Delete Bin/Obj

By default, Gum projects copy content to the bin folder if the content is newer in source. If a file somehow becomes newer (such as by copy/pate, or reverting) in the bin folder, then the source fill will not get copied over.

You can delete bin/obj folders and re-build your project to verify that content is loading correctly. Alternatively you can try loading the .gumx file that is in the bin folder to see if it visually matches the content in source.
