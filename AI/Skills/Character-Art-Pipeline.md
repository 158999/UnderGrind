# Skill — Personaggi, mesh e pipeline Blender/Unity

**Attivazione:** modelli umanoidi, separazione mesh, rig, skinning, FBX, prefab, equipaggiamento e import Unity.

**Fonti omgskills:** [blender-modeling](https://github.com/roble3/cc-blender-skill/tree/a2634b588e80265e796aa53f8ab178d75105c2af), [build-rigged-game-assets](https://github.com/MengTo/Skills/tree/a45892d2fdadab75b31fb16abecc02f101b31c43), [asset-audit](https://github.com/Donchitos/Claude-Code-Game-Studios/tree/05ad7c8a5b8aa2aa86bbd3439ae7cf7295cac418).

## Contratto UnderGrind

Preservare le sorgenti maschile e femminile in `Assets/UnderGrind/Art/Source/humans.blend`. Due rig: `UG_Male_Armature`, `UG_Female_Armature`. Sei mesh figlie per personaggio: `Human_<Character>_Body`, `Head`, `TP_Arms`, `TP_Hands`, `FP_Arms`, `FP_Hands`. Altezze di riferimento documentate: 1,78 m maschile e 1,70 m femminile, configurabili nel profilo rig.

## Procedura

1. Leggere `Documentation/Humanoid-Rig-Pipeline.md`, `Player-Locomotion.md`, `Tools/Blender/README.md` e `PlayerRigSettings.asset`.
2. Inventariare mesh, armature, ossa, modificatori, vertex groups, pesi, materiali, UV, scale e trasformazioni.
3. Distinguere controlli strutturali da qualità della deformazione: nomi e gerarchia corretti non dimostrano skinning valido.
4. Verificare Humanoid mapping, orientamento, rest pose, scale applicate, parent, Armature modifier e pesi per vertice.
5. Controllare corrispondenza con PlayerRigSettings e gli script di assemblaggio.
6. Usare `Tools/Blender/run-export.ps1`; non usare `-Force` senza autorizzazione esplicita.
7. Verificare report, file generati, import Unity, Avatar e prefab.
8. Provare deformazioni e visibilità FP/TP in PlayMode e aggiornare la documentazione.

Il plugin GitHub non equivale a Blender o Unity aperti. Se l'istanza non dispone di accesso operativo a questi strumenti, non dichiarare mesh modificate, pesi validati, FBX esportati o prefab verificati. Preparare invece script/istruzioni precise o usare gli strumenti realmente collegati alla sessione.

**Accettazione:** sorgenti intatte, due rig e sei mesh coerenti, export validato, import Unity e deformazioni verificate separatamente.
