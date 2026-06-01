param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "dist\installers",
    [switch]$Restore
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $repoRoot "ToastFish.sln"
$msbuildPath = Join-Path $repoRoot ".local\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
$releaseDir = Join-Path $repoRoot "bin\$Configuration"
$stagingRoot = Join-Path $repoRoot "dist\package-staging"
$stagingApp = Join-Path $stagingRoot "app"
$payloadPath = Join-Path $stagingRoot "payload.zip"
$installerOutDir = Join-Path $repoRoot $OutputDirectory
$installerPath = Join-Path $installerOutDir "Nihongo-ToastFish-Setup.exe"
$iexpressBuild = Join-Path $env:TEMP "NihongoToastFishIExpressBuild"
$iexpressPath = Join-Path $env:WINDIR "System32\iexpress.exe"

if (-not (Test-Path -LiteralPath $msbuildPath)) {
    throw "MSBuild was not found: $msbuildPath"
}

if (-not (Test-Path -LiteralPath $iexpressPath)) {
    throw "IExpress was not found: $iexpressPath"
}

function Add-ZipEntry {
    param(
        [System.IO.Compression.ZipArchive]$Archive,
        [string]$SourcePath,
        [string]$EntryName
    )

    $entry = $Archive.CreateEntry($EntryName.Replace("\", "/"), [System.IO.Compression.CompressionLevel]::Optimal)
    $inputStream = [System.IO.File]::Open($SourcePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $outputStream = $entry.Open()
        try {
            $inputStream.CopyTo($outputStream)
        }
        finally {
            $outputStream.Dispose()
        }
    }
    finally {
        $inputStream.Dispose()
    }
}

function New-PayloadZip {
    param(
        [string]$Destination,
        [string]$Root
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    Remove-Item -LiteralPath $Destination -Force -ErrorAction SilentlyContinue
    $zipStream = [System.IO.File]::Open($Destination, [System.IO.FileMode]::CreateNew, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
    try {
        $archive = New-Object System.IO.Compression.ZipArchive($zipStream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
        try {
            Add-ZipEntry -Archive $archive -SourcePath (Join-Path $Root "install.cmd") -EntryName "install.cmd"
            $appRoot = Join-Path $Root "app"
            Get-ChildItem -LiteralPath $appRoot -Recurse -File -Force | ForEach-Object {
                $relativePath = $_.FullName.Substring($Root.Length).TrimStart("\", "/")
                Add-ZipEntry -Archive $archive -SourcePath $_.FullName -EntryName $relativePath
            }
        }
        finally {
            $archive.Dispose()
        }
    }
    finally {
        $zipStream.Dispose()
    }
}

Write-Host "Building $Configuration..."
$buildArgs = @($solutionPath, "/p:Configuration=$Configuration", "/m")
if ($Restore) {
    $buildArgs = @($solutionPath, "/restore", "/p:Configuration=$Configuration", "/m")
}
& $msbuildPath @buildArgs
if ($LASTEXITCODE -ne 0) {
    throw "MSBuild failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath (Join-Path $releaseDir "Nihongo ToastFish.exe"))) {
    throw "Release executable was not found in $releaseDir"
}

Write-Host "Preparing staging files..."
Remove-Item -LiteralPath $stagingRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $stagingApp -Force | Out-Null
New-Item -ItemType Directory -Path $installerOutDir -Force | Out-Null

Get-ChildItem -LiteralPath $releaseDir -Force |
    Where-Object { $_.Name -ne "app.publish" -and $_.Extension -ne ".pdb" } |
    Copy-Item -Destination $stagingApp -Recurse -Force

Copy-Item -LiteralPath (Join-Path $PSScriptRoot "installer\install.cmd") -Destination $stagingRoot -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "installer\bootstrap.cmd") -Destination $stagingRoot -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot "installer\uninstall.cmd") -Destination $stagingApp -Force

Remove-Item -LiteralPath $payloadPath -Force -ErrorAction SilentlyContinue
New-PayloadZip -Destination $payloadPath -Root $stagingRoot
if (-not (Test-Path -LiteralPath $payloadPath)) {
    throw "Failed to create payload zip."
}

Write-Host "Creating IExpress package..."
Remove-Item -LiteralPath $iexpressBuild -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $iexpressBuild -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $stagingRoot "bootstrap.cmd") -Destination $iexpressBuild -Force
Copy-Item -LiteralPath $payloadPath -Destination $iexpressBuild -Force

$tempInstaller = Join-Path $iexpressBuild "Nihongo-ToastFish-Setup.exe"
$sedPath = Join-Path $iexpressBuild "NihongoToastFish.sed"
$sed = @"
[Version]
Class=IEXPRESS
SEDVersion=3
[Options]
PackagePurpose=InstallApp
ShowInstallProgramWindow=1
HideExtractAnimation=0
UseLongFileName=1
InsideCompressed=1
CAB_FixedSize=0
CAB_ResvCodeSigning=0
RebootMode=N
InstallPrompt=
DisplayLicense=
FinishMessage=Nihongo ToastFish installation finished.
TargetName=$tempInstaller
FriendlyName=Nihongo ToastFish Setup
AppLaunched=bootstrap.cmd
PostInstallCmd=<None>
AdminQuietInstCmd=
UserQuietInstCmd=
SourceFiles=SourceFiles
[SourceFiles]
SourceFiles0=$iexpressBuild
[SourceFiles0]
%FILE0%=
%FILE1%=
[Strings]
FILE0="bootstrap.cmd"
FILE1="payload.zip"
"@
Set-Content -LiteralPath $sedPath -Value $sed -Encoding ASCII

& $iexpressPath /N $sedPath
for ($i = 0; $i -lt 180 -and -not (Test-Path -LiteralPath $tempInstaller); $i++) {
    Start-Sleep -Milliseconds 500
}

if ($LASTEXITCODE -ne 0 -and -not (Test-Path -LiteralPath $tempInstaller)) {
    throw "IExpress failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $tempInstaller)) {
    throw "IExpress did not create installer: $tempInstaller"
}

Copy-Item -LiteralPath $tempInstaller -Destination $installerPath -Force
$hash = Get-FileHash -LiteralPath $installerPath -Algorithm SHA256
$sizeMb = [Math]::Round((Get-Item -LiteralPath $installerPath).Length / 1MB, 2)

Write-Host "Installer created: $installerPath"
Write-Host "Size: $sizeMb MB"
Write-Host "SHA256: $($hash.Hash)"
