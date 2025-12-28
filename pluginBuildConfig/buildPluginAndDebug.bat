@echo off

md "%APPDATA%\RoundStudio\BedrockBoot2\BedrockBoot.Plugin"
dotnet publish ./src/Round.SDK/PluginTools/PluginTools.csproj -c Release -o ./build/PluginTools
powershell ./build/PluginTools/PluginTools.exe -b -config ./pluginBuildConfig/Plugin.ToolsBox.json
powershell copy "build/Plugin.ToolsBox/publish/pack.rplck" "%APPDATA%\RoundStudio\BedrockBoot2\BedrockBoot.Plugin\ToolsBox.rplck"