$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$webRoot = Join-Path $projectRoot 'rpg_quest_parent_web_starter'
$output = Join-Path $projectRoot 'Temp/ParentGeoChecks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
Push-Location $webRoot
try {
    node node_modules/typescript/bin/tsc --ignoreConfig --target ES2020 --module commonjs --skipLibCheck --outDir $output src/lib/geoQuest.ts src/lib/types.ts
    if ($LASTEXITCODE -ne 0) { throw 'Geo quest TypeScript compilation failed.' }
    node --test (Join-Path $PSScriptRoot 'geo-quest.test.cjs')
    if ($LASTEXITCODE -ne 0) { throw 'Parent geo quest tests failed.' }
} finally { Pop-Location }
