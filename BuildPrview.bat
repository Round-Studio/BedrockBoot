@echo off

cd ./src/BedrockBoot

dotnet publish -c Debug -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:SelfContained=true -o "./../../debug-publish/" -p:DebugType=none -p:DebugSymbols=false