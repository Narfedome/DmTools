<#
.SYNOPSIS
    Publie DmToolsApp (Windows + Android) et genere l'installeur Inno Setup, avec le meme numero
    de version que celui affiche dans l'application (Reglages > version).

.DESCRIPTION
    La version suit le meme schema que la cible SetVersionFromGit du csproj :
    AppVersionMajor.AppVersionMinor.<nombre de commits git> - lu ici depuis le csproj pour
    ne jamais s'en ecarter silencieusement.
    L'installeur Windows finit dans D:\Dev\DmTools\Installer. L'APK Android reste ou
    dotnet publish le genere (bin\Release\net10.0-android\publish\) : le csproj gere deja
    tout seul le format et la signature, pas besoin d'y toucher.

.PARAMETER SkipWindows
    N'effectue que la publication Android.

.PARAMETER SkipAndroid
    N'effectue que la publication Windows + installeur.

.EXAMPLE
    .\Build-Release.ps1
    Genere l'installeur Windows ET l'APK Android.

.EXAMPLE
    .\Build-Release.ps1 -SkipAndroid
    Ne genere que l'installeur Windows.
#>

param(
    [switch]$SkipWindows,
    [switch]$SkipAndroid
)

$ErrorActionPreference = "Stop"

$repoRoot    = $PSScriptRoot
$csprojPath  = Join-Path $repoRoot "DmToolsApp\DmToolsApp.csproj"
$issPath     = Join-Path $repoRoot "DmToolsApp\Installer.iss"
$isccPath    = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$outputDir   = Join-Path $repoRoot "Installer"

New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

# --- Version : major.minor du csproj + patch = nombre de commits git (identique a la cible
#     SetVersionFromGit du csproj, pour que l'installeur, l'APK et l'appli affichent le meme numero) ---
[xml]$csproj = Get-Content $csprojPath
$major = $csproj.Project.PropertyGroup.AppVersionMajor | Where-Object { $_ } | Select-Object -First 1
$minor = $csproj.Project.PropertyGroup.AppVersionMinor | Where-Object { $_ } | Select-Object -First 1
$patch = (git -C $repoRoot rev-list --count HEAD).Trim()
$version = "$major.$minor.$patch"

Write-Host "Version : $version" -ForegroundColor Cyan

# --- Windows : publish (unpackaged, cf. WindowsPackageType=None du csproj) + installeur Inno Setup ---
if (-not $SkipWindows) {
    if (-not (Test-Path $isccPath)) {
        throw "Inno Setup Compiler introuvable a '$isccPath'. Installe Inno Setup 6 ou ajuste `$isccPath dans ce script."
    }

    Write-Host "`n=== Windows : publication ===" -ForegroundColor Cyan
    dotnet publish $csprojPath -f net10.0-windows10.0.19041.0 -c Release -p:WindowsPackageType=None
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish (Windows) a echoue (code $LASTEXITCODE)." }

    Write-Host "=== Windows : compilation de l'installeur ===" -ForegroundColor Cyan
    & $isccPath "/DMyAppVersion=$version" $issPath
    if ($LASTEXITCODE -ne 0) { throw "ISCC a echoue (code $LASTEXITCODE)." }

    Write-Host "Installeur : $outputDir\DmToolsInstaller-$version.exe" -ForegroundColor Green
}

# --- Android : publish. Le csproj gere tout en interne (AndroidPackageFormat=apk force en
#     config Release, signature avec le keystore de debug faute de keystore perso configure) -
#     l'APK signe sort directement dans bin\Release\net10.0-android\publish\. ---
if (-not $SkipAndroid) {
    Write-Host "`n=== Android : publication ===" -ForegroundColor Cyan
    dotnet publish $csprojPath -f net10.0-android -c Release
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish (Android) a echoue (code $LASTEXITCODE)." }

    Write-Host "APK : $repoRoot\DmToolsApp\bin\Release\net10.0-android\publish\com.narfedome.dmtoolsapp-Signed.apk" -ForegroundColor Green
}
