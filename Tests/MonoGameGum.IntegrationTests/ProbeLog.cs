using System.Diagnostics;
using System.Reflection;

namespace MonoGameGum.IntegrationTests;

// TEMPORARY (#6092): one CSV row per test with thread, handle, working-set and managed-heap figures,
// plus an optional census of the objects reachable from static fields, to find what still grows the
// process. Dropped before the PR.
public static class ProbeLog
{
    private static int _censusCount;
    private static Dictionary<string, int>? _firstCensus;

    public static void LogProcess(string label)
    {
        string directory = Path.Combine(
            Environment.GetEnvironmentVariable("GITHUB_WORKSPACE") ?? ".", "TestResults");
        Directory.CreateDirectory(directory);
        using Process process = Process.GetCurrentProcess();
        File.AppendAllText(
            Path.Combine(directory, "process.csv"),
            $"{DateTime.UtcNow:O},{process.Threads.Count},{process.HandleCount}," +
            $"{process.WorkingSet64 / (1024 * 1024)},{process.PrivateMemorySize64 / (1024 * 1024)}," +
            $"{GC.GetTotalMemory(false) / (1024 * 1024)},{label}\n");

        if (Environment.GetEnvironmentVariable("GUM_CENSUS") == "1")
        {
            Census(directory);
        }
    }

    // Breadth-first walk from every static field of the Gum and MonoGame assemblies to the target.
    public static string? PathTo(object target)
    {
        Dictionary<object, (object? Parent, string Edge)> parents = new(ReferenceEqualityComparer.Instance);
        Queue<(object Value, int Depth)> queue = new();

        void Visit(object? value, object? parent, string edge, int depth)
        {
            if (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsPointer || parents.ContainsKey(value))
            {
                return;
            }
            parents[value] = (parent, edge);
            queue.Enqueue((value, depth));
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string? name = assembly.GetName().Name;
            if (name == null || !(name.StartsWith("Gum") || name.StartsWith("MonoGame") || name.StartsWith("RenderingLibrary") || name.StartsWith("ToolsUtilities")))
            {
                continue;
            }
            Type[] types;
            try { types = assembly.GetTypes(); } catch { continue; }
            foreach (Type type in types)
            {
                if (type.ContainsGenericParameters) { continue; }
                foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsLiteral || field.FieldType.IsPointer) { continue; }
                    try { Visit(field.GetValue(null), null, $"static {type.FullName}.{field.Name}", 0); } catch { }
                }
            }
        }

        while (queue.Count > 0)
        {
            (object value, int depth) = queue.Dequeue();
            if (ReferenceEquals(value, target))
            {
                List<string> path = new();
                object? current = value;
                while (current != null)
                {
                    (object? parent, string edge) = parents[current];
                    path.Add($"{current.GetType().Name} <- {edge}");
                    current = parent;
                }
                path.Reverse();
                return string.Join(Environment.NewLine, path);
            }
            if (depth >= 16) { continue; }

            Type valueType = value.GetType();
            if (value is Array array)
            {
                if (!valueType.GetElementType()!.IsPrimitive)
                {
                    int index = 0;
                    foreach (object? element in array) { Visit(element, value, $"[{index++}]", depth + 1); }
                }
                continue;
            }
            for (Type? type = valueType; type != null; type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.FieldType.IsPrimitive || field.FieldType.IsPointer || field.FieldType == typeof(string)) { continue; }
                    try { Visit(field.GetValue(value), value, $"{type.Name}.{field.Name}", depth + 1); } catch { }
                }
            }
        }
        return null;
    }

    // Counts objects reachable from the Gum and MonoGame assemblies' static fields, by type, and writes
    // the types that grew since the first census.
    private static void Census(string directory)
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Dictionary<string, int> counts = new();
        Dictionary<object, (object? Parent, string Edge)> parents = new(ReferenceEqualityComparer.Instance);
        Queue<object> queue = new();
        object? currentParent = null;
        string currentEdge = "";

        void Visit(object? value)
        {
            if (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsPointer || parents.ContainsKey(value))
            {
                return;
            }
            parents[value] = (currentParent, currentEdge);
            queue.Enqueue(value);
        }

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string? name = assembly.GetName().Name;
            if (name == null || !(name.StartsWith("Gum") || name.StartsWith("MonoGame") || name.StartsWith("RenderingLibrary") || name.StartsWith("ToolsUtilities")))
            {
                continue;
            }
            Type[] types;
            try { types = assembly.GetTypes(); } catch { continue; }
            foreach (Type type in types)
            {
                if (type.ContainsGenericParameters) { continue; }
                foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsLiteral || field.FieldType.IsPointer) { continue; }
                    currentParent = null;
                    currentEdge = $"static {type.FullName}.{field.Name}";
                    try { Visit(field.GetValue(null)); } catch { }
                }
            }
        }

        List<object> projects = new();
        while (queue.Count > 0)
        {
            object value = queue.Dequeue();
            Type valueType = value.GetType();
            string typeName = valueType.FullName ?? valueType.Name;
            counts[typeName] = counts.GetValueOrDefault(typeName) + 1;
            if (typeName == "Gum.DataTypes.GumProjectSave" || typeName == "RenderingLibrary.Graphics.Fonts.BitmapFont")
            {
                projects.Add(value);
            }

            currentParent = value;
            if (value is Array array)
            {
                if (!valueType.GetElementType()!.IsPrimitive)
                {
                    int index = 0;
                    foreach (object? element in array) { currentEdge = $"[{index++}]"; Visit(element); }
                }
                continue;
            }
            for (Type? type = valueType; type != null; type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.FieldType.IsPrimitive || field.FieldType.IsPointer || field.FieldType == typeof(string)) { continue; }
                    currentEdge = $"{type.Name}.{field.Name}";
                    try { Visit(field.GetValue(value)); } catch { }
                }
            }
        }

        string paths = "";
        foreach (object project in projects.Skip(3).Take(4))
        {
            List<string> path = new();
            object? current = project;
            while (current != null)
            {
                (object? parent, string edge) = parents[current];
                path.Add($"{current.GetType().Name} <- {edge}");
                current = parent;
            }
            path.Reverse();
            paths += "\n--- path to a retained " + project.GetType().Name + ":\n" + string.Join("\n", path);
        }
        File.WriteAllText(Path.Combine(directory, "paths.txt"), $"retained project/font objects: {projects.Count}\n{paths}");

        _censusCount++;
        if (_firstCensus == null)
        {
            _firstCensus = counts;
            return;
        }

        IEnumerable<string> lines = counts
            .Select(pair => (Type: pair.Key, Delta: pair.Value - _firstCensus.GetValueOrDefault(pair.Key), Now: pair.Value))
            .Where(item => item.Delta > 0)
            .OrderByDescending(item => item.Delta)
            .Take(25)
            .Select(item => $"{item.Delta,7} (now {item.Now,7})  {item.Type}");
        File.WriteAllText(Path.Combine(directory, "census.txt"), $"census #{_censusCount}, total objects {counts.Values.Sum()} (first {_firstCensus.Values.Sum()})\n" + string.Join("\n", lines));
    }
}
