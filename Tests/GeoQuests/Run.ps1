$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runner = Join-Path $projectRoot 'Temp/GeoQuestChecks'
New-Item -ItemType Directory -Force -Path $runner | Out-Null
$sourceRoot = [System.Security.SecurityElement]::Escape($projectRoot)
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$sourceRoot/Assets/Scripts/Quests/Geo/*.cs" Exclude="$sourceRoot/Assets/Scripts/Quests/Geo/GeoLocationProvider.cs" />
    <Compile Include="$sourceRoot/Tests/GeoQuests/Program.cs" />
    <Compile Include="$sourceRoot/Tests/GeoQuests/UnityDoubles.cs" />
    <Compile Include="$sourceRoot/Tests/GeoQuests/CheckInChecks.cs" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $runner 'GeoQuestChecks.csproj') -Encoding UTF8
$nugetConfig = Join-Path $runner 'NuGet.Config'
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -LiteralPath $nugetConfig -Encoding UTF8
dotnet restore (Join-Path $runner 'GeoQuestChecks.csproj') --configfile $nugetConfig
if ($LASTEXITCODE -ne 0) { throw 'Geo quest restore failed.' }
dotnet run --project (Join-Path $runner 'GeoQuestChecks.csproj') --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Geo quest checks failed.' }

