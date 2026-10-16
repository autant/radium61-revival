# Build multi-file self-contained release (single-file did not start on reference PC).
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$project = '.\Radium61Revival\Radium61Revival.csproj'
$destination = '.\dist\Radium61Revival-win-x64'
Write-Host 'Publishing Radium61 Revival 5.0 (Windows x64, multi-file)...'
dotnet publish $project -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o $destination
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed: $LASTEXITCODE" }
Write-Host "Published to $destination"
Write-Host 'Close the player before ZIP compression.'
