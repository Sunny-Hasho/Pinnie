# Publish Release Script for Pinnie


$projectPath = "./PinMe/Pinnie.csproj" 
$outputDir = "./Releases"

# Clean previous releases
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
    Write-Host "Cleaned previous release directory." -ForegroundColor Yellow
}

# 1. Portable Version (Self-Contained, No .NET install required)
Write-Host "Building Portable Version (Self-Contained)..." -ForegroundColor Cyan
& dotnet publish $projectPath -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false -o "$outputDir/Portable"

# 2. Lightweight Version (Framework-Dependent, Requires .NET 8)
Write-Host "Building Lightweight Version (Framework-Dependent)..." -ForegroundColor Cyan
& dotnet publish $projectPath -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:PublishTrimmed=false -o "$outputDir/Lightweight"

Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "Artifacts are in $outputDir" -ForegroundColor Green
