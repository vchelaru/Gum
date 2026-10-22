# Gum #6388 repro: text drawing with TieredCompilation off

This runs the same Gum text drawing as your game with .NET's tiered compilation on and off, and with the current and a proposed version of one small rounding function (`MathFunctions.RoundToInt`), to see which setting and which code make drawing slow on your CPU.
It also checks whether a call into the .NET framework is slower right after other code has used the CPU's wide vector registers, which is the suspected cause.
Nothing is installed or changed; it only needs the .NET 10 SDK, and it opens no visible window.

## Run it

From the root of the Gum repository, with nothing else heavy running:

```
dotnet run -c Release --project Tests/Gum.TieringRepro
```

It builds first (a minute or two the first time), then runs for about 3 to 4 minutes.

## What to paste back

When it finishes it prints a block between two `-----8<-----` lines and saves the same text to `tiering-repro-result.md` in the folder you ran it from. Paste that whole block into the issue.
