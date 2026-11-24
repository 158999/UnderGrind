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

`PlayerVisualRig` gestisce gruppi distinti per testa, corpo, arti FP e arti TP. In prima persona nasconde la testa, usa i renderer FP e mostra il corpo quando il pitch supera la soglia configurata; in terza persona mostra testa, corpo e renderer TP. La correzione integrata in `main` con la PR #12 conserva lo stato `Renderer.enabled` originale e fa salvare i prefab con valori neutri prima dell'applicazione della modalità camera. I prefab versionati sono stati aggiornati nello stesso branch. Il nuovo comportamento non è ancora stato confermato da una nuova esecuzione Unity.

La registrazione del 23 novembre mostra ancora geometria/arti troppo vicini alla camera in prima persona. La selezione corretta dei gruppi renderer non certifica la posizione degli arti rispetto alla camera né elimina eventuali intersezioni; serve ancora una verifica visiva in Play Mode dopo la correzione dei pesi.

### Animazioni

Gli otto FBX `mixamo-fbx` sono presenti in `Assets/UnderGrind/Animations` e le relative clip sono assegnate in `PlayerAnimationSettings.asset`, insieme al controller Animator. Ogni FBX usa un Avatar Humanoid sorgente; Unity esegue il retargeting runtime sull'Avatar del Player. Le curve non sono state ribake-ate sullo scheletro UnderGrind.

Il test Unity del 23 novembre ha rilevato zero incroci anatomici per Walking e Running su sorgente e sui due Player. Per `Crouch_Walk_Forward`, la run del 24 novembre ha rilevato 6/232 incroci sulla sorgente, 10/232 sul Player maschile e 12/232 sul femminile. La clip resta assegnata ma non è approvata.

La cartella `Assets/UnderGrind/Animations/Candidates/PINOC_CrouchWalk` contiene otto candidati non assegnati al controller. Il report Blender `Tools/Blender/pinoc-crouch-walk-candidate-audit.json` valuta lo scheletro FBX grezzo e non equivale al retargeting Humanoid Unity. Il test EditMode `CrouchWalkCandidatesAreAuditedOnSourceAndPlayerAvatars` confronta il clip corrente e gli otto candidati su Avatar sorgente, maschile e femminile e scrive `TestResults/test/Unity/CrouchWalkCandidateAudit.txt`. È stato aggiunto nel branch ma non è ancora stato eseguito.

Non sostituire il clip di produzione finché una variante non mostra risultati accettabili su entrambi gli Avatar e supera la verifica visiva del ciclo.

## Graybox

`PlayerGrayboxSettings.asset` configura pavimento, marcatori di distanza, gradini, scala, pareti, rampe, tunnel, passaggio stretto, slalom, ostacoli e piattaforme. La scena completa `LocomotionTest.unity` richiede entrambi i prefab Humanoid validi. Per provare subito movimento, collisioni e camera prima dell'importazione dei modelli, il menu `UnderGrind → Build Graybox Locomotion Test (Temporary Proxy)` crea `LocomotionGraybox.unity` con una capsula esplicitamente temporanea: non è il personaggio definitivo e non sostituisce i prefab maschile/femminile.

## Risultati dei test e conservazione

Gli script PowerShell `Tools/Testing/run-tests.ps1` e `Tools/Testing/run-unity-tests.ps1` salvano i report in `TestResults/test`. La cartella `TestResults/test old` conserva l'esecuzione immediatamente precedente. A ogni nuova esecuzione la precedente cartella `test old` viene eliminata, `test` diventa `test old` e viene creata una nuova `test`. Il comando `Tools/Testing/run-all-tests.ps1` esegue entrambe le suite nella stessa cartella, effettuando una sola rotazione.

Ogni suite produce un JSON riepilogativo, un TXT, i log e i report XML/TRX disponibili. Un report assente, non valido o con zero test non viene considerato superato. I log Unity riportano separatamente gli errori di compilazione.

### Ultima esecuzione disponibile nel repository

L'ultima esecuzione Unity versionata è la run `20261124-110717-910` su Unity **6000.6.9f1**:

- Test autonomi C#: **6/6 superati**.
- Unity EditMode: **53/55 superati**, 2 falliti.
- Unity PlayMode: **12/13 superati**, 1 fallito.
- Totale Unity: **65/68 superati**, 3 falliti.

I tre fallimenti registrati sono:

1. `LocomotionClipsDoNotCrossFeetOnSourceAndPlayerAvatars`: Crouch Walk Forward, 6 incroci sulla sorgente, 10 sul Player maschile, 12 sul femminile.
2. `VisualRigSwitchesExclusiveRendererGroupsForEachCameraMode`: aspettativa di stato FP non coerente con l'asset FBX/prefab.
3. `VisualRigPreservesInitiallyDisabledRenderers`: il rig riattivava un renderer disabilitato in origine.

I due problemi dei renderer sono corretti in `main` tramite PR #12: il baseline conserva lo stato enabled originale; il builder ripristina i default prima di salvare il prefab; gli FBX e i prefab versionati mantengono i gruppi renderer in stato neutro. La correzione integrata in `main` deve ancora essere verificata eseguendo nuovamente EditMode e PlayMode.

Il fallimento Crouch Walk resta aperto. I candidati PINOC sono stati sottoposti ad audit Blender grezzo, ma la selezione deve essere fatta con il test Unity di retargeting aggiunto nel branch. Nessuna variante è stata promossa al controller.

L'assenza di un nuovo report Unity dopo le correzioni non equivale a test superati. La verifica di importazione, compilazione, rendering, EditMode e PlayMode deve essere eseguita in Unity 6000.6.9f1.

## Stato aggiornato della pipeline

La riparazione anatomica dei pesi è entrata in `main` il 24 novembre 2026. I nuovi FBX e prefab sono versionati; il blend sorgente originale resta preservato e il derivato reweighted è in `Tools/Blender/Generated/humans_reweighted.blend`.

**Verificato in Blender:** 12 mesh controllate, massimo quattro influenze per vertice, pesi normalizzati, nessuna influenza anatomica vietata, nessun vertice non pesato e 54 pose sintetiche valutate per personaggio.

**Verificato nell'ultima run Unity del 24 novembre:** 65/68 test superati. Restano tre fallimenti, due dei quali sono corretti nel branch `fix/crouch-walk-animation-audit` ma non ancora rieseguiti. Il fallimento Crouch Walk resta aperto.

**Non ancora verificato dopo le correzioni:** import e resa dei prefab aggiornati, EditMode e PlayMode sul nuovo baseline renderer, posizione degli arti FP rispetto alla camera, deformazione visiva in prima e terza persona e retargeting definitivo di Crouch Walk.

La documentazione di dettaglio del rig è in `Documentation/Humanoid-Rig-Pipeline.md`; provenienza e stato delle clip sono in `Documentation/Player-Animation-Asset-Intake-2026-11-20.md` e `Assets/UnderGrind/Animations/README.md`.
