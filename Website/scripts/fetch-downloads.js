// Récupère l'exe Windows et l'APK Android de la dernière GitHub Release avant que Netlify ne
// publie le site : ces binaires ne sont jamais commités dans le dépôt (cf. Build-Release.ps1
// -Publish et le .gitignore de Website/downloads/), donc le site les télécharge lui-même à
// chaque build plutôt que de dépendre d'une copie versionnée qui ferait gonfler l'historique git
// à chaque nouvelle release.
"use strict";

const https = require("https");
const fs = require("fs");
const path = require("path");

const REPO = "Narfedome/DmTools";
const ASSETS = ["DmToolsInstaller.exe", "DmTools.apk"];
const OUT_DIR = path.join(__dirname, "..", "downloads");

function get(url, headers) {
  return new Promise((resolve, reject) => {
    https
      .get(url, { headers: { "User-Agent": "dmtools-netlify-build", ...headers } }, (res) => {
        // GitHub redirige les assets de release vers un stockage temporaire (S3) : il faut suivre
        // la redirection nous-mêmes, https.get() ne le fait pas automatiquement.
        if (res.statusCode >= 300 && res.statusCode < 400 && res.headers.location) {
          res.resume();
          return resolve(get(res.headers.location, headers));
        }
        if (res.statusCode !== 200) {
          res.resume();
          return reject(new Error(`${url} -> HTTP ${res.statusCode}`));
        }
        resolve(res);
      })
      .on("error", reject);
  });
}

async function readJson(url) {
  const res = await get(url, { Accept: "application/vnd.github+json" });
  let body = "";
  for await (const chunk of res) body += chunk;
  return JSON.parse(body);
}

function download(url, destPath) {
  return get(url).then(
    (res) =>
      new Promise((resolve, reject) => {
        const file = fs.createWriteStream(destPath);
        res.pipe(file);
        file.on("finish", () => file.close(resolve));
        file.on("error", reject);
      })
  );
}

// Met à jour "Version actuelle : X.Y.Z" dans les deux pages de changelog à partir du tag de la
// release (v1.0.124 -> 1.0.124) : la version affichée au public reste synchronisée avec le vrai
// binaire distribué sans avoir à l'éditer à la main à chaque release.
const CHANGELOG_FILES = [path.join(__dirname, "..", "changelog.html"), path.join(__dirname, "..", "en", "changelog.html")];
const VERSION_PATTERN = /(<strong id="current-version">)[^<]*(<\/strong>)/;

function updateChangelogVersion(version) {
  for (const file of CHANGELOG_FILES) {
    const html = fs.readFileSync(file, "utf8");
    if (!VERSION_PATTERN.test(html)) {
      console.warn(`Marqueur de version introuvable dans ${file} - non mis à jour.`);
      continue;
    }
    fs.writeFileSync(file, html.replace(VERSION_PATTERN, `$1${version}$2`));
    console.log(`Version affichée dans ${path.basename(file)} : ${version}`);
  }
}

async function main() {
  fs.mkdirSync(OUT_DIR, { recursive: true });

  const release = await readJson(`https://api.github.com/repos/${REPO}/releases/latest`);
  const version = release.tag_name.replace(/^v/, "");

  for (const name of ASSETS) {
    const asset = (release.assets || []).find((a) => a.name === name);
    if (!asset) {
      console.warn(`Asset "${name}" absent de la release ${release.tag_name} - ignoré.`);
      continue;
    }
    console.log(`Téléchargement de ${name} (${release.tag_name})...`);
    await download(asset.browser_download_url, path.join(OUT_DIR, name));
  }

  updateChangelogVersion(version);

  console.log("Téléchargement des binaires terminé.");
}

main().catch((err) => {
  console.error("Échec du téléchargement des binaires :", err.message);
  // Ne fait pas échouer le build : publier le site avec des liens de téléchargement obsolètes
  // (ou temporairement absents) reste préférable à bloquer toute la mise à jour du site pour un
  // souci de récupération d'assets transitoire (rate-limit GitHub, panne, etc.).
  process.exit(0);
});
