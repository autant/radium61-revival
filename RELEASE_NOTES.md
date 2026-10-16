# Radium61 Revival v5.0.0 — Windows x64

**Première version publique candidate** du pont USB WinUSB → MIDI Windows pour le M-Audio Radium 61.

### Testé sur la machine de référence

- Lecture du clavier avec WinUSB (PID `1015`), notes, vélocité, relâchements, potentiomètres et faders.
- Sortie audio via Microsoft GS Wavetable Synth et routage MIDI vers des applications via loopMIDI.
- Interface graphique et distribution **autonome multifichier ZIP** après extraction.

### À savoir avant d'installer

- Windows 11 x64 uniquement testé ; aucun engagement de compatibilité universelle.
- Firmware **non inclus**, doit avoir été chargé séparément ; WinUSB doit être associé au périphérique opérationnel PID `1015`.
- Pas d'installateur, ni installation automatique de pilote, ni mise à jour du firmware.
- Extraire **tous les fichiers du ZIP** : ne pas exécuter le binaire isolément.
- loopMIDI n'émet aucun son sans instrument MIDI récepteur.
- SysEx non pris en charge ; pitch bend et reconnexion après coupure de courant restent à valider.

Projet indépendant non affilié à M-Audio. Le code original est sous licence MIT ; les composants propriétaires du fabricant ne sont pas redistribués.
