@echo off

md "%APPDATA%\RoundStudio\BedrockBoot2\BedrockBoot.Plugin"
dotnet publish ./src/Round.SDK/Round.SDK.PluginTools/Round.SDK.PluginTools.csproj -c Release -o ./build/Round.SDK.PluginTools
powershell ./build/Round.SDK.PluginTools/Round.SDK.PluginTools.exe -b -config ./pluginBuildConfig/Plugin.ToolsBox.json
powershell copy "build/Plugin.ToolsBox/publish/pack.rplck" "%APPDATA%\RoundStudio\BedrockBoot2\BedrockBoot.Plugin\ToolsBox.rplck"