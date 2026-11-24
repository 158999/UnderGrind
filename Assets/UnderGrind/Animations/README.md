# Sorgenti delle animazioni del Player

Questa cartella contiene i file sorgente delle animazioni importate e i relativi file `.meta` generati da Unity.

## Nomi delle clip riconosciuti

Il generatore della scena di test effettua una ricerca senza distinzione tra maiuscole e minuscole. I nomi previsti sono:

- Idle / Standing Idle
- Walk / Walking
- Run / Running
- Jump
- Fall / Fall down
- Land / Hard Landing
- Crouch Idle / Crouching Idle
- Crouch Walk

## Formati di esportazione PINOC

PINOC mette a disposizione due famiglie di file FBX, che non devono essere confuse:

- `splat-skeleton-fbx`: scheletro MetaHuman di 441 ossa, comprese quelle facciali, senza mesh. È destinato ai personaggi PINOC utilizzati con Splat Engine in Unity tramite `com.viggle.splat-engine` dalla versione 0.2.2. L'importazione richiede il rig Generic e l'Avatar Definition impostata su No Avatar.
- `mixamo-fbx`: mesh X Bot e scheletro con nomi Mixamo. È destinato agli strumenti DCC e al retargeting su altri scheletri umanoidi. È il formato candidato per trasferire le animazioni sul corpo UnderGrind.

La mesh X Bot serve come sorgente per il retargeting: non è il modello del giocatore e non deve sostituirlo.

## Stato dell'importazione

Gli otto movimenti di produzione sono già versionati in questa cartella e assegnati a `Assets/UnderGrind/Settings/PlayerAnimationSettings.asset`. L'asset assegna anche il controller Animator generato. Gli FBX sono importati come Humanoid con Avatar sorgente separato; Unity esegue il retargeting runtime sull'Avatar del Player maschile o femminile. Le curve non sono state ribake-ate sullo scheletro UnderGrind.

`PlayerAnimationControllerBuilder` individua i file FBX, applica le impostazioni Humanoid e aggiorna i riferimenti alle clip mancanti. Le sei clip Idle, Walk, Run, Jump, Crouch Idle e Crouch Walk sono obbligatorie; Fall e Land sono opzionali. Il loop di ogni clip è configurabile separatamente nell'asset `PlayerAnimationSettings.asset`.

### Validazione — run Unity `20261124-114001-048`

- **Walking** e **Running**: il test `LocomotionClipsDoNotCrossFeetOnSourceAndPlayerAvatars` non ha segnalato incroci anatomici nella run corrente sui tre Avatar campionati. Running sul Player femminile ha però una separazione minima di soli 0,004 m: il test passa, ma la verifica visiva sul rig aggiornato resta importante.
- **Crouch Walk Forward**: la clip ancora assegnata in `PlayerAnimationSettings.asset` continua a fallire: 6/232 campioni sulla sorgente Mixamo, 10/232 sul Player maschile e 12/232 sul femminile. Separazione minima anatomica: -0,114 m, -0,119 m e -0,133 m rispettivamente. Non è approvata.
- **Visual Rig**: i quattro casi FBX/prefab del test `VisualRigSwitchesExclusiveRendererGroupsForEachCameraMode` passano; il problema precedente era l'aspettativa errata sui FBX sorgente, non un componente mancante nei prefab.
- **Jump, Fall, Land, Idle e Crouch Idle**: importati e assegnati; non sono coperti dal test di incrocio dei piedi e non sono ancora validati visivamente sul rig riparato.

### Candidati Crouch Walk

La cartella `Candidates/PINOC_CrouchWalk` contiene otto varianti FBX importabili come Humanoid. Nessuna è assegnata al controller di produzione e nessuna sostituisce `Crouch_Walk_Forward.fbx`.

Il test EditMode `CrouchWalkCandidatesAreAuditedOnSourceAndPlayerAvatars` è stato eseguito anche nella run `20261124-114001-048`. Il candidato 08 resta il migliore nel retargeting misurato da Unity: zero incroci di piedi e ginocchia su sorgente, Player maschile e Player femminile; separazione minima dei piedi 0,254 m sulla sorgente, 0,183 m sul Player maschile e 0,155 m sul femminile; errore di chiusura del loop pari a 0 m. Il candidato 07 è anch'esso senza incroci, con separazione minima dei piedi sul Player femminile di 0,103 m e separazione minima delle ginocchia maggiore rispetto al candidato 08.

Questi dati sono una classifica diagnostica automatica, non una verifica visiva. Il candidato 08 non è stato promosso e il controller continua a usare la clip originale. Prima dell'assegnazione occorre verificare in Play Mode il ciclo continuo, le deformazioni e le transizioni su entrambi gli Avatar.

`LocomotionTestSceneBuilder` non cerca clip dentro `humans.blend`: usa l'eventuale Avatar presente nel file soltanto come fallback per il modello statico. Il controller di animazione viene assegnato tramite `PlayerAnimationSettings.asset`.

## Provenienza dei movimenti selezionati

I seguenti asset della libreria PINOC sono stati selezionati e acquisiti senza consumo di crediti:

| Movimento | Identificativo asset |
|---|---|
| Standing Idle | `1cbf45b5-5abe-4a88-97cd-a1a21da60d7b` |
| Walking | `32d71c7f-e88a-4d1f-9c0c-1fde2da5f060` |
| Running | `32c1d7ba-29d6-466c-a2bc-56ff17ca4d1a` |
| Jump | `08872e23-d695-493d-8c03-cb595cd55e9c` |
| Crouching Idle | `7cd56394-0b47-4fa7-b951-6658d5ea3da6` |
| Crouch Walk Forward | `e4f5b9e6-381c-4227-88d4-94d6a0615df8` |
| Fall down | `2e513074-b11f-4108-9ba7-80f2995ecba8` |
| Hard Landing | `241c8c51-4bfb-4f0c-ad0b-f9a7c50f8ee7` |

Prima di distribuire o includere nel prodotto gli export scaricati, verificare le condizioni d'uso applicabili del fornitore.
