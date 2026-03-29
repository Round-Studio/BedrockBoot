# 强制指定输出编码为 UTF8，防止终端乱码
$OutputEncoding = [System.Text.Encoding]::UTF8

param(
    [string]$Version = $(Get-Date -Format 'yyyy.MM.dd.HHmm'),
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputPath = "./Release-publish/",
    [string]$ProjectPath = "./src/BedrockBoot"
)

$ErrorActionPreference = "Stop"

# 使用简单的英文标签，避免在解析变量块时发生编码错误
Write-Host "`n"
Write-Host " ======================================================== " -ForegroundColor Cyan
Write-Host "             BedrockBoot Build Tools (Native AOT)         " -ForegroundColor Cyan
Write-Host " ======================================================== " -ForegroundColor Cyan

Write-Host " [Version]   : $Version" -ForegroundColor Yellow
Write-Host " [Config]    : $Configuration" -ForegroundColor Yellow
Write-Host " [Runtime]   : $Runtime" -ForegroundColor Yellow
Write-Host " [Output]    : $OutputPath" -ForegroundColor Yellow
Write-Host " -------------------------------------------------------- " -ForegroundColor Gray

# 1. 环境清理与输出目录检查
if (Test-Path "$ProjectPath/bin") { Remove-Item -Recurse -Force "$ProjectPath/bin" }
if (Test-Path "$ProjectPath/obj") { Remove-Item -Recurse -Force "$ProjectPath/obj" }

if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host " [+] Created output directory." -ForegroundColor Gray
}

Write-Host " [*] Running dotnet publish, please wait..." -ForegroundColor Green

# 2. 执行构建
try {
    dotnet publish $ProjectPath `
        -c $Configuration `
        -r $Runtime `
        -o $OutputPath `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:SelfContained=false `
        /p:Version=$Version

    if ($LASTEXITCODE -ne 0) {
        throw "Dotnet publish failed with exit code: $LASTEXITCODE"
    }

    Write-Host "`n [√] Build Success!`n" -ForegroundColor Green

    # 3. 产物列表
    $builtFiles = Get-ChildItem -Path $OutputPath -File | Where-Object { $_.Extension -eq ".exe" -or $_.Extension -eq ".dll" }
    
    if ($builtFiles.Count -gt 0) {
        Write-Host " Result Summary:" -ForegroundColor Cyan
        Write-Host " -------------------------------------------------------" -ForegroundColor DarkGray
        Write-Host "   FileName                            | Size           " -ForegroundColor DarkGray
        Write-Host " -------------------------------------------------------" -ForegroundColor DarkGray
        
        foreach ($file in $builtFiles) {
            $name = $file.Name.PadRight(35)
            $size = ([math]::Round($file.Length / 1MB, 2)).ToString().PadLeft(8)
            Write-Host "   $name | $size MB" -ForegroundColor White
        }
        Write-Host " -------------------------------------------------------" -ForegroundColor DarkGray
    }

} catch {
    Write-Host "`n [!] FATAL ERROR:" -ForegroundColor Red
    Write-Host "     $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host " ======================================================== " -ForegroundColor Cyan
Write-Host "                  Build Task Finished                     " -ForegroundColor Cyan
Write-Host " ======================================================== `n" -ForegroundColor Cyan

# 返回结果对象
$result = @{
    Version = $Version
    OutputPath = $OutputPath
    BuiltFiles = $builtFiles
}
return $result