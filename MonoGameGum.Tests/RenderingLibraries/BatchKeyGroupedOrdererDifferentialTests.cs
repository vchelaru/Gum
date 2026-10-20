using System;
using System.Collections.Generic;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using static MonoGameGum.Tests.RenderingLibraries.BatchOrdererScenes;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Compares <see cref="BatchKeyGroupedOrderer"/> against <see cref="LegacyPairwiseBatchOrderer"/>
/// (the all-pairs reference) over many random scenes, so any change to how the precedence graph is
/// built must leave the draw list identical (#6179).
/// </summary>
public class BatchKeyGroupedOrdererDifferentialTests : BaseTestClass
{
    private enum Layout { Dense, Sparse, Rows, Columns, SameRowY }

    private static Layer RandomScene(int seed)
    {
        Random random = new Random(seed);
        Layout layout = (Layout)(seed % 5);
        // Key mode varies how many nodes share a (BatchKey, BatchSortKey): the original mix, one
        // batch key with many sort keys (some equal by value but different instances), or two keys.
        int keyMode = (seed / 5) % 3;
        int nodeCount = random.Next(1, seed % 50 == 0 ? 400 : 60);
        object[] sortKeys = keyMode == 0
            ? new object[] { new object(), new object() }
            : new object[] { new object(), new object(), 1, 1, new string('a', 2), new string('a', 2), 2.5f };
        string[] batchKeys = keyMode switch
        {
            0 => new[] { Sprites, Shapes, "" },
            1 => new[] { Sprites },
            _ => new[] { Sprites, Shapes },
        };

        List<OrdererFake> all = new List<OrdererFake>();
        List<IRenderableIpso> roots = new List<IRenderableIpso>();
        for (int i = 0; i < nodeCount; i++)
        {
            float x, y, width, height;
            switch (layout)
            {
                case Layout.Dense:
                    x = random.Next(-10, 40); y = random.Next(-10, 40);
                    width = random.Next(0, 30); height = random.Next(0, 30);
                    break;
                case Layout.Sparse:
                    x = random.Next(0, 2000); y = random.Next(0, 2000);
                    width = random.Next(0, 60); height = random.Next(0, 60);
                    break;
                case Layout.Rows:
                    x = random.Next(0, 3) * 5; y = i * 12 + random.Next(0, 6);
                    width = random.Next(10, 300); height = random.Next(0, 20);
                    break;
                case Layout.Columns:
                    x = i * 12 + random.Next(0, 6); y = random.Next(0, 3) * 5;
                    width = random.Next(0, 20); height = random.Next(10, 300);
                    break;
                default:
                    // A few shared Y values with spread-out X: same-Y runs under SecondarySortOnY.
                    x = random.Next(0, 400); y = random.Next(0, 4) * 40;
                    width = random.Next(5, 60); height = random.Next(5, 30);
                    break;
            }

            string batchKey = batchKeys[random.Next(batchKeys.Length)];
            OrdererFake node = new OrdererFake($"n{i}", batchKey)
            {
                X = x, Y = y, Width = width, Height = height,
                BatchSortKey = random.Next(4) == 0 ? null : sortKeys[random.Next(sortKeys.Length)],
                ClipsChildren = random.Next(20) == 0,
                IsRenderTarget = random.Next(30) == 0,
                Visible = random.Next(30) != 0,
            };

            if (all.Count > 0 && random.Next(2) == 0)
            {
                OrdererFake parent = all[random.Next(all.Count)];
                node.SetParentDirect(parent);
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
            all.Add(node);
        }

        return BuildLayer(roots, secondarySortOnY: layout == Layout.SameRowY ? random.Next(2) == 0 : random.Next(7) == 0);
    }

    [Fact]
    public void BuildDrawList_RandomScenes_MatchesAllPairsReference()
    {
        // One orderer reused across scenes, as in production, so pooled scratch state is exercised.
        BatchKeyGroupedOrderer orderer = new BatchKeyGroupedOrderer();
        LegacyPairwiseBatchOrderer reference = new LegacyPairwiseBatchOrderer();
        List<DrawCommand> actual = new List<DrawCommand>();

        for (int seed = 0; seed < 3000; seed++)
        {
            Layer layer = RandomScene(seed);

            orderer.BuildDrawList(layer, actual);
            List<DrawCommand> expected = reference.Build(layer);

            string context = $"seed {seed}";
            string.Join(",", Describe(actual)).ShouldBe(string.Join(",", Describe(expected)), context);
        }
    }
}
