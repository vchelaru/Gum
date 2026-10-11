#!/bin/bash
# Builds the packaged macOS app the way the release workflow does (.github/workflows/build-and-release.yml,
# the osx-* legs of publish-avalonia), into publish/app/Gum.app, the default GUM_APP of macpass.py.
# Usage: Tools/MacPass/build-app.sh [osx-arm64|osx-x64]   (about 10 minutes, Native AOT for GumPreview)
set -euo pipefail
cd "$(dirname "$0")/../.."
rid="${1:-osx-arm64}"
version=$(date +%Y.%m.%d)
out="publish/$rid"

for plugin in ConvertToJsonPlugin EventOutputPlugin GumFormsPlugin ImportFromGumxPlugin PerformanceMeasurementPlugin; do
  dotnet build "Gum/$plugin/$plugin.csproj" -c Release
done
dotnet build Gum/SvgPlugin/SkiaPlugin.csproj -c Release

dotnet publish Tool/Gum.Avalonia/Gum.Avalonia.csproj -c Release -r "$rid" --self-contained true -p:Version="$version" -p:EmbedBmFont=false -o "$out"
dotnet publish Tool/GumPreview/GumPreview.csproj -c Release -r "$rid" --self-contained true -p:PublishAot=true -p:Version="$version" -p:EmbedBmFont=false -o "$out/Preview-Aot"
find "$out/Preview-Aot" \( -name '*.dSYM' -o -name '*.dbg' -o -name '*.pdb' \) -prune -exec rm -rf {} +
mkdir -p "$out/Plugins"
cp -R Tool/Gum.Avalonia/bin/Release/net10.0/Plugins/. "$out/Plugins/"
if [ -d Tool/Gum.Avalonia/bin/Release/net10.0/Content ]; then
  mkdir -p "$out/Content"
  cp -R Tool/Gum.Avalonia/bin/Release/net10.0/Content/. "$out/Content/"
fi
dotnet publish Tools/Gum.Cli/Gum.Cli.csproj -c Release -r "$rid" --self-contained false -p:Version="$version" -p:EmbedBmFont=false -o "$out/GumCli"
find "$out" -name '*.xml' -not -path '*/Content/*' -delete

app="publish/app/Gum.app"
rm -rf "$app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp -R "$out/." "$app/Contents/MacOS/"
sed "s/__VERSION__/$version/g" Tool/Gum.Avalonia/Packaging/macOS/Info.plist > "$app/Contents/Info.plist"
cp Tool/Gum.Avalonia/Packaging/macOS/Gum.icns "$app/Contents/Resources/"
chmod +x "$app/Contents/MacOS/Gum"
codesign --force --deep --sign - "$app"
echo "Built $PWD/$app"
