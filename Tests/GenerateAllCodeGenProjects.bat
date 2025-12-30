@echo off

echo 0% Starting to generate...
%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_Maui_FullCodegen/Content/GumProject/CodeGenTestProject.gumx --generatecode
echo 25% generated CodeGen_Maui_FullCodegen 

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGame_ByReference/Content/GumProject/CodeGenTestProject.gumx --generatecode
echo 50% generated CodeGen_MonoGame_ByReference 

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGameForms_ByReference/Content/GumProject/CodeGenTestProject.gumx --generatecode
echo 75% generated CodeGen_MonoGameForms_ByReference

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGameForms_FullCodegen/Content/CodeGenProject.gumx --generatecode
echo 100% generated CodeGen_MonoGameForms_FullCodegen
