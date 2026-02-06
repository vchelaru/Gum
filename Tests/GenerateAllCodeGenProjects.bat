@echo off

echo 0/100 Starting to generate...
%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_Maui_FullCodegen/Content/GumProject/CodeGenTestProject.gumx --generatecode
echo 20/100 generated CodeGen_Maui_FullCodegen 

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGame_ByReference/Content/GumProject/CodeGenTestProject.gumx --generatecode
echo 40/100 generated CodeGen_MonoGame_ByReference 

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGameForms_ByReference/Content/GumProject/CodeGenTestProject.gumx --generatecode
echo 60/100 generated CodeGen_MonoGameForms_ByReference

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGameForms_Localization_ByReference/Content/GumProject/LocalizationCodeGenTestProject.gumx --generatecode
echo 80/100 generated CodeGen_MonoGameForms_Localization_ByReference

%~dp0../Gum/bin/Debug/Gum.exe %~dp0CodeGen_MonoGameForms_FullCodegen/Content/CodeGenProject.gumx --generatecode
echo 100/100 generated CodeGen_MonoGameForms_FullCodegen
