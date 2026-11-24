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

### Validazione

- **Walking** e **Running**: la run Unity del 23 novembre 2026 ha registrato zero incroci anatomici dei piedi su sorgente, Player maschile e Player femminile. Il risultato precede la riparazione dei pesi del 24 novembre e va ripetuto sul rig aggiornato.
- **Crouch Walk Forward**: la run Unity `20261124-113037-739` del 24 novembre 2026 ha confermato il difetto: 6/232 campioni con incrocio sulla sorgente, 10/232 sul Player maschile e 12/232 sul femminile. Il clip resta assegnato ma non è approvato.
- **Jump, Fall, Land, Idle e Crouch Idle**: importati e assegnati; non sono coperti dal test di incrocio dei piedi e non sono ancora validati visivamente sul rig riparato.

### Candidati Crouch Walk

La cartella `Candidates/PINOC_CrouchWalk` contiene otto varianti FBX importabili come Humanoid. Non sono assegnate al controller di produzione e non sostituiscono `Crouch_Walk_Forward.fbx`.

Il report `Tools/Blender/pinoc-crouch-walk-candidate-audit.json` misura la separazione anatomica sullo scheletro FBX grezzo. Le varianti 05–08 non mostrano incroci grezzi di piedi o ginocchia, ma l'audit Blender non equivale al retargeting Humanoid di Unity. Non è stato selezionato alcun sostituto.

Il test EditMode `CrouchWalkCandidatesAreAuditedOnSourceAndPlayerAvatars` confronta il clip corrente e gli otto candidati sulla sorgente, sull'Avatar maschile e sull'Avatar femminile. Scrive `TestResults/test/Unity/CrouchWalkCandidateAudit.txt`, includendo mapping delle ossa e separazione di piedi/ginocchia. Il test è presente in `main` dopo il merge della PR #12, ma non è ancora stato eseguito nell'Editor Unity.

Non assegnare un candidato al controller finché il report Unity non mostra un risultato accettabile su entrambi gli Avatar e la verifica visiva non conferma un ciclo continuo senza deformazioni.

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
