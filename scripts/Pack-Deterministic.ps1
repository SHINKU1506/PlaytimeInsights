[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$ToolboxPath
)

$ErrorActionPreference = 'Stop'

$source = (Resolve-Path -LiteralPath $SourceDirectory).Path
$toolbox = (Resolve-Path -LiteralPath $ToolboxPath).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
$temporaryRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$expectedFiles = @(
    'extension.yaml',
    'icon-dashboard.png',
    'icon-sessions.png',
    'icon.png',
    'LICENSE',
    'PlaytimeInsights.dll',
    'PRIVACY.md',
    'Localization\en_US.xaml',
    'Localization\zh_CN.xaml'
)

$actualFiles = @(Get-ChildItem -LiteralPath $source -Recurse -File |
    ForEach-Object {
        $_.FullName.Substring($source.Length).TrimStart('\')
    } |
    Sort-Object)
$expectedSorted = @($expectedFiles | Sort-Object)
if ([string]::Join('|', $actualFiles) -ne
    [string]::Join('|', $expectedSorted)) {
    throw 'Release directory does not contain exactly the nine expected files.'
}

New-Item -ItemType Directory -Force -Path $output | Out-Null
$packRoot = [IO.Path]::GetFullPath((Join-Path $temporaryRoot (
    'PlaytimeInsights-pack-' + [Guid]::NewGuid().ToString('N'))
))
if (-not $packRoot.StartsWith(
    $temporaryRoot,
    [StringComparison]::OrdinalIgnoreCase) -or
    [IO.Path]::GetFileName($packRoot) -notlike 'PlaytimeInsights-pack-*') {
    throw "Invalid temporary pack path: $packRoot"
}
$fixedTimestamp = [DateTime]::SpecifyKind(
    [DateTime]::ParseExact(
        '2000-01-01 00:00:00',
        'yyyy-MM-dd HH:mm:ss',
        [Globalization.CultureInfo]::InvariantCulture),
    [DateTimeKind]::Local)

try {
    foreach ($relativePath in $expectedFiles) {
        $sourcePath = Join-Path $source $relativePath
        $targetPath = Join-Path $packRoot $relativePath
        $targetDirectory = Split-Path -Parent $targetPath
        New-Item -ItemType Directory -Force -Path $targetDirectory |
            Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination $targetPath
        (Get-Item -LiteralPath $targetPath).LastWriteTime = $fixedTimestamp
    }

    & $toolbox pack $packRoot $output
    if ($LASTEXITCODE -ne 0) {
        throw "Toolbox pack failed with exit code $LASTEXITCODE."
    }

    $packages = @(Get-ChildItem -LiteralPath $output -File -Filter *.pext)
    if ($packages.Count -ne 1) {
        throw "Expected exactly one PEXT package, found $($packages.Count)."
    }

    $packages[0].FullName
}
finally {
    if (Test-Path -LiteralPath $packRoot) {
        Remove-Item -LiteralPath $packRoot -Recurse -Force
    }
}
