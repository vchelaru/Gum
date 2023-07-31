# Reflection

The default behavior for DataUiGrid is to reflect properties from the assigned Instance. When the Instance is assigned, the Grid automatically populates its Categories with members based on reflection.&#x20;

Properties which have the `System.ComponentModel.CategoryAttribute` will be categorized according to the category assigned. Otherwise, properties will be put in the Uncategorized category.
