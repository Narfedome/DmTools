<#
.SYNOPSIS
    Publie DmToolsApp (Windows + Android) et genere l'installeur Inno Setup, avec le meme numero
    de version que celui affiche dans l'application (Reglages > version).

.DESCRIPTION
    La version suit le meme schema que la cible SetVersionFromGit du csproj :
    AppVersionMajor.AppVersionMinor.<nombre de commits git> - lu ici depuis le csproj pour
    ne jamais s'en ecarter silencieusement.
    Les deux artefacts finissent sous D:\Dev\DmTools\Installer, dans des sous-dossiers Windows\
    et Android\, avec un nom de fichier fixe (DmToolsInstaller.exe / DmTools.apk) : pas de numero
    de version dans le nom, pour que le fichier mis a disposition du public (lien de telechargement,
    etc.) puisse etre remplace tel quel a chaque nouvelle version sans changer le lien. La signature
    Android vient de la keystore de release partagee si dmtools-release.keystore est present a la
    racine du repo (recupere depuis le Drive partage, jamais commite) et que Build-Release.local.ps1
    definit ses identifiants (a creer une fois par machine depuis Build-Release.local.ps1.example) ;
    sinon elle retombe sur le debug.keystore local, qui differe d'un PC a l'autre.

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

# Tout le corps du script est dans ce try/catch : lance en double-clic (pas depuis un terminal deja
# ouvert), une erreur non rattrapee fermerait sinon la fenetre PowerShell instantanement avec elle,
# sans laisser le temps de lire le message.
try {
    $repoRoot         = $PSScriptRoot
    $csprojPath       = Join-Path $repoRoot "DmToolsApp\DmToolsApp.csproj"
    $issPath          = Join-Path $repoRoot "DmToolsApp\Installer.iss"
    $isccPath         = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    $outputDir        = Join-Path $repoRoot "Installer"
    $outputDirWindows = Join-Path $outputDir "Windows"
    $outputDirAndroid = Join-Path $outputDir "Android"

    New-Item -ItemType Directory -Force -Path $outputDirWindows, $outputDirAndroid | Out-Null

    # --- Config locale (jamais commitee, cf. .gitignore) : chemin + mots de passe de la keystore de
    #     release, propres a chaque machine. Copier Build-Release.local.ps1.example -> Build-Release.local.ps1
    #     et renseigner les 4 valeurs une seule fois par PC. ---
    $localConfigPath = Join-Path $repoRoot "Build-Release.local.ps1"
    if (Test-Path $localConfigPath) {
        . $localConfigPath
    }

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
        # /O et /F surchargent OutputDir/OutputBaseFilename du .iss : nom de fichier fixe (sans version)
        # pour que le lien de telechargement public n'ait jamais besoin de changer.
        & $isccPath "/DMyAppVersion=$version" "/O$outputDirWindows" "/FDmToolsInstaller" $issPath
        if ($LASTEXITCODE -ne 0) { throw "ISCC a echoue (code $LASTEXITCODE)." }

        Write-Host "Installeur : $outputDirWindows\DmToolsInstaller.exe" -ForegroundColor Green
    }

    # --- Android : publish. Le csproj gere le format (AndroidPackageFormat=apk force en config
    #     Release) ; la signature vient de la keystore de release partagee, attendue a la racine du
    #     repo sous dmtools-release.keystore (gitignoree - cf. .gitignore) : recuperer le fichier
    #     depuis le Drive partage et le deposer a cote de ce script suffit, aucun chemin a configurer.
    #     Sans ce fichier, dotnet publish retombe sur le debug.keystore auto-genere par machine, ce
    #     qui produit une signature differente d'un PC a l'autre et fait refuser les mises a jour par
    #     Android ("app not installed as package conflicts with an existing package"). ---
    if (-not $SkipAndroid) {
        Write-Host "`n=== Android : publication ===" -ForegroundColor Cyan

        $keystorePath  = Join-Path $repoRoot "dmtools-release.keystore"
        $keystoreAlias = $env:DMTOOLS_KEYSTORE_ALIAS
        $storePass     = $env:DMTOOLS_KEYSTORE_STOREPASS
        $keyPass       = $env:DMTOOLS_KEYSTORE_KEYPASS

        $signingArgs = @()
        if ((Test-Path $keystorePath) -and $keystoreAlias -and $storePass -and $keyPass) {
            Write-Host "Signature : keystore de release ($keystorePath)" -ForegroundColor DarkGray
            $signingArgs = @(
                "-p:AndroidKeyStore=true"
                "-p:AndroidSigningKeyStore=$keystorePath"
                "-p:AndroidSigningKeyAlias=$keystoreAlias"
                "-p:AndroidSigningStorePass=$storePass"
                "-p:AndroidSigningKeyPass=$keyPass"
            )
        } else {
            Write-Warning "dmtools-release.keystore absent de la racine du repo (ou identifiants manquants dans Build-Release.local.ps1) : signature avec le debug.keystore local (differente d'un PC a l'autre, a eviter pour une release distribuee)."
        }

        dotnet publish $csprojPath -f net10.0-android -c Release @signingArgs
        if ($LASTEXITCODE -ne 0) { throw "dotnet publish (Android) a echoue (code $LASTEXITCODE)." }

        # Copie sous un nom fixe (sans version) dans Installer\Android\, a cote de l'installeur Windows :
        # meme raison que pour l'exe, le lien de telechargement public n'a jamais besoin de changer.
        $publishedApk = Join-Path $repoRoot "DmToolsApp\bin\Release\net10.0-android\publish\com.narfedome.dmtoolsapp-Signed.apk"
        if (-not (Test-Path $publishedApk)) { throw "APK signe introuvable a '$publishedApk'." }
        $apkPath = Join-Path $outputDirAndroid "DmTools.apk"
        Copy-Item -Path $publishedApk -Destination $apkPath -Force

        Write-Host "APK : $apkPath" -ForegroundColor Green
    }
}
catch {
    Write-Host "`nECHEC : $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "`nAppuie sur Entree pour fermer"
    exit 1
}
