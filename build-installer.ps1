param([string]$DotNet = 'dotnet', [string]$InnoCompiler = 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe')
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    & $DotNet test tests/StatsScreen.Tests/StatsScreen.Tests.csproj -c Release -m:1 /nr:false
    if ($LASTEXITCODE) { throw 'Tests failed.' }
    & $DotNet publish src/StatsScreen/StatsScreen.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish -m:1 /nr:false
    if ($LASTEXITCODE) { throw 'Publish failed.' }
    Copy-Item README.md artifacts/publish/README.md
    Copy-Item THIRD-PARTY-NOTICES.txt artifacts/publish/THIRD-PARTY-NOTICES.txt
    New-Item -ItemType Directory -Force artifacts/publish/licenses | Out-Null
    Copy-Item licenses/* artifacts/publish/licenses -Recurse -Force
    if ((Get-AuthenticodeSignature installer/vendor/PawnIO_setup.exe).Status -ne 'Valid') { throw 'PawnIO signature validation failed.' }
    & $InnoCompiler installer/StatsScreen.iss
    if ($LASTEXITCODE) { throw 'Installer compilation failed.' }
} finally { Pop-Location }
