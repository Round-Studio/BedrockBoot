@echo off
echo ������ʼ����...

cd ./src/BedrockBoot

dotnet publish -c Debug -r win-x86 /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:SelfContained=true -o "./../../debug-publish/" -p:DebugType=none -p:DebugSymbols=false

echo ��Ŀ������ϡ�