$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'LuminaPlayer/LuminaPlayer.csproj'
$out = Join-Path $PSScriptRoot 'publish'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
dotnet restore $project
dotnet publish $project -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false `
  -o $out
Write-Host "Publicado em: $out/LuminaPlayer.exe"
