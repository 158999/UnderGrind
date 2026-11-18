# Player e locomozione umanoide

## Personaggio di riferimento

Il personaggio maschile è il riferimento dimensionale del Player e misura 1,78 m nel profilo iniziale. Il modello femminile presente nella sorgente misura 1,70 m. Entrambe le altezze sono configurabili.

La versione Blendstra corrente contiene due armature e sei mesh separate per personaggio: corpo, testa, braccia e mani dedicate alla prima e alla terza persona. La sorgente `.blend` nel repository non è stata sostituita automaticamente: l'exporter `Tools/Blender/export-undergrind-humans.py` valida i pesi e produce i due FBX configurati in `PlayerRigSettings.asset`.

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

I valori iniziali sono contenuti nell'asset e modificabili dall'Inspector. I modelli iniziali dei parametri di input, interazione, UI, animazione e rig sono file JSON in `Assets/UnderGrind/Settings/Defaults`; i builder li applicano soltanto quando creano il relativo ScriptableObject. Dopo la creazione, le modifiche all'asset Inspector non vengono sovrascritte dal builder. Se manca il file JSON necessario, il builder segnala l'errore e non crea un asset vuoto. La scena di test assegna lo stesso asset al controller e al dimensionamento iniziale del CharacterController.

## Input e controlli

`PlayerInputSettings.asset` collega gli identificatori logici del Player ai nomi delle azioni dell'`InputActionAsset`. L'asset iniziale è `Assets/UnderGrind/Settings/PlayerInputActions.inputactions`, importato da Unity e modificabile nell'Input Actions editor. **UnderGrind → Player → Create or Repair Input Actions** collega l'asset e ripara i riferimenti mancanti. Le binding predefinite sono dati dell'asset, non costanti del controller.

| Azione | Tastiera e mouse | Gamepad |
|---|---|---|
| Movimento | WASD / frecce | Stick sinistro |
| Visuale | Mouse | Stick destro |
| Corsa | Shift | Pressione stick sinistro |
| Salto | Spazio | Pulsante sud |
| Accovacciamento | C | Pulsante est |
| Cambio telecamera | F5 | Pulsante nord |
| Sblocco cursore | Esc | Start |
| Cattura cursore | Clic sinistro | — |
| Interazione/raccolta | E | Pulsante ovest |
| Rilascio oggetto | G | D-pad giù |
| Apri/chiudi inventario | Tab | Select |
| Slot successivo/precedente | ] / [ | D-pad destra/sinistra |

Le dead zone radiali di movimento e visuale gamepad, la sensibilità, il comportamento del cursore, il timeout di rimappatura e la chiave di persistenza sono configurati nei rispettivi asset `PlayerInputSettings.asset` e `PlayerLocomotionSettings.asset`. La stessa action map viene condivisa da locomozione, inventario e interazione.

`HumanLocomotionController` crea o riutilizza `PlayerInputRebindingController` e carica gli override prima di abilitare la action map. L'API consente di avviare una rimappatura con `BeginRebind(PlayerInputActionId, bindingIndex)`, annullarla con `CancelRebind()`, ottenere l'etichetta del controllo con `GetBindingDisplayString()` e ripristinare le binding originali con `ResetOverrides()`. Le rimappature vengono salvate in `PlayerPrefs`; `RebindTimeoutSeconds` e `OverrideStorageKey` sono configurabili in `PlayerInputSettings.asset`. Il componente espone l'evento `RebindCompleted`.

Il runtime supporta la rimappatura, ma non è ancora presente una schermata UI dedicata alle impostazioni dei controlli.

## Sovraccarico da inventario

Il componente `PlayerInventory` calcola il peso trasportato sommando il peso unitario di ogni oggetto per la relativa quantità. Il peso massimo è una soglia di penalità, non un limite alla raccolta.

Quando il peso corrente supera `MaxCarryWeightKg`:

- la corsa viene disabilitata;
- il salto viene disabilitato e l'eventuale input bufferizzato viene annullato;
- la velocità di camminata viene ridotta della percentuale configurata;
- la penalità si applica anche alla camminata accovacciata.

Quando il peso torna uguale o inferiore alla soglia, corsa, salto e velocità normale vengono ripristinati. Un salto già iniziato non viene interrotto a mezz'aria. I valori iniziali dell'asset inventario sono 24 slot, 50 kg e riduzione del 25%.

## Componenti di runtime

- `HumanLocomotionController`: movimento tramite `CharacterController`, input, accelerazione, decelerazione, salto, postura e gestione delle camere in prima e terza persona.
- `PlayerInventory`: slot, quantità, peso derivato, operazioni e stato di sovraccarico.
- `PlayerInventoryUI`: interfaccia overlay con slot, icone, quantità, selezione e peso. Layout e stile sono configurati in `PlayerInventoryUISettings.asset`.
- `PlayerInteractionController`: raccolta tramite SphereCast dalla camera e rilascio dello stack selezionato. Distanza, raggio, layer, quantità e impulso sono configurati in `PlayerInteractionSettings.asset`.
- `WorldItemPickup`: componente di un oggetto nel mondo; conserva l'eventuale quantità non trasferita all'inventario.
- `PlayerVisualRig`: visibilità di testa e corpo in prima persona, arti dedicati alla prima persona e arti dedicati alla terza persona.
- `PlayerAnimationDriver`: invia lo stato della locomozione all'Animator senza applicare root motion.

## Rig e visibilità del modello

`PlayerRigSettings.asset` contiene due percorsi FBX distinti, uno per il modello maschile e uno per quello femminile. Ogni file deve contenere il proprio scheletro e il proprio Avatar Humanoid: i due prefab non condividono lo stesso Avatar. I percorsi predefiniti sono:

- `Assets/UnderGrind/Art/Source/Rigged/Human_Male_Rigged.fbx`
- `Assets/UnderGrind/Art/Source/Rigged/Human_Female_Rigged.fbx`

Dopo aver importato entrambi i file, **UnderGrind → Player → Build Male and Female Prefabs From Configured FBX** prepara separatamente i due Model Importer, valida i rispettivi Avatar e genera i prefab. L'operazione si interrompe se manca uno dei file, se i percorsi coincidono, se l'Avatar non è valido o se la gerarchia non soddisfa i requisiti di renderer.

Convenzioni dei renderer:

- `*_Head`: testa, nascosta in prima persona e ripristinata in terza persona.
- `*_Body`: corpo, nascosto quando la visuale in prima persona guarda avanti e mostrato quando il giocatore guarda verso il basso.
- `*_TP_Arms` e `*_TP_Hands`: arti visibili soltanto in terza persona.
- `*_FP_Arms` e `*_FP_Hands`: arti visibili soltanto in prima persona.

`PlayerVisualRig` applica le convenzioni tramite `PlayerRigSettings`. Le soglie `FirstPersonBodyRevealPitch` e `FirstPersonBodyHidePitch` configurano la rivelazione del corpo con isteresi, evitando cambi continui vicino alla soglia. `RequireSeparateHead` e `RequireFirstPersonArms` rendono obbligatorie le rispettive parti durante la generazione del prefab.

Il generatore della scena richiede entrambi i prefab riggati, ciascuno con Animator, Avatar Humanoid valido, SkinnedMeshRenderer e gruppi renderer richiesti. Se i prefab mancano, avvia il builder configurato; se non riesce a generarli, interrompe la creazione della scena. Non crea prefab statici di ripiego.

## Animazioni

`PlayerAnimationSettings.asset` contiene i riferimenti alle clip, il Runtime Animator Controller, i nomi e i tipi dei parametri, le soglie dei blend, le durate di transizione e l'opzione di loop separata per ciascuna delle otto clip.

Dopo aver importato le clip in `Assets/UnderGrind/Animations`, il comando **UnderGrind → Player → Refresh Clips and Build Animator Controller** aggiorna i riferimenti mancanti e genera `Assets/UnderGrind/Generated/Animation/PlayerAnimator.controller`.

Le clip richieste sono Idle, Walk, Run, Jump, Fall, Land, Crouch Idle e Crouch Walk. Tutte e otto sono necessarie per generare il controller completo. Il generatore individua le clip sia dal nome interno sia dal nome del file FBX. Le opzioni di importazione sono configurabili in `PlayerAnimationSettings.asset`: attivazione della configurazione automatica, rig Humanoid o Generic, creazione dell'Avatar, importazione delle animazioni, materiali, camere, luci e ottimizzazione dei GameObject. Il profilo iniziale usa Humanoid, crea l'Avatar dal singolo FBX e importa le animazioni; le impostazioni vengono applicate soltanto ai file FBX nella cartella Animations. Le soglie di velocità e le transizioni sono lette dall'asset di configurazione. Il controller usa blend tree per locomozione e accovacciamento e stati separati per salto, caduta e atterraggio.

`PlayerAnimationDriver` aggiorna soltanto parametri presenti nel controller e con tipo corretto. Il root motion è disabilitato perché lo spostamento del personaggio è gestito da `HumanLocomotionController`.

Otto movimenti sono ora presenti nella libreria PINOC dell'utente: Standing Idle, Walking, Running, Jump, Crouching Idle, Crouch Walk Forward, Fall down e Hard Landing. Per il rig Humanoid UnderGrind è disponibile l'export `mixamo-fbx`, con X Bot e scheletro Mixamo, da retargettare sul rig definitivo. Il formato predefinito preso dalla libreria è invece un GLB armature-only MetaHuman a 441 ossa, destinato al workflow Splat e non direttamente compatibile con questo controller. Gli FBX Mixamo non sono ancora importati in `Assets/UnderGrind/Animations` né trasferiti sul rig definitivo.

## Scena di test

**UnderGrind → Build First Locomotion Test Scene** crea o aggiorna `Assets/UnderGrind/Scenes/LocomotionTest.unity`. Il generatore assegna gli asset di configurazione e aggiunge inventario, UI, interazione, locomozione e diagnostica. Se sono presenti i prefab Humanoid configurati, utilizza quelli; altrimenti importa le mesh sorgente da `humans.blend` e crea prefab statici di riferimento. Il comando chiede conferma prima di sostituire una scena già esistente e verifica il salvataggio delle scene aperte con modifiche non salvate.

### Stato della scena versionata

Il file `LocomotionTest.unity` attualmente versionato non corrisponde ancora alla configurazione prodotta dal generatore aggiornato. Il GameObject Player contiene un `CharacterController`, `HumanLocomotionController` e `LocomotionDebugOverlay`. Il controller ha assegnato `PlayerLocomotionSettings.asset`, ma il riferimento a `PlayerInputSettings` non è assegnato e il riferimento all'inventario è nullo. La scena non serializza `PlayerInventory`, `PlayerInventoryUI`, `PlayerInteractionController` o `PlayerAnimationDriver`.

La visuale è composta da parti mesh primitive separate (testa, torso e arti), non da un modello Humanoid animabile. La scena non contiene uno `SkinnedMeshRenderer` né un `Animator`. Il controller può aggiungere alcuni componenti di fallback durante `Awake`, ma senza gli asset di input, animazione e rig non ripristina la configurazione completa.

Di conseguenza, la scena versionata è un prototipo visivo precedente e non costituisce una verifica del Player integrato attuale. Il generatore contiene già la configurazione aggiornata, ma la scena deve essere ricreata nell'Editor Unity e verificata in Play Mode prima di poter essere considerata operativa.

## Stato delle verifiche

Risultati effettivamente presenti nel repository:

- Test autonomi C#: 6/6 superati.
- Unity EditMode: 12/12 superati nel report \`TestResults/UnderGrind/EditMode_20261118_103737.xml\`. Il report precede le nuove prove di input e i test aggiunti successivamente all'inventario.
- Unity PlayMode: 7/7 superati nel report \`TestResults/UnderGrind/PlayMode_20261118_092745.xml\`. Questo report precede i test aggiunti successivamente per sovraccarico, raccolta, rilascio e UI.
- La suite presente nel codice contiene attualmente 28 test EditMode e 13 test PlayMode. Il conteggio è ricavato dai metodi NUnit presenti nei sei file di test; non è un risultato di esecuzione.
- La successiva esecuzione batch ha prodotto un report EditMode valido da 12 test, ma il processo PlayMode è terminato con codice 1 prima di produrre un report valido. Il JSON aggregato è marcato Failed e non va interpretato come zero test effettivi.
- Il runner è stato aggiornato per accettare l'XML \`test-suite\` prodotto da \`TestRunner API\`, selezionare solo report creati durante l'esecuzione corrente e rifiutare report obsoleti. Il runner aggiornato e le suite complete non sono ancora stati eseguiti nell'Editor Unity.
- La compilazione delle assembly runtime e di test è riuscita nel log EditMode del 18 novembre. Non è una prova di esecuzione PlayMode o di correttezza visiva.
- Verifica visiva del modello riggato e della scena: non documentata.

La verifica finale richiede una nuova esecuzione dei due script PowerShell di test sul commit aggiornato e un controllo visivo in Unity.
