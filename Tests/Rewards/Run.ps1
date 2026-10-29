$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$runner = Join-Path $projectRoot 'Temp/RewardChecks'
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
    <Compile Include="$sourceRoot/Assets/Scripts/Rewards/CloudFunctionsService.cs" />
    <Compile Include="$sourceRoot/Tests/Rewards/*.cs" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $runner 'RewardChecks.csproj') -Encoding UTF8
$nugetConfig = Join-Path $runner 'NuGet.Config'
'<configuration><packageSources><clear /></packageSources></configuration>' | Set-Content -LiteralPath $nugetConfig -Encoding UTF8
dotnet restore (Join-Path $runner 'RewardChecks.csproj') --configfile $nugetConfig
if ($LASTEXITCODE -ne 0) { throw 'Reward checks restore failed.' }
dotnet run --project (Join-Path $runner 'RewardChecks.csproj') --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Reward checks failed.' }
