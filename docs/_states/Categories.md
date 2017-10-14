---
title: Categories
order: 1
---

# Introduction

Categories can be used to organize similar states into one group (such as a button's Pressed and Unpressed states). 

Link test 1:

For a tutorial on working with categories, see the [State Categories Tutorial](/Gum/tutorials/Usage Guide _ State Categories.html)

A category can contain one or more states. States within a category have special behavior:

1. If one state in a category explicitly sets a variable (such as X), then all other states in that category will also explicitly set the variable.
1. Each category can be set individually on an instance of a component or standard element. In other words, if a component has two categories, each category can be assigned to a state within that category independently.



