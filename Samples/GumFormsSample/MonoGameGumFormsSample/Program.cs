// --benchmark [out.md]   RELEASE ONLY (dotnet build -c Release); Debug refuses. 600 warmup + 300 measured frames,
//                        prints a markdown row of CPU ms per frame, then exits (#8052). Add --hidden for no visible window.
if (args.Contains("--benchmark"))
{
    using var benchmark = new MonoGameGumFormsSample.BenchmarkGame(args);
    benchmark.Run();
    return;
}

using var game = new GumFormsSample.GumFormsSampleGame();
game.Run();
