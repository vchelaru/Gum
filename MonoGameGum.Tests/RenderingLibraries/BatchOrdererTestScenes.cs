using System.Collections.Generic;
using System.Collections.ObjectModel;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>Minimal renderable with settable absolute-style bounds, batch key and sort key.</summary>
internal sealed class OrdererFake : IRenderableIpso
{
    public OrdererFake(string name, string batchKey = "SpriteBatch")
    {
        Name = name;
        BatchKey = batchKey;
        Visible = true;
        Children = new ObservableCollection<IRenderableIpso>();
    }

    public string? Name { get; set; }
    public bool Visible { get; set; }
    public bool ClipsChildren { get; set; }
    public bool IsRenderTarget { get; set; }
    public ObservableCollection<IRenderableIpso> Children { get; }

    public IRenderableIpso? Parent { get; set; }
    IVisible? IVisible.Parent => Parent;
    public bool AbsoluteVisible => Visible;

    public void SetParentDirect(IRenderableIpso? newParent) => Parent = newParent;

    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Rotation { get; set; }
    public bool FlipHorizontal { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public object? Tag { get; set; }

    public int Alpha => 255;
    public ColorOperation ColorOperation => ColorOperation.Modulate;
    public Gum.BlendState BlendState => Gum.BlendState.NonPremultiplied;
    public bool Wrap => false;

    public string BatchKey { get; set; }
    public object? BatchSortKey { get; set; }
    public void Render(ISystemManagers managers) { }
    public void PreRender() { }
    public void StartBatch(ISystemManagers managers) { }
    public void EndBatch(ISystemManagers managers) { }
}

/// <summary>Scene builders and output formatting shared by the BatchKeyGroupedOrderer tests.</summary>
internal static class BatchOrdererScenes
{
    public const string Sprites = "SpriteBatch";
    public const string Shapes = "Apos.Shapes";

    public static OrdererFake AddChild(
        OrdererFake parent, string name, string batchKey, float x, float y, float width, float height, object? sortKey = null)
    {
        OrdererFake child = new OrdererFake(name, batchKey)
        {
            X = x, Y = y, Width = width, Height = height, BatchSortKey = sortKey,
        };
        child.SetParentDirect(parent);
        parent.Children.Add(child);
        return child;
    }

    public static Layer BuildLayer(IEnumerable<IRenderableIpso> renderables, bool secondarySortOnY = false)
    {
        Layer layer = new Layer();
        layer.SecondarySortOnY = secondarySortOnY;
        foreach (IRenderableIpso renderable in renderables)
        {
            layer.Add(renderable);
        }
        return layer;
    }

    public static List<string> Describe(List<DrawCommand> commands)
    {
        List<string> result = new List<string>();
        foreach (DrawCommand command in commands)
        {
            result.Add($"{command.Kind}:{((OrdererFake)command.Target).Name}");
        }
        return result;
    }

    /// <summary>
    /// A list of rows, each a free container (empty batch key) holding a rounded-rect background,
    /// an icon sprite, a label, and a badge shape: 1 + 4 nodes per row. Rows stack vertically and
    /// share one horizontal extent, the common shape of a list or menu.
    /// </summary>
    public static List<IRenderableIpso> BuildVerticalList(int rowCount, object iconTexture, object fontTexture)
    {
        List<IRenderableIpso> roots = new List<IRenderableIpso>();
        for (int i = 0; i < rowCount; i++)
        {
            float y = i * 30;
            OrdererFake row = new OrdererFake($"row{i}", "") { X = 0, Y = y, Width = 300, Height = 28 };
            AddChild(row, $"bg{i}", Shapes, 0, y, 300, 28);
            AddChild(row, $"icon{i}", Sprites, 4, y + 4, 20, 20, iconTexture);
            AddChild(row, $"label{i}", Sprites, 30, y + 6, 180, 16, fontTexture);
            AddChild(row, $"badge{i}", Shapes, 250, y + 6, 40, 16);
            roots.Add(row);
        }
        return roots;
    }

    /// <summary>The same row content as <see cref="BuildVerticalList"/>, laid out left to right.</summary>
    public static List<IRenderableIpso> BuildHorizontalList(int columnCount, object iconTexture, object fontTexture)
    {
        List<IRenderableIpso> roots = new List<IRenderableIpso>();
        for (int i = 0; i < columnCount; i++)
        {
            float x = i * 60;
            OrdererFake column = new OrdererFake($"col{i}", "") { X = x, Y = 0, Width = 58, Height = 200 };
            AddChild(column, $"bg{i}", Shapes, x, 0, 58, 200);
            AddChild(column, $"icon{i}", Sprites, x + 4, 4, 20, 20, iconTexture);
            AddChild(column, $"label{i}", Sprites, x + 4, 30, 50, 16, fontTexture);
            AddChild(column, $"badge{i}", Shapes, x + 4, 150, 40, 16);
            roots.Add(column);
        }
        return roots;
    }

    /// <summary>Counts every renderable in the given roots and their descendants.</summary>
    public static int CountNodes(IEnumerable<IRenderableIpso> roots)
    {
        int count = 0;
        foreach (IRenderableIpso root in roots)
        {
            count += 1 + CountNodes(root.Children);
        }
        return count;
    }
}
