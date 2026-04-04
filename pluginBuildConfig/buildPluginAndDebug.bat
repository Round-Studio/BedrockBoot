@echo off

md "%APPDATA%\RoundStudio\BedrockBoot2\BedrockBoot.Plugin"
:: dotnet publish ./src/Round.SDK/Round.SDK.PluginTools/Round.SDK.PluginTools.csproj -c Release -o ./build/Round.SDK.PluginTools
powershell ./build/Round.SDK.PluginTools/Round.SDK.PluginTools.exe -b -config ./pluginBuildConfig/Plugin.Pro.json
powershell copy "build/Plugin.Pro/publish/pack.rplck" "%APPDATA%\RoundStudio\BedrockBoot2\BedrockBoot.Plugin\Pro.rplck"