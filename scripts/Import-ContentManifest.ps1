param(
    [Parameter(Mandatory = $true)]
    [string] $ManifestPath,

    [Parameter(Mandatory = $true)]
    [string] $DatabasePath,

    [string] $BuildOutput = ".\bin\Debug"
)

$ErrorActionPreference = "Stop"

$resolvedBuildOutput = Resolve-Path $BuildOutput
$resolvedManifest = Resolve-Path $ManifestPath
$databaseParent = Split-Path -Parent $DatabasePath
if (-not [string]::IsNullOrWhiteSpace($databaseParent) -and -not (Test-Path $databaseParent)) {
    New-Item -ItemType Directory -Path $databaseParent | Out-Null
}
$resolvedDatabase = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($DatabasePath)
$sqliteAssembly = Join-Path $resolvedBuildOutput "System.Data.SQLite.dll"
$appAssembly = Join-Path $resolvedBuildOutput "ToastFish.exe"

Add-Type -Path $sqliteAssembly
[System.Reflection.Assembly]::LoadFrom($appAssembly) | Out-Null

$connection = New-Object System.Data.SQLite.SQLiteConnection("Data Source=$resolvedDatabase;Version=3;")
$connection.Open()
try {
    $migrator = New-Object ToastFish.Model.Storage.ContentSchemaMigrator
    $migrator.EnsureCreated($connection)

    $importer = New-Object ToastFish.Services.ContentUpdate.ContentPackImporter
    $result = $importer.ImportManifest($resolvedManifest, $connection)

    Write-Output "SourcesImported=$($result.SourcesImported)"
    Write-Output "PacksImported=$($result.PacksImported)"
    Write-Output "VocabularyItemsImported=$($result.VocabularyItemsImported)"
    Write-Output "GrammarExamplesImported=$($result.GrammarExamplesImported)"
    Write-Output "GojuonItemsImported=$($result.GojuonItemsImported)"
    Write-Output "GrammarPointsImported=$($result.GrammarPointsImported)"
}
finally {
    $connection.Dispose()
}
