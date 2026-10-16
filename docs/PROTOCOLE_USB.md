# Observations sur le transport USB MIDI du Radium 61

**Statut : rétro-ingénierie expérimentale à partir de captures locales ; pas une spécification officielle.**

## Appareil opérationnel

- VID `0763`, PID `1015` après chargement du firmware ; état préalable observé : PID `1014`.
- Interface vendor-specific (`0xFF`) : le périphérique ne se présente pas comme une interface USB MIDIStreaming standard.
- Endpoints observés : **interrupt IN `0x81` (max 32 octets)**, **bulk IN `0x82` (max 32 octets)** et **bulk OUT `0x02` (max 32 octets)**.
- Revival ne lit que le endpoint `0x81` et n'envoie pas de commandes USB OUT.

## Encapsulation observée

Les transferts contiennent couramment des enregistrements de 4 octets. Le décodeur émet l'hypothèse que le 4e octet encode le nombre d'octets MIDI utiles (1 ou 2). Les données se reconstruisent sur plusieurs enregistrements :

```text
90 00 00 01 -> 0x90 (status Note On, canal 1)
3C 00 00 01 -> 0x3C (note 60)
2D 00 00 01 -> 0x2D (vélocité 45)
                 MIDI reconstitué : 90 3C 2D

90 00 00 01 -> 0x90
3C 00 00 01 -> 0x3C
00 00 00 01 -> 0x00
                 MIDI reconstitué : 90 3C 00 (Note Off par vélocité nulle)
```

Dans d'autres captures, les enregistrements `47 11 00 02` et `44 2D 00 02` suggèrent deux octets utiles (`47 11` et `44 2D`). La séquence `FE 00 00 01` a été observée périodiquement (environ toutes les 0,28 s) et traitée comme bruit/activité, non comme Note On/Off. `FE FE FE 03` apparaît dans les transferts initiaux ; sa signification exacte est inconnue.

## Validation et incertitudes

La capture contrôlée de la note MIDI 60 a permis de reconstruire quatre couples Note On / Note Off avec vélocités 45, 50, 83 et 81 ; pas d'anomalie détectée sur les 75 paquets de cet essai. La v4.7 a ensuite émis 192 messages MIDI sur un essai plus long, et les versions graphiques ont produit du son et transmis les contrôleurs sur la machine de référence. **Cela ne suffit pas à établir la totalité du protocole** : SysEx non pris en charge, pitch bend et variantes éventuelles non entièrement testés.

Aucun pilote original, firmware ou désassemblage propriétaire n'est fourni avec ce dépôt.
