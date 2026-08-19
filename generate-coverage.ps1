[CmdletBinding()]
param(
    [switch]$OpenReport,
    [string]$Threshold = ""
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host "Restoring local .NET tools..." -ForegroundColor Cyan
dotnet tool restore

Write-Host "Running tests with code coverage..." -ForegroundColor Cyan
$testArgs = @(
    "test",
    "HappyPaws.sln",
    "-c", "Release",
    "--settings", "coverlet.runsettings",
    "--collect:XPlat Code Coverage",
    "--results-directory", "TestResults"
)

if ($Threshold -ne "") {
    $testArgs += "--"
    $testArgs += "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Threshold=$Threshold"
}

dotnet @testArgs

$coverageFiles = Get-ChildItem -Path "TestResults" -Recurse -Filter "coverage.cobertura.xml"
if (-not $coverageFiles) {
    Write-Error "No coverage.cobertura.xml files found in TestResults."
    exit 1
}

Write-Host "Generating HTML and Badge coverage reports..." -ForegroundColor Cyan
$reportsParam = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
$targetDir = Join-Path $scriptDir "TestResults\CoverageReport"

dotnet reportgenerator `
    -reports:"$reportsParam" `
    -targetdir:"$targetDir" `
    -reporttypes:"Html;Badges;MarkdownSummary" `
    -historydir:"TestResults\CoverageHistory"

Write-Host "Coverage report generated at: $targetDir\index.html" -ForegroundColor Green

if ($OpenReport) {
    Start-Process (Join-Path $targetDir "index.html")
}
