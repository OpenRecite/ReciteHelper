[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$RuntimeIdentifier = "win-x64",

    [switch]$SelfContained,

    [switch]$SkipArchive,

    [string]$ArtifactsDirectory
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
Set-StrictMode -Version Latest

$projectRoot = $PSScriptRoot
$projectFile = Join-Path $projectRoot "ReciteHelper.Wpf\ReciteHelper.Wpf.csproj"

if ([string]::IsNullOrWhiteSpace($ArtifactsDirectory)) {
    $ArtifactsDirectory = Join-Path $projectRoot "artifacts"
}

$artifactsPath = [System.IO.Path]::GetFullPath($ArtifactsDirectory)
$projectRootPath = [System.IO.Path]::GetFullPath($projectRoot)

if ($artifactsPath.TrimEnd('\') -eq $projectRootPath.TrimEnd('\')) {
    throw "ArtifactsDirectory must not be the repository root."
}

$projectRootPrefix = $projectRootPath.TrimEnd('\') + '\'
if (-not $artifactsPath.StartsWith($projectRootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "ArtifactsDirectory must be located inside the repository."
}

if (-not (Test-Path -LiteralPath $projectFile -PathType Leaf)) {
    throw "WPF project not found: $projectFile"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK was not found. Install the .NET 10 SDK and try again."
}

$publishDirectory = Join-Path $artifactsPath "publish"
$reportsDirectory = Join-Path $artifactsPath "reports"
$logFile = Join-Path $reportsDirectory "build.log"
$reportFile = Join-Path $reportsDirectory "build-report.md"
$manifestFile = Join-Path $reportsDirectory "publish-manifest.csv"
$checksumFile = Join-Path $reportsDirectory "SHA256SUMS.txt"
$archiveName = "ReciteHelper.Wpf-$RuntimeIdentifier-$Configuration.zip"
$archiveFile = Join-Path $artifactsPath $archiveName
$startedAt = Get-Date
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$buildSucceeded = $false
$failureMessage = $null
$removedPdbCount = 0
$publishedFiles = @()
$archiveHash = $null

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $displayCommand = "dotnet " + ($Arguments -join " ")
    Add-Content -LiteralPath $script:logFile -Value "`r`n> $displayCommand"
    Write-Host "`n> $displayCommand" -ForegroundColor Cyan

    $output = & dotnet @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | Tee-Object -FilePath $script:logFile -Append | Out-Host

    if ($exitCode -ne 0) {
        throw "Command failed with exit code ${exitCode}: $displayCommand"
    }
}

function Get-RelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$BasePath,

        [Parameter(Mandatory = $true)]
        [string]$TargetPath
    )

    $baseUri = [System.Uri]::new($BasePath.TrimEnd('\') + '\')
    $targetUri = [System.Uri]::new($TargetPath)
    return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString())
}

try {
    if (Test-Path -LiteralPath $artifactsPath) {
        Remove-Item -LiteralPath $artifactsPath -Recurse -Force
    }

    New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $reportsDirectory -Force | Out-Null

    @(
        "ReciteHelper.Wpf build log"
        "Started: $($startedAt.ToString('yyyy-MM-dd HH:mm:ss zzz'))"
        "Configuration: $Configuration"
        "Runtime: $RuntimeIdentifier"
        "Self-contained: $($SelfContained.IsPresent)"
    ) | Set-Content -LiteralPath $logFile -Encoding UTF8

    $selfContainedValue = if ($SelfContained.IsPresent) { "true" } else { "false" }

    Invoke-DotNet -Arguments @(
        "restore", $projectFile,
        "--runtime", $RuntimeIdentifier
    )

    Invoke-DotNet -Arguments @(
        "clean", $projectFile,
        "--configuration", $Configuration,
        "--runtime", $RuntimeIdentifier
    )

    Invoke-DotNet -Arguments @(
        "publish", $projectFile,
        "--configuration", $Configuration,
        "--runtime", $RuntimeIdentifier,
        "--self-contained", $selfContainedValue,
        "--no-restore",
        "--output", $publishDirectory,
        "-p:DebugType=None",
        "-p:DebugSymbols=false",
        "-p:ContinuousIntegrationBuild=true"
    )

    $mainExecutable = Join-Path $publishDirectory "ReciteHelper.Wpf.exe"
    if (-not (Test-Path -LiteralPath $mainExecutable -PathType Leaf)) {
        throw "Publish completed, but the main executable is missing: $mainExecutable"
    }

    $pdbFiles = @(Get-ChildItem -LiteralPath $publishDirectory -Filter "*.pdb" -File -Recurse)
    $removedPdbCount = $pdbFiles.Count
    if ($removedPdbCount -gt 0) {
        $pdbFiles | Remove-Item -Force
    }

    $remainingPdbFiles = @(Get-ChildItem -LiteralPath $publishDirectory -Filter "*.pdb" -File -Recurse)
    if ($remainingPdbFiles.Count -ne 0) {
        throw "One or more PDB files could not be removed from the publish directory."
    }

    $publishedFiles = @(Get-ChildItem -LiteralPath $publishDirectory -File -Recurse | Sort-Object FullName)
    $manifestRows = foreach ($file in $publishedFiles) {
        [PSCustomObject]@{
            Path = Get-RelativePath -BasePath $publishDirectory -TargetPath $file.FullName
            SizeBytes = $file.Length
            SHA256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        }
    }
    $manifestRows | Export-Csv -LiteralPath $manifestFile -NoTypeInformation -Encoding UTF8

    if (-not $SkipArchive.IsPresent) {
        Compress-Archive -Path (Join-Path $publishDirectory "*") -DestinationPath $archiveFile -CompressionLevel Optimal -Force
        $archiveHash = (Get-FileHash -LiteralPath $archiveFile -Algorithm SHA256).Hash.ToLowerInvariant()
    }

    $checksumLines = foreach ($row in $manifestRows) {
        "$($row.SHA256)  publish/$($row.Path)"
    }
    if ($null -ne $archiveHash) {
        $checksumLines += "$archiveHash  $archiveName"
    }
    $checksumLines | Set-Content -LiteralPath $checksumFile -Encoding UTF8

    $buildSucceeded = $true
}
catch {
    $failureMessage = $_.Exception.Message
    if (Test-Path -LiteralPath $reportsDirectory) {
        Add-Content -LiteralPath $logFile -Value "`r`nERROR: $failureMessage"
    }
}
finally {
    $stopwatch.Stop()
    $finishedAt = Get-Date

    if (Test-Path -LiteralPath $reportsDirectory) {
        $sdkVersion = (& dotnet --version 2>$null | Select-Object -First 1)
        $gitCommit = "unavailable"
        if (Get-Command git -ErrorAction SilentlyContinue) {
            $commitOutput = & git -C $projectRoot rev-parse --short HEAD 2>$null
            if ($LASTEXITCODE -eq 0) {
                $gitCommit = $commitOutput
            }
        }

        $statusText = if ($buildSucceeded) { "SUCCESS" } else { "FAILED" }
        $archiveText = if ($SkipArchive.IsPresent) { "Skipped" } elseif ($null -ne $archiveHash) { $archiveName } else { "Not generated" }
        $failureSection = if ($null -ne $failureMessage) { "`r`n## Error`r`n`r`n``$failureMessage```r`n" } else { "" }
        $warningCount = @(
            Select-String -LiteralPath $logFile -Pattern ": warning " -SimpleMatch -ErrorAction SilentlyContinue
        ).Count

        @"
# ReciteHelper.Wpf Build Report

| Item | Value |
| --- | --- |
| Status | $statusText |
| Started | $($startedAt.ToString('yyyy-MM-dd HH:mm:ss zzz')) |
| Finished | $($finishedAt.ToString('yyyy-MM-dd HH:mm:ss zzz')) |
| Duration | $($stopwatch.Elapsed.ToString()) |
| Configuration | $Configuration |
| Runtime identifier | $RuntimeIdentifier |
| Self-contained | $($SelfContained.IsPresent) |
| .NET SDK | $sdkVersion |
| Git commit | $gitCommit |
| Warning lines | $warningCount |
| Published files | $($publishedFiles.Count) |
| Removed PDB files | $removedPdbCount |
| Archive | $archiveText |

## Outputs

- Published application: ``publish/``
- Build log: ``reports/build.log``
- File manifest: ``reports/publish-manifest.csv``
- SHA-256 checksums: ``reports/SHA256SUMS.txt``
$failureSection
"@ | Set-Content -LiteralPath $reportFile -Encoding UTF8
    }
}

if (-not $buildSucceeded) {
    Write-Error "Build failed: $failureMessage. See $logFile"
    exit 1
}

Write-Host "`nBuild succeeded." -ForegroundColor Green
Write-Host "Publish directory: $publishDirectory"
if (-not $SkipArchive.IsPresent) {
    Write-Host "Archive: $archiveFile"
}
Write-Host "Report: $reportFile"
