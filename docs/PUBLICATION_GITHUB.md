# Procédure de publication GitHub v5.0

Ce kit contient le **code source**, pas le binaire Windows généré sur l'ordinateur de validation.

## 1. Valider l'archive publique

- Confirmer que `MAudio*.bin`, `*.sys`, `oem*.inf`, fichiers de capture personnels et répertoires `bin/`, `obj/`, `dist/` ne figurent pas dans le dépôt.
- Lire le code, les README et la licence MIT avant publication.
- Ne pas publier les fichiers firmware/pilotes M-Audio sans autorisation.

## 2. Compiler sur Windows 11 x64

Depuis la racine du projet :

```powershell
dotnet publish .\Radium61Revival\Radium61Revival.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o .\dist\Radium61Revival-win-x64
```

Tester `dist\Radium61Revival-win-x64\Radium61Revival.exe`, fermer le programme, puis :

```powershell
Compress-Archive -Path '.\dist\Radium61Revival-win-x64\*' -DestinationPath '.\dist\Radium61Revival-v5.0-win-x64.zip'
Get-FileHash '.\dist\Radium61Revival-v5.0-win-x64.zip' -Algorithm SHA256
```

Extraire dans un **nouveau dossier** et retester l'exécutable (le test du paquet extrait a été réalisé sur la machine de référence).

## 3. Créer un dépôt

Sur github.com, créer un dépôt vide `radium61-revival` sans README initial, puis dans la racine locale du projet :

```powershell
git init
git add .
git commit -m "Release candidate v5.0"
git branch -M main
git remote add origin https://github.com/UTILISATEUR/radium61-revival.git
git push -u origin main
```

Remplacer `UTILISATEUR` par le compte GitHub choisi ; ne pas publier de secrets. Vérifier l'auteur Git configuré avant le commit.

## 4. Release

GitHub → dépôt → **Releases** → **Draft a new release** → tag `v5.0.0`, titre `Radium61 Revival v5.0 — Windows x64`. Joindre **le ZIP multifichier compilé et testé** (pas l'archive de sources seule) et les notes de `RELEASE_NOTES.md`.

La solution nécessite encore le chargement préalable du firmware et l'association WinUSB. Ne pas annoncer une installation plug-and-play.
