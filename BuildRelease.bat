@echo off

cd ./src/BedrockBoot

dotnet publish -c Release -r win-x64 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:SelfContained=false -o "./../../release/"