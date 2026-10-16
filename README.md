# Radium61 Revival v5.0 🎹

**FR** | [English](README.en.md)

Projet communautaire indépendant : pont **USB WinUSB → MIDI Windows** pour le clavier **M-Audio Radium 61** sur Windows 11 x64. Il récupère les événements reçus de l'appareil opérationnel `VID_0763&PID_1015` et les transmet à un périphérique de sortie MIDI Windows (par exemple loopMIDI ou Microsoft GS Wavetable Synth).

## État du projet

**Testé sur un PC Windows 11 x64** : notes, vélocité, relâchements, potentiomètres et faders ; interface graphique ; exécutable autonome **multifichier** extrait d'une archive ZIP. La reconnexion automatique existe dans le projet, mais son comportement selon le type de débranchement n'a pas été validé sur plusieurs machines. **Aucune garantie de fonctionnement sur d'autres PC.**

## Prérequis

- Un M-Audio Radium 61 avec son **firmware déjà chargé**, visible comme `VID_0763&PID_1015` ; pilote **WinUSB** associé à ce périphérique opérationnel.
- Windows 11 x64 (autres versions non testées).
- Pour un séquenceur : [loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html), avec un port virtuel ouvert. Pour tester le son : `Microsoft GS Wavetable Synth`.
- SDK .NET 8 seulement si vous compilez les sources ; non requis avec la publication autonome multifichier.

**Attention : ce logiciel n'installe pas de pilote et ne charge pas le firmware.** Après une mise hors tension, le clavier peut réapparaître en `PID_1014` ; la procédure de restauration reste séparée et demande des fichiers obtenus légalement. Voir [Installation](docs/INSTALLATION_FR.md).

## Utilisation

1. Vérifier que le Radium 61 est détecté sous `PID_1015` avec WinUSB.
2. Lancer `Radium61Revival.exe` depuis un dossier où **tous les fichiers extraits du ZIP** sont présents.
3. Choisir une sortie MIDI : synthétiseur Windows pour test audio, ou port loopMIDI pour un logiciel musical.
4. Cliquer sur **Démarrer**. Arrêter proprement avant de fermer la fenêtre.

Les paramètres sont mémorisés sous `%LOCALAPPDATA%\Radium61Inspector\player-settings.json`. Des journaux USB peuvent être créés dans `%LOCALAPPDATA%\Radium61Inspector` ; vous pouvez les supprimer si vous n'en avez plus besoin.

## Compilation et packaging Windows

```powershell
dotnet build .\Radium61Revival\Radium61Revival.csproj
dotnet publish .\Radium61Revival\Radium61Revival.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o .\dist\Radium61Revival-win-x64
Compress-Archive -Path '.\dist\Radium61Revival-win-x64\*' -DestinationPath '.\dist\Radium61Revival-v5.0-win-x64.zip'
```

Fermer l'application avant compression si Windows verrouille une DLL. **Ne pas activer `PublishSingleFile=true`** pour cette release : ce mode a compilé, mais n'a pas démarré sur la machine de validation. Le script `Publier-Windows-x64.ps1` suit désormais la méthode multifichier ; si PowerShell bloque les scripts, utiliser les commandes `dotnet` ci-dessus sans changer la politique d'exécution.

## Contenu du dépôt

- `Radium61Revival/` : source C# du lecteur Windows Forms, décodage et pont MIDI.
- `docs/INSTALLATION_FR.md` et `docs/INSTALLATION_EN.md` : mode d'emploi.
- `docs/PROTOCOLE_USB.md` : observations du protocole, avec limites de validation.
- `docs/PUBLICATION_GITHUB.md` : création de la release et de ses fichiers.
- `CHANGELOG.md` : évolutions.
- `LICENSE` : licence MIT du code original.

## Sécurité, redistribution et limites

- Le lecteur effectue des lectures USB ; aucune écriture sur les endpoints USB OUT ni installation automatique de pilote. L'ouverture WinUSB peut demander les droits lecture/écriture au niveau du handle.
- **Aucun firmware, pilote, image mémoire ou binaire M-Audio n'est inclus.** Ne pas ajouter ces éléments aux releases sans en vérifier les droits de redistribution.
- Ne pas contourner la signature des pilotes ni désactiver les protections Windows.
- SysEx non pris en charge ; pitch bend et compatibilité générale non exhaustivement testés.
- Projet indépendant, **non affilié à M-Audio**. Les marques appartiennent à leurs titulaires.

Contributions et tests sur d'autres configurations bienvenus. [English documentation](README.en.md).
