# Player e locomozione umanoide

## Personaggio di riferimento

Il personaggio maschile è il riferimento dimensionale del Player e misura 1,78 m nel profilo iniziale. Il modello femminile presente nella sorgente misura 1,70 m. Entrambe le altezze sono configurabili.

La sorgente Blender ispezionata contiene una mesh statica completa per personaggio, senza scheletro, pesi di deformazione o testa separata. Le sorgenti originali non vengono modificate: rig, pesi e parti derivate devono essere prodotti in prefab separati.

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

I valori iniziali sono contenuti nell'asset e modificabili dall'Inspector. Se manca l'asset, il controller segnala l'errore e non applica il movimento. La scena di test assegna lo stesso asset al controller e al dimensionamento iniziale del CharacterController.

## Input e controlli

`PlayerInputSettings` collega il Player a un `InputActionAsset`. L'asset iniziale viene creato da **UnderGrind → Player → Create or Repair Input Actions** e salvato in `Assets/UnderGrind/Settings/PlayerInputActions.asset`; le associazioni tra identificatori logici e nomi delle azioni sono in `PlayerInputSettings.asset`. Le singole binding di tastiera, mouse e gamepad sono modificabili direttamente nell'Input Actions asset.

| Azione | Tastiera e mouse | Gamepad |
|---|---|---|
| Movimento | WASD / frecce | Stick sinistro |
| Visuale | Mouse | Stick destro |
| Corsa | Shift | Pressione stick sinistro |
| Salto | Spazio | Pulsante sud |
| Accovacciamento | C | Pulsante est |
| Cambio telecamera | F5 | Pulsante nord |
| Sblocco cursore | Esc | Start |
| Blocca nuovamente il cursore | Clic sinistro | — |
| Interazione/raccolta | E | Pulsante ovest |
| Rilascio oggetto | G | D-pad giù |
| Apri/chiudi inventario | Tab | Select |
| Slot successivo/precedente | ] / [ | D-pad destra/sinistra |

Le azioni vengono lette dalla stessa action map. Il cursore viene bloccato all'avvio se l'opzione è attiva e può essere liberato o catturato nuovamente tramite le azioni configurate.

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
- `PlayerVisualRig`: visibilità dei renderer di testa e arti dedicati alla prima persona.
- `PlayerAnimationDriver`: invia lo stato della locomozione all'Animator senza applicare root motion.

## Rig e visibilità del modello

`PlayerRigSettings.asset` contiene i nomi delle radici degli scheletri, i nomi dei prefab, i suffissi dei renderer e le opzioni di importazione FBX.

Dopo aver selezionato un FBX riggato, **UnderGrind → Player → Build Prefabs From Selected Rigged FBX** imposta l'importazione Humanoid e genera i prefab maschile e femminile. Il file selezionato deve contenere entrambe le radici configurate e un Avatar Humanoid valido. Il generatore richiede una testa con renderer separato se `RequireSeparateHead` è attivo.

Convenzioni dei renderer:

- `*_Head`: testa, nascosta in prima persona e ripristinata in terza persona.
- `*_FP_Arms`: braccia dedicate alla prima persona.
- `*_FP_Hands`: mani dedicate alla prima persona.

Le associazioni esplicite hanno priorità sulla ricerca per nome. I renderer non classificati mantengono lo stato di abilitazione originale. Se il prefab riggato è disponibile, la scena di test lo preferisce al modello statico importato da `humans.blend`. In assenza del prefab riggato, il Player può muoversi ma la sorgente statica non fornisce uno scheletro animabile né una testa indipendente.

La sorgente attualmente ispezionata non dimostra ancora una separazione della testa, pesi di deformazione o parti dedicate alle braccia e alle mani in prima persona. I test su gerarchie artificiali verificano la logica di visibilità, non il modello finale.

## Animazioni

`PlayerAnimationSettings.asset` contiene i riferimenti alle clip, il Runtime Animator Controller, i nomi e i tipi dei parametri, le soglie dei blend e le durate di transizione.

Dopo aver importato le clip in `Assets/UnderGrind/Animations`, il comando **UnderGrind → Player → Refresh Clips and Build Animator Controller** aggiorna i riferimenti mancanti e genera `Assets/UnderGrind/Generated/Animation/PlayerAnimator.controller`.

Le clip richieste sono Idle, Walk, Run, Jump, Crouch Idle e Crouch Walk. Fall e Land sono opzionali. Le soglie di velocità e le transizioni sono lette dall'asset di configurazione. Il controller usa blend tree per locomozione e accovacciamento e stati separati per salto, caduta e atterraggio quando le clip sono disponibili.

`PlayerAnimationDriver` aggiorna soltanto parametri presenti nel controller e con tipo corretto. Il root motion è disabilitato perché lo spostamento del personaggio è gestito da `HumanLocomotionController`.

Otto movimenti sono ora presenti nella libreria PINOC dell'utente: Standing Idle, Walking, Running, Jump, Crouching Idle, Crouch Walk Forward, Fall down e Hard Landing. Per il rig Humanoid UnderGrind è disponibile l'export `mixamo-fbx`, con X Bot e scheletro Mixamo, da retargettare sul rig definitivo. Il formato predefinito preso dalla libreria è invece un GLB armature-only MetaHuman a 441 ossa, destinato al workflow Splat e non direttamente compatibile con questo controller. Gli FBX Mixamo non sono ancora importati in `Assets/UnderGrind/Animations` né trasferiti sul rig definitivo.

## Scena di test

**UnderGrind → Build First Locomotion Test Scene** crea o aggiorna `Assets/UnderGrind/Scenes/LocomotionTest.unity`. Il generatore assegna gli asset di configurazione e aggiunge inventario, UI, interazione, locomozione e diagnostica. Se sono presenti i prefab Humanoid configurati, utilizza quelli; altrimenti importa le mesh sorgente da `humans.blend` e crea prefab statici di riferimento. Il comando chiede conferma prima di sostituire una scena già esistente e verifica il salvataggio delle scene aperte con modifiche non salvate.

### Stato della scena versionata

Il file `LocomotionTest.unity` attualmente versionato non corrisponde ancora alla configurazione prodotta dal generatore aggiornato. Il GameObject Player contiene un `CharacterController`, `HumanLocomotionController` e `LocomotionDebugOverlay`. Il controller ha assegnato `PlayerLocomotionSettings.asset`, ma il riferimento a `PlayerInputSettings` non è assegnato e il riferimento all'inventario è nullo. La scena non serializza `PlayerInventory`, `PlayerInventoryUI`, `PlayerInteractionController` o `PlayerAnimationDriver`.

La visuale è composta da parti mesh primitive separate (testa, torso e arti), non da un modello Humanoid animabile. La scena non contiene uno `SkinnedMeshRenderer` né un `Animator`. Il controller può aggiungere alcuni componenti di fallback durante `Awake`, ma senza gli asset di input, animazione e rig non ripristina la configurazione completa.

Di conseguenza, la scena versionata è un prototipo visivo precedente e non costituisce una verifica del Player integrato attuale. Il generatore contiene già la configurazione aggiornata, ma la scena deve essere ricreata nell'Editor Unity e verificata in Play Mode prima di poter essere considerata operativa.

## Stato delle verifiche

Risultati presenti nel repository:

- Test autonomi C#: 6/6 superati.
- Unity EditMode: 12/12 superati nel report `TestResults/Unity-20261118-103717-325/EditMode.xml`. Il report comprende 7 test inventario e 5 test di configurazione locomozione.
- Unity PlayMode: 7/7 superati nel report `TestResults/UnderGrind/PlayMode_20261118_092745.xml`. Questo report precede l'aggiunta dei test PlayMode per sovraccarico, raccolta, rilascio e aggiornamento UI.
- La successiva esecuzione batch ha prodotto 12 test EditMode superati, ma il processo PlayMode è terminato con codice 1 prima di iniziare i test: Unity era già in uso dal processo EditMode. Il JSON aggregato è marcato Failed e non va interpretato come zero test effettivi.
- Il runner è stato modificato per attendere il completamento di ogni processo e validare anche i test inconclusivi. La revisione del runner e i nuovi test non sono ancora stati eseguiti nell'Editor Unity.
- La compilazione delle assembly runtime e di test è riuscita nel log EditMode del 18 novembre. Non è una prova di esecuzione PlayMode o di correttezza visiva.
- Verifica visiva del modello riggato e della scena: non documentata.

La verifica finale richiede una nuova esecuzione dei due script PowerShell di test sul commit aggiornato e un controllo visivo in Unity.
