using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Gum.DataTypes.Variables;

namespace Gum.Wireframe.Editors.Handlers;

/// <summary>
/// Writes a polygon's edited points into the selected state's <c>Points</c> list variable.
/// </summary>
internal static class PolygonPointsStateWriter
{
    public static void Write(EditorContext context, IReadOnlyList<Vector2> points)
    {
        string variableName = "Points";
        if (context.SelectedState.SelectedInstance != null)
        {
            variableName = context.SelectedState.SelectedInstance.Name + "." + variableName;
        }

        StateSave? stateSave = context.SelectedState.SelectedStateSave;
        if (stateSave == null) return;

        VariableListSave? pointsVariableList =
            stateSave.VariableLists.FirstOrDefault(item => item.Name == variableName);

        if (pointsVariableList == null)
        {
            pointsVariableList = new VariableListSave<Vector2>();
            pointsVariableList.Name = variableName;
            pointsVariableList.Type = "Vector2";
            stateSave.VariableLists.Add(pointsVariableList);
        }
        pointsVariableList.ValueAsIList = points.ToList();
    }
}
