@echo off
rem Regenerates the checked-in generated code for every CodeGen_* harness project.
rem Uses GumCli — the same path CI's "Codegen Drift Check" step uses — so local
rem regeneration and CI verification can never diverge. Commit the resulting
rem diff; CI fails if checked-in generated code is stale.

dotnet build "%~dp0..\Tools\Gum.Cli\Gum.Cli.csproj" --configuration Release || exit /b 1
set CLI=%~dp0..\Tools\Gum.Cli\bin\Release\net8.0\GumCli.exe

echo 0/100 Starting to generate...
"%CLI%" codegen %~dp0CodeGen_Maui_FullCodegen/Content/GumProject/CodeGenTestProject.gumx
echo 15/100 generated CodeGen_Maui_FullCodegen

"%CLI%" codegen %~dp0CodeGen_MonoGame_ByReference/Content/GumProject/CodeGenTestProject.gumx
echo 30/100 generated CodeGen_MonoGame_ByReference

"%CLI%" codegen %~dp0CodeGen_MonoGameForms_ByReference/Content/GumProject/CodeGenTestProject.gumx
echo 45/100 generated CodeGen_MonoGameForms_ByReference

"%CLI%" codegen %~dp0CodeGen_MonoGameForms_Localization_ByReference/Content/GumProject/LocalizationCodeGenTestProject.gumx
echo 60/100 generated CodeGen_MonoGameForms_Localization_ByReference

"%CLI%" codegen %~dp0CodeGen_MonoGameForms_FullCodegen/Content/CodeGenProject.gumx
echo 70/100 generated CodeGen_MonoGameForms_FullCodegen

"%CLI%" codegen %~dp0CodeGen_Raylib_ByReference/Content/GumProject/CodeGenTestProject.gumx
echo 85/100 generated CodeGen_Raylib_ByReference

"%CLI%" codegen %~dp0CodeGen_Skia_ByReference/Content/GumProject/CodeGenTestProject.gumx
echo 100/100 generated CodeGen_Skia_ByReference
