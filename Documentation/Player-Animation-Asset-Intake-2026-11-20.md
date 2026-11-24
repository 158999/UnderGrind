# Intake animazioni Player — 20 novembre 2026

## Scopo

Selezione di clip di locomozione per il Player umanoide UnderGrind. Gli asset sono stati acquisiti dalla libreria di movimento PINOC senza generare nuovi contenuti e senza consumare crediti.

## Asset selezionati

| Uso | Clip PINOC | Asset ID |
|---|---|---|
| Idle | Standing Idle | `1cbf45b5-5abe-4a88-97cd-a1a21da60d7b` |
| Camminata | Walking | `32d71c7f-e88a-4d1f-9c0c-1fde2da5f060` |
| Corsa | Running | `32c1d7ba-29d6-466c-a2bc-56ff17ca4d1a` |
| Salto | Jump | `08872e23-d695-493d-8c03-cb595cd55e9c` |
| Caduta | Fall down | `2e513074-b11f-4108-9ba7-80f2995ecba8` |
| Atterraggio | Hard Landing | `241c8c51-4bfb-4f0c-ad0b-f9a7c50f8ee7` |
| Idle accovacciato | Crouching Idle | `7cd56394-0b47-4fa7-b951-6658d5ea3da6` |
| Camminata accovacciata | Crouch Walk Forward | `e4f5b9e6-381c-4227-88d4-94d6a0615df8` |

## Formato corretto

La libreria espone due famiglie di esportazione:

- **`mixamo-fbx`**: include mesh X Bot e scheletro con nomenclatura Mixamo. È il formato da usare per il retargeting DCC/Unity verso i rig umanoidi UnderGrind.
- **`splat-skeleton-fbx`**: scheletro MetaHuman a 441 ossa, senza mesh, pensato per personaggi PINOC con Splat Engine e rig Generic. Non è il formato da usare nel normale Animator Humanoid del Player.

Non confondere il formato di default riportato da PINOC (GLB scheletrico MetaHuman) con il formato `mixamo-fbx` disponibile per ciascuno degli otto asset. Per l'importazione nel progetto va richiesto esplicitamente `mixamo-fbx`.

## Destinazione e procedura

1. Recuperare un nuovo link firmato per ciascun asset tramite PINOC usando il relativo Asset ID e il formato `mixamo-fbx`. I link firmati scadono e non devono essere versionati.
2. Salvare i file in `Assets/UnderGrind/Animations`, usando nomi coerenti con le clip: `Standing_Idle.fbx`, `Walking.fbx`, `Running.fbx`, `Jump.fbx`, `Fall_down.fbx`, `Hard_Landing.fbx`, `Crouching_Idle.fbx`, `Crouch_Walk_Forward.fbx`.
3. In Unity, importare le clip con rig Humanoid e verificare che ogni Avatar di importazione sia valido. Il controller del Player deve usare il proprio Avatar per il retargeting.
4. Eseguire **UnderGrind → Player → Refresh Clips and Build Animator Controller**.
5. Verificare transizioni, loop, root motion, velocità percepita e allineamento dei piedi sul prefab maschile e femminile.

## Provenienza e diritti

Fonte: libreria curata PINOC, asset identificati dagli ID sopra. Il servizio descrive la libreria come gratuita da usare; la risposta dello strumento non espone un identificatore di licenza né chiarisce separatamente i diritti per distribuzione commerciale. Conservare provenienza e ID; verificare le condizioni del provider prima di una release commerciale.

## Stato iniziale — 20 novembre 2026

Al momento dell'acquisizione gli otto asset erano disponibili nella libreria PINOC ma non ancora presenti nel repository.

## Stato aggiornato — 24 novembre 2026

Gli otto FBX `mixamo-fbx` sono ora versionati in `Assets/UnderGrind/Animations`. I riferimenti alle otto clip e al controller sono assegnati in `Assets/UnderGrind/Settings/PlayerAnimationSettings.asset`. Ogni FBX usa un Avatar Humanoid sorgente; il Player usa il proprio Avatar maschile o femminile per il retargeting runtime. Non è stato eseguito un bake DCC delle curve sullo scheletro UnderGrind.

### Risultati Unity disponibili

La run Unity `20261124-110717-910` ha eseguito 55 test EditMode e 13 PlayMode: 65 superati e 3 falliti. I fallimenti registrati sono:
- `LocomotionClipsDoNotCrossFeetOnSourceAndPlayerAvatars`: Crouch Walk Forward, 6/232 incroci sulla sorgente, 10/232 sul Player maschile, 12/232 sul femminile.
- `VisualRigSwitchesExclusiveRendererGroupsForEachCameraMode`: aspettativa non coerente con gli stati iniziali dei renderer FP.
- `VisualRigPreservesInitiallyDisabledRenderers`: il rig riattivava un renderer disabilitato in origine.

Le due anomalie dei renderer sono corrette nel branch `fix/crouch-walk-animation-audit`: il rig conserva lo stato enabled originale, il builder ripristina i default prima di salvare i prefab e gli asset prefab mantengono i gruppi renderer in stato neutro. La correzione non è ancora stata verificata con una nuova esecuzione Unity.

### Ricerca di un sostituto per Crouch Walk

Otto candidati PINOC sono presenti in `Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk`, con importazione Humanoid e Avatar sorgente. L'audit Blender grezzo è in `Tools/Blender/pinoc-crouch-walk-candidate-audit.json`. Le varianti 05–08 non presentano incroci di piedi o ginocchia sullo scheletro FBX grezzo; tuttavia l'audit non riproduce la mappatura Avatar Humanoid di Unity e non può determinare da solo il candidato definitivo.

È stato aggiunto il test EditMode `CrouchWalkCandidatesAreAuditedOnSourceAndPlayerAvatars`. Confronta la clip corrente e gli otto candidati su sorgente, Avatar maschile e Avatar femminile, registra il mapping delle ossa e genera `TestResults/test/Unity/CrouchWalkCandidateAudit.txt`. Il test non è ancora stato eseguito.

Nessun candidato è stato assegnato al controller di produzione. La clip originale resta assegnata finché una variante non supera il confronto Unity su entrambi gli Avatar e la verifica visiva del ciclo.

### Limiti e diritti

La presenza di un asset nella libreria o nel progetto non certifica i diritti di distribuzione commerciale. Prima di una release commerciale verificare le condizioni d'uso del piano PINOC applicabile e conservare gli identificativi di provenienza riportati in questo documento.
