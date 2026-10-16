# Installation de Radium61 Revival v5.0 (Windows 11 x64)

## Avant de commencer

L'application publiée est autonome du point de vue de .NET, mais **n'inclut ni pilote WinUSB ni firmware**. Elle ne peut pas initialiser un clavier détecté en mode chargeur `VID_0763&PID_1014`. La restauration du firmware a été réalisée expérimentalement sur un seul appareil et n'est pas intégrée à cette version publique.

Si le clavier est déjà en `VID_0763&PID_1015` avec WinUSB, passez à la suite. Sinon, consultez la documentation technique du projet avant toute modification de pilote. Toute association WinUSB doit viser **le bon périphérique et le bon PID** ; ne remplacez pas les pilotes d'autres appareils. Conservez les protections Windows.

## Exécuter

1. Téléchargez la **release ZIP Windows x64** depuis la page Releases GitHub, une fois publiée.
2. Extrayez intégralement le ZIP dans un dossier, par exemple `C:\Applications\Radium61Revival`.
3. Lancez `Radium61Revival.exe`. Ne déplacez pas l'exécutable seul.
4. Pour obtenir du son immédiatement, choisissez `Microsoft GS Wavetable Synth` puis cliquez **Démarrer**.
5. Pour un DAW, installez et ouvrez loopMIDI, créez `Radium61 Virtual`, choisissez ce nom dans Revival (sortie) et dans le DAW (entrée). Activez un instrument et le monitoring MIDI si nécessaire.
6. À la fin de la session, cliquez **Arrêter**, puis fermez la fenêtre.

## Limitations et dépannage

- **Aucun son avec loopMIDI seul :** c'est un routeur MIDI, pas un synthétiseur. Chargez un instrument dans le DAW.
- **Application ne démarre pas :** vérifier que tous les fichiers du ZIP sont extraits ; la version monofichier n'a pas été validée.
- **Clavier non détecté :** vérifier `VID_0763&PID_1015` et WinUSB. Si `PID_1014`, le firmware est à recharger séparément.
- **Contrôleurs :** touches, faders et potentiomètres ont été testés sur la machine de référence ; pitch bend, SysEx et tous les scénarios de reconnexion ne sont pas garantis.
- **Journaux :** `%LOCALAPPDATA%\Radium61Inspector`.

Ne désactivez ni Defender ni la vérification des signatures de pilotes.
