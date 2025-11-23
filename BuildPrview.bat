@echo off
echo 即将开始构建...

cd ./src/BedrockBoot

dotnet publish -c Debug -r win-x86 /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:IncludeNativeLibrariesForSelfExtract=true /p:SelfContained=true -o "./../../debug-publish/" -p:DebugType=none -p:DebugSymbols=false

echo 项目构建完毕。