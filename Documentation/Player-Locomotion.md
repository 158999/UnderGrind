# Player e locomozione umanoide

## Personaggio di riferimento

Il personaggio maschile è il riferimento dimensionale del Player e misura 1,78 m nel profilo iniziale; il modello femminile misura 1,70 m. Le due altezze sono configurate in `PlayerLocomotionSettings.asset` e sono usate dal controller e dal builder dei prefab.

### Sorgente, rig derivato ed export

`Assets/UnderGrind/Art/Source/humans.blend` resta la sorgente originale versionata e non viene sovrascritta dalla riparazione automatica. Il workflow Blender crea `Tools/Blender/Generated/humans_reweighted.blend` come derivato e da questo esporta:

- `Assets/UnderGrind/Art/Source/Rigged/Human_Male_Rigged.fbx`
- `Assets/UnderGrind/Art/Source/Rigged/Human_Female_Rigged.fbx`

I prefab generati sono `Assets/UnderGrind/Generated/Prefabs/Human_Male_Rigged.prefab` e `Human_Female_Rigged.prefab`. Ogni personaggio contiene sei gruppi mesh: Body, Head, TP Arms, TP Hands, FP Arms e FP Hands.

Il report `Tools/Blender/weight-repair-report.json` registra 12 mesh, zero vertici non pesati, zero influenze oltre il limite di quattro, zero somme non normalizzate e zero assegnazioni a gruppi anatomici vietati. Sono stati usati 14 fallback su vertici femminili (10 testa, 4 mani). Il report `Tools/Blender/player-skinning-deformation-audit.json` valuta 54 pose sintetiche per personaggio; non sostituisce la verifica delle animazioni e del rendering in Unity.

## Configurazione della locomozione

Tutti i parametri di movimento, postura, collisione e visuale sono contenuti nell'asset `Assets/UnderGrind/Settings/PlayerLocomotionSettings.asset`, di tipo `PlayerLocomotionSettings`. Il componente `HumanLocomotionController` conserva il riferimento all'asset e non duplica i valori di bilanciamento.

Le proprietà sono organizzate in otto gruppi:

1. **Velocità di camminata**: velocità base.
2. **Velocità di corsa**: moltiplicatore rispetto alla camminata; la velocità effettiva è derivata.
3. **Salto**: altezza teorica e velocità iniziale, calcolata da altezza e gravità.
4. **Gravità e comportamento in aria**: gravità, controllo aereo, velocità di aderenza al terreno e soglia tra salita e discesa.
5. **Accelerazione e decelerazione**: variazione della velocità orizzontale e soglie di input.
6. **Accovacciamento e collisioni**: altezza, velocità accovacciata, dimensioni della capsula, verifica dello spazio per rialzarsi, layer, step offset, pendenza, skin width e opzioni del CharacterController.
7. **Camera e sensibilità**: sensibilità mouse/gamepad, angolo massimo, altezza del punto di vista, offset e distanza in terza persona, collisioni, interpolazione, FOV e near clip.
8. **Coyote time e jump buffer**: finestre temporali indipendenti per il salto.

I valori iniziali sono contenuti nell'asset e modificabili dall'Inspector: altezza maschile 1,78 m, altezza femminile 1,70 m, camminata 1,40 m/s, corsa 1,8667 m/s, salto 0,40 m e gravità 9,81 m/s². Il parametro Animator `runThreshold` iniziale è 1,65 m/s, compreso tra velocità di camminata e corsa; il precedente valore di 2 m/s rendeva irraggiungibile il blend di corsa. I modelli iniziali dei parametri di input, interazione, UI, animazione, rig e graybox sono file JSON in `Assets/UnderGrind/Settings/Defaults`; i builder li applicano quando creano il relativo ScriptableObject. Le migrazioni versionate compilano i nuovi campi senza sovrascrivere le modifiche successive effettuate nell'Inspector.

## Input e controlli

`PlayerInputSettings.asset` collega gli identificatori logici del Player ai nomi delle azioni dell'`InputActionAsset`. L'asset iniziale è `Assets/UnderGrind/Settings/PlayerInputActions.inputactions`, modificabile nell'Input Actions editor. Le binding sono dati dell'asset e non costanti del controller.

| Azione | Tastiera e mouse | Gamepad |
|---|---|---|
| Movimento | WASD / frecce | Stick sinistro |
| Visuale | Mouse | Stick destro |
| Corsa | Shift | Pressione stick sinistro |
| Salto | Spazio | Pulsante sud |
| Accovacciamento | C | Pulsante est |
| Cambio telecamera | F5 | Pulsante nord |
| Sblocco cursore | Esc | Start |
| Interazione/raccolta | E | Pulsante ovest |
| Rilascio oggetto | G | D-pad giù |
| Apri/chiudi inventario | Tab | Select |
| Slot successivo/precedente | ] / [ | D-pad destra/sinistra |

Le dead zone radiali, la sensibilità, il comportamento del cursore, il timeout di rimappatura e la chiave di persistenza sono configurati nei rispettivi asset. `PlayerInputRebindingController` supporta rimappatura interattiva, annullamento, visualizzazione della binding, salvataggio e reset degli override. La schermata UI dedicata alla rimappatura non è ancora presente.

## Sovraccarico e inventario

`PlayerInventory` calcola il peso trasportato sommando peso unitario e quantità. La soglia di peso non limita la raccolta. Quando il peso supera la soglia configurata, corsa e salto vengono disabilitati e la velocità di camminata viene ridotta della percentuale definita nell'asset; la penalità si applica anche alla camminata accovacciata. Un salto già iniziato non viene interrotto. Quando il peso torna uguale o inferiore alla soglia, le capacità vengono ripristinate.

`PlayerInventoryUI` visualizza slot, icone, quantità, selezione e peso. `PlayerInteractionController` raccoglie oggetti tramite SphereCast dalla camera e rilascia lo stack selezionato. `WorldItemPickup` conserva la quantità che non trova spazio nell'inventario. Salvataggio/caricamento, equipaggiamento e uso degli oggetti non sono ancora implementati.

## Rig, visuale e animazioni

`PlayerRigSettings.asset` contiene i percorsi FBX maschile/femminile, le radici degli scheletri, i requisiti Avatar Humanoid, il mapping dei renderer e la variante predefinita della scena di test. Il builder valida entrambi i personaggi e interrompe la generazione se un Avatar o un gruppo renderer richiesto manca.

`PlayerVisualRig` gestisce gruppi distinti per testa, corpo, arti FP e arti TP. In prima persona nasconde la testa, usa i renderer FP e mostra il corpo quando il pitch supera la soglia configurata; in terza persona mostra testa, corpo e renderer TP. `PlayerVisualRig` conserva lo stato `Renderer.enabled` originale e il builder salva i prefab con i valori neutri prima dell'applicazione della modalità camera. La run Unity `20261124-114001-048` ha verificato il test sui casi FBX maschile/femminile e sui prefab maschile/femminile: tutti e quattro passano.

La registrazione del 23 novembre mostra ancora geometria/arti troppo vicini alla camera in prima persona. La selezione corretta dei gruppi renderer non certifica la posizione degli arti rispetto alla camera né elimina eventuali intersezioni; serve ancora una verifica visiva in Play Mode dopo la correzione dei pesi.

### Animazioni

Gli otto FBX `mixamo-fbx` sono presenti in `Assets/UnderGrind/Animations`. Le clip sono assegnate in `PlayerAnimationSettings.asset` e il controller Animator usa il retargeting Humanoid runtime: ogni sorgente ha un proprio Avatar e Unity valuta le curve sugli Avatar Player maschile e femminile.

Nella run Unity `20261124-114001-048`, Walking e Running non hanno prodotto incroci anatomici nel controllo dei piedi. Running sul Player femminile arriva però a una separazione minima di 0,004 m, quindi il passaggio automatico non sostituisce la verifica visiva. `Crouch_Walk_Forward.fbx`, sorgente originale ora non assegnata, resta invece difettosa: 6/232 incroci sulla sorgente Mixamo, 10/232 sul Player maschile e 12/232 sul femminile; separazione minima anatomica rispettivamente -0,114 m, -0,119 m e -0,133 m.

Il test `CrouchWalkCandidatesAreAuditedOnSourceAndPlayerAvatars` ha confrontato gli otto candidati sul rig sorgente e sui due Avatar Player. `Crouch_Walk_Candidate_08.fbx` è il migliore risultato diagnostico: zero incroci di piedi e ginocchia sui tre rig, separazione minima dei piedi 0,183 m sul Player maschile e 0,155 m sul femminile, errore di loop pari a 0 m. Il candidato 07 è anch'esso senza incroci, ma la separazione minima dei piedi sul Player femminile scende a 0,103 m. Il 24 novembre 2026 `Crouch_Walk_Candidate_08.fbx` è stato assegnato provvisoriamente tramite `PlayerAnimationSettings.asset`: è il primo candidato nella classifica Unity e non presenta incroci di piedi o ginocchia sui tre Avatar. Il report ultra dettagliato segnala tuttavia un errore di loop di rotazione di circa 15,3°, un errore di posizione di 8,6 mm e un contatto stimato basso (circa 4–5% dei campioni). La qualità visiva del ciclo, le deformazioni e le transizioni devono ancora essere verificate in Play Mode su entrambi gli Avatar. Per confrontare in sicurezza la clip originale e i candidati sui prefab Player senza alterare gli asset di produzione, è disponibile la finestra [Anteprima dei candidati Crouch Walk](Crouch-Walk-Candidate-Preview.md), che riproduce direttamente le clip sui due Avatar in un'istanza temporanea. La finestra non esegue controller, transizioni o foot-separation IK: la verifica finale nel controller reale resta da eseguire in Play Mode. La stessa finestra consente di esportare un pacchetto ZIP di ispezione ultra dettagliato per la clip selezionata o per l'originale e tutti gli otto candidati, su entrambi gli Avatar Player. Il pacchetto include contact sheet a quattro viste, campionamento delle pose Humanoid e di ogni Transform della gerarchia (anche non Humanoid), trasformazioni e derivate cinematiche, curve/keyframe, eventi, impostazioni d'importazione, mapping Avatar, diagnostica delle mesh skinned e metriche di piedi, ginocchia, bacino e loop. I dati e le immagini sono generati su istanze temporanee nel Preview Scene e non modificano gli asset di produzione. I limiti e le euristiche del report sono descritti in `Documentation/Crouch-Walk-Candidate-Preview.md`.

## Graybox

`PlayerGrayboxSettings.asset` configura pavimento, marcatori di distanza, gradini, scala, pareti, rampe, tunnel, passaggio stretto, slalom, ostacoli e piattaforme. La scena completa `LocomotionTest.unity` richiede entrambi i prefab Humanoid validi. Per provare subito movimento, collisioni e camera prima dell'importazione dei modelli, il menu `UnderGrind → Build Graybox Locomotion Test (Temporary Proxy)` crea `LocomotionGraybox.unity` con una capsula esplicitamente temporanea: non è il personaggio definitivo e non sostituisce i prefab maschile/femminile.

## Risultati dei test e conservazione

Gli script PowerShell `Tools/Testing/run-tests.ps1` e `Tools/Testing/run-unity-tests.ps1` salvano i report in `TestResults/test`. La cartella `TestResults/test old` conserva l'esecuzione immediatamente precedente. A ogni nuova esecuzione la precedente cartella `test old` viene eliminata, `test` diventa `test old` e viene creata una nuova `test`. Il comando `Tools/Testing/run-all-tests.ps1` esegue entrambe le suite nella stessa cartella, effettuando una sola rotazione.

Ogni suite produce un JSON riepilogativo, un TXT, i log e i report XML/TRX disponibili. Un report assente, non valido o con zero test non viene considerato superato. I log Unity riportano separatamente gli errori di compilazione.

### Ultima esecuzione disponibile nel repository

L'ultima esecuzione completa versionata è la run Unity `20261124-114001-048` su Unity **6000.6.9f1**:

- Test autonomi C#: **6/6 superati**.
- Unity EditMode: **56/57 superati**, 1 fallito.
- Unity PlayMode: **13/13 superati**.
- Totale Unity: **69/70 superati**, 1 fallito.

L'unico fallimento è `LocomotionClipsDoNotCrossFeetOnSourceAndPlayerAvatars`, limitato alla clip `Crouch_Walk_Forward.fbx`: 6/232 incroci sulla sorgente, 10/232 sul Player maschile e 12/232 sul femminile. I due precedenti fallimenti Visual Rig non si riproducono: i casi FBX e prefab passano dopo la correzione del test che distingueva sorgenti importate e prefab runtime.

Non risultano errori di compilazione Unity nella run. Il report PlayMode è valido e completo (13/13); il report EditMode è valido ma fallisce per l'asserzione sulla clip. La verifica visiva del movimento non è eseguita dal runner.

## Stato aggiornato della pipeline

La riparazione anatomica dei pesi è entrata in `main` il 24 novembre 2026. I nuovi FBX e prefab sono versionati; il blend sorgente originale resta preservato e il derivato reweighted è in `Tools/Blender/Generated/humans_reweighted.blend`.

**Verificato in Blender:** 12 mesh controllate, massimo quattro influenze per vertice, pesi normalizzati, nessuna influenza anatomica vietata, nessun vertice non pesato e 54 pose sintetiche valutate per personaggio.

**Verificato nell'ultima run Unity `20261124-114001-048`:** 69/70 test Unity superati; EditMode 56/57, PlayMode 13/13. I test standalone sono 6/6. I due precedenti casi Visual Rig ora passano. La clip originale Crouch Walk resta difettosa ma non è più assegnata; il candidato 08 è ora configurato provvisoriamente e attende la verifica visiva in Play Mode.

**Non ancora verificato:** qualità visiva delle deformazioni in prima e terza persona, posizione degli arti FP rispetto alla camera, continuità visiva dei cicli e promozione del candidato Crouch Walk. La run automatica non sostituisce il controllo visivo in Unity.

La documentazione di dettaglio del rig è in `Documentation/Humanoid-Rig-Pipeline.md`; provenienza e stato delle clip sono in `Documentation/Player-Animation-Asset-Intake-2026-11-20.md` e `Assets/UnderGrind/Animations/README.md`.
