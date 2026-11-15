// --benchmark [out.md]   RELEASE ONLY (dotnet build -c Release); Debug refuses. The row is appended to out.md (default benchmark-result.md in the launch directory; this is a WinExe, so the console shows nothing).
//                        600 warmup + 300 measured frames, then exits (#8052). Add --hidden for no visible window.
using var game = new KniSkiaGumSample.Game1(args);
game.Run();
