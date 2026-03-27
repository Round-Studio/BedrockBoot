# build.ps1
param(
    [string]$Version = $(Get-Date -Format 'yyyy.MM.dd.HHmm'),
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = "./Release-publish/",
    [string]$ProjectPath = "./src/BedrockBoot"
)

# 设置错误处理
$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "开始构建 BedrockBoot" -ForegroundColor Cyan
Write-Host "版本: $Version" -ForegroundColor Yellow
Write-Host "配置: $Configuration" -ForegroundColor Yellow
Write-Host "运行时: $Runtime" -ForegroundColor Yellow
Write-Host "输出路径: $OutputPath" -ForegroundColor Yellow
Write-Host "========================================" -ForegroundColor Cyan

# 创建输出目录（如果不存在）
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "创建输出目录: $OutputPath" -ForegroundColor Green
}

# 构建参数
$publishParams = @{
    Project = $ProjectPath
    Configuration = $Configuration
    Runtime = $Runtime
    Output = $OutputPath
    NoRestore = $false
    NoBuild = $false
}

Write-Host "正在执行 dotnet publish..." -ForegroundColor Green
Write-Host "命令: dotnet publish $ProjectPath -c $Configuration -r $Runtime -o $OutputPath /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:SelfContained=false /p:Version=$Version" -ForegroundColor Gray

try {
    # 执行 dotnet publish
    dotnet publish $ProjectPath `
        -c $Configuration `
        -r $Runtime `
        -o $OutputPath `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:SelfContained=false `
        /p:Version=$Version

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish 失败，退出代码: $LASTEXITCODE"
    }

    Write-Host "构建成功完成!" -ForegroundColor Green

    # 列出生成的文件
    $builtFiles = Get-ChildItem -Path $OutputPath -Filter "*.exe"
    if ($builtFiles.Count -gt 0) {
        Write-Host "生成的可执行文件:" -ForegroundColor Green
        foreach ($file in $builtFiles) {
            $fileSize = [math]::Round($file.Length / 1MB, 2)
            Write-Host "  - $($file.Name) ($fileSize MB)" -ForegroundColor White
        }
    }
} catch {
    Write-Host "构建失败: $($_.Exception.Message)" -ForegroundColor Red
    throw
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "构建完成" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 返回版本信息（用于后续步骤）
return @{
    Version = $Version
    OutputPath = $OutputPath
    BuiltFiles = $builtFiles
}