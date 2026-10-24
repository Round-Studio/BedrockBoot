$ErrorActionPreference = "Stop"

$RootPath    = (Get-Location).Path
$HeaderFile  = Join-Path $RootPath "docs/license-header.txt"
$ExcludeDirs = @("obj", "bin", ".git", ".vs", "packages", "node_modules")

if (-not (Test-Path -LiteralPath $HeaderFile)) {
    Write-Error "许可证头文件不存在: $HeaderFile"
    exit 2
}

$licenseText = (Get-Content -LiteralPath $HeaderFile -Raw -Encoding UTF8).TrimEnd()
$licenseText = $licenseText -replace "`r`n", "`n"

$submodulePaths = @()

try {
    $gitOutput = & git -C $RootPath submodule status --recursive 2>$null
    if ($LASTEXITCODE -eq 0 -and $gitOutput) {
        foreach ($line in $gitOutput) {
            # 每行格式: [ +|-]<sha1> <path> (<describe>)
            # 取 sha1 和 path 之间的部分
            if ($line -match '^\s*[+\-U]?[0-9a-fA-F]{40}\s+(\S+)') {
                $rel = $matches[1] -replace '/', [System.IO.Path]::DirectorySeparatorChar
                $abs = Join-Path $RootPath $rel
                $submodulePaths += $abs
            }
        }
    }
}
catch {
    Write-Warning "无法执行 git submodule status，将不跳过 submodule: $_"
}

if ($submodulePaths.Count -gt 0) {
    Write-Host "识别到 $($submodulePaths.Count) 个 submodule，将跳过：" -ForegroundColor Cyan
    foreach ($p in $submodulePaths) {
        Write-Host "  - $p" -ForegroundColor DarkGray
    }
}
else {
    Write-Host "未识别到 submodule。"
}
Write-Host ""

$submodulePrefixes = $submodulePaths | ForEach-Object {
    if ($_.EndsWith([System.IO.Path]::DirectorySeparatorChar)) { $_ }
    else { $_ + [System.IO.Path]::DirectorySeparatorChar }
}

$headerPattern = '(?s)^\s*/\*.*?\*/\s*'

$allFiles = Get-ChildItem -LiteralPath $RootPath -Filter *.cs -Recurse -File -ErrorAction SilentlyContinue

$files = $allFiles | Where-Object {
    $filePath = $_.FullName

    foreach ($prefix in $submodulePrefixes) {
        if ($filePath.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
    }

    # 跳过排除目录
    foreach ($dir in $ExcludeDirs) {
        if ($filePath -match "[\\/]$([regex]::Escape($dir))[\\/]") {
            return $false
        }
    }

    return $true
}

$total = ($files | Measure-Object).Count
Write-Host "找到 $total 个 .cs 文件"
Write-Host ""

$addedCount    = 0
$replacedCount = 0
$skippedCount  = 0

foreach ($file in $files) {
    try {
        $content = Get-Content -LiteralPath $file.FullName -Raw -Encoding UTF8

        if ([string]::IsNullOrEmpty($content)) {
            [System.IO.File]::WriteAllText(
                $file.FullName,
                $licenseText + "`n`n",
                (New-Object System.Text.UTF8Encoding($false))
            )
            Write-Host "[添加] $($file.FullName)"
            $addedCount++
            continue
        }

        $normalizedContent = $content -replace "`r`n", "`n"

        if ($normalizedContent -match $headerPattern) {
            $existingHeader = $matches[0].Trim()
            if ($existingHeader -eq $licenseText.Trim()) {
                $skippedCount++
                continue
            }

            $newContent = $normalizedContent -replace $headerPattern, ($licenseText + "`n`n")
            [System.IO.File]::WriteAllText(
                $file.FullName,
                $newContent,
                (New-Object System.Text.UTF8Encoding($false))
            )
            Write-Host "[替换] $($file.FullName)"
            $replacedCount++
        }
        else {
            $newContent = $licenseText + "`n`n" + $normalizedContent
            [System.IO.File]::WriteAllText(
                $file.FullName,
                $newContent,
                (New-Object System.Text.UTF8Encoding($false))
            )
            Write-Host "[添加] $($file.FullName)"
            $addedCount++
        }
    }
    catch {
        Write-Error "处理失败: $($file.FullName) -> $_"
        exit 2
    }
}

Write-Host ""
Write-Host "===== 完成 ====="
Write-Host "添加: $addedCount"
Write-Host "替换: $replacedCount"
Write-Host "跳过: $skippedCount"
Write-Host "总计: $total"

$changed = $addedCount + $replacedCount

if ($changed -gt 0) {
    Write-Host ""
    Write-Host "共修改 $changed 个文件。" -ForegroundColor Yellow
    exit 1
}

exit 0