@echo off

echo Starting to generate...
%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_Maui_FullCodegen/Content/GumProject/CodeGenTestProject.gumx --generatecode
echo 1/4 generated CodeGen_Maui_FullCodegen 

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGame_ByReference/Content/GumProject/CodeGenTestProject.gumx --generatecode
echo 2/4 generated CodeGen_MonoGame_ByReference 

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGameForms_ByReference/Content/GumProject/CodeGenTestProject.gumx --generatecode
echo 3/4 generated CodeGen_MonoGameForms_ByReference

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGameForms_FullCodegen/Content/CodeGenProject.gumx --generatecode
echo 4/4 generated CodeGen_MonoGameForms_FullCodegen
