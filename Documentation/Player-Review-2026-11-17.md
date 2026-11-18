# Revisione dell'implementazione del Player — 17 novembre 2026

## Ambito della revisione

La revisione ha confrontato il codice del Player, i test, la gerarchia dei modelli sorgente e i formati delle animazioni disponibili.

Sono stati esaminati:

- Responsabilità, correttezza, casi limite e copertura dei test dei componenti C#.
- Gerarchia e limiti dei modelli sorgente ispezionati in Blendstra.
- Formati effettivamente disponibili per i sei movimenti selezionati nella libreria PINOC.
- Stato del repository e risultati delle esecuzioni GitHub Actions.

Questa revisione riguarda codice e asset. Non costituisce una verifica visiva nell'Editor Unity.

## Risultati

### I modelli sorgente non consentono di nascondere soltanto la testa

La versione ispezionata contiene due oggetti mesh: una mesh statica completa per `Human_Male` e una per `Human_Female`. Nessuna delle due ha uno scheletro, pesi di deformazione o un renderer separato per la testa.

I test precedenti di `PlayerVisualRig` utilizzano una gerarchia artificiale con renderer distinti per testa, corpo e braccia. Dimostrano la logica di visibilità del componente, ma non verificano il comportamento sul modello sorgente reale.

È stato predisposto un progetto Blendstra separato per creare derivati riggati senza alterare le mesh originali. Il tentativo di generazione disponibile è però terminato con l'errore `DUPLICATE_OR_INVALID_OBJECT_ID`; non è stata salvata una versione utilizzabile. La separazione della testa e la pesatura delle mesh restano quindi da realizzare e verificare.

### I formati PINOC hanno finalità differenti

I sei movimenti selezionati espongono due famiglie di file FBX:

- `splat-skeleton-fbx`: scheletro MetaHuman di 441 ossa, comprese quelle facciali, senza mesh. È destinato ai personaggi PINOC utilizzati con Splat Engine e rig Generic.
- `mixamo-fbx`: mesh X Bot e scheletro con nomi Mixamo. È destinato al retargeting tramite Blender o altri strumenti DCC.

Per il normale rig umanoide di UnderGrind, il formato Mixamo è il candidato pertinente. X Bot è soltanto un modello di supporto al retargeting e non deve sostituire il corpo del giocatore. I sei movimenti risultano disponibili nella libreria, ma non sono ancora importati nel repository né trasferiti sul rig UnderGrind.

### Associazione dei renderer

- Le associazioni esplicite di `PlayerVisualRig` hanno priorità sulla ricerca euristica per nome.
- `ConfigureFromHierarchy` riconosce la convenzione `*_Head`, `*_FP_Arms` e `*_FP_Hands`.
- Un test PlayMode verifica la convenzione dei nomi, la visibilità della testa e delle braccia in prima persona e il mantenimento della visibilità di renderer non pertinenti, come i capelli.
- Il test utilizza una gerarchia sintetica: non certifica il modello generato.

### Sicurezza dei parametri Animator

`PlayerAnimationDriver` rileva la sostituzione del Runtime Animator Controller durante l'esecuzione e controlla l'esistenza e il tipo dei parametri prima di chiamare `SetFloat` o `SetBool`.

### Selezione delle clip

Il generatore della scena di test esclude le clip di accovacciamento dalla selezione degli stati normali di riposo, camminata, corsa e salto. Per gli stati di accovacciamento richiede invece clip con i relativi termini identificativi.

## Criticità ancora aperte

- La linea di separazione della testa e i pesi automatici devono essere verificati sul modello derivato.
- Non esiste ancora una mesh dedicata per braccia e mani in prima persona.
- La verifica di collisione della telecamera usa una maschera di layer configurabile: il prefab definitivo deve escludere il layer del giocatore oppure il filtro deve impedire in modo affidabile le collisioni con il corpo stesso.
- Il generatore della scena cerca attualmente clip incorporate in `humans.blend`; non importa né individua automaticamente i file FBX esterni provenienti da PINOC.
- Le soglie tra riposo, camminata e corsa sono tarate sui valori di 1,40 m/s e 1,8667 m/s, ma richiedono una verifica visiva durante la riproduzione.
- Nel repository non è ancora presente un Animator Controller di produzione né un Avatar Humanoid verificato per il modello definitivo.

## Modifiche presenti nel repository al 18 novembre 2026

### Locomozione e configurazione

- `PlayerLocomotionSettings` centralizza gli otto gruppi di parametri della locomozione.
- `HumanLocomotionController` usa l'asset per velocità, salto, gravità, postura, collisioni e camera.
- L'inventario applica la soglia di sovraccarico definita dall'utente: oltre soglia disabilita corsa e salto e riduce la camminata del 25% nel profilo iniziale.
- L'input è collegato tramite `PlayerInputSettings` e `PlayerInputActions.asset`. Le azioni e le binding sono modificabili nell'Inspector.
- L'interfaccia inventario sopprime il movimento e la visuale mentre è aperta; il cursore non viene ricatturato dal clic durante questa condizione.

### Inventario e interazione

- `PlayerInventory` gestisce slot, stack, selezione, peso e operazioni di trasferimento.
- `PlayerInventoryUI` visualizza slot, icone, quantità, selezione e peso tramite una Canvas overlay.
- `PlayerInteractionController` raccoglie oggetti con una SphereCast dalla camera e rilascia il contenuto dello slot selezionato.
- `WorldItemPickup` conserva la quantità che non trova spazio nell'inventario.
- I parametri di interazione e UI sono contenuti in asset ScriptableObject.

### Rig e animazioni

- `PlayerVisualRig` supporta associazioni esplicite e convenzioni di nome per testa e parti dedicate alla prima persona.
- `PlayerAnimationDriver` controlla i parametri effettivamente presenti nell'Animator e disabilita il root motion.
- `PlayerAnimationControllerBuilder` genera il controller a partire dalle clip importate e da `PlayerAnimationSettings.asset`. Otto movimenti PINOC sono stati aggiunti alla libreria personale; i relativi export `mixamo-fbx` sono disponibili ma non sono ancora stati importati nel repository né retargettati sul rig UnderGrind.
- `HumanoidRigPrefabBuilder` configura l'importazione Humanoid e genera prefab da un FBX con entrambe le radici scheletriche.
- La scena di test preferisce il prefab maschile riggato quando disponibile; la mesh statica resta soltanto un fallback visivo.
- La versione Blendstra `version_b8a60164bd0c3dbcb97cb79bf78d03069c370fc7c28abb093cd4025fbaf1a662` conserva le mesh originali, contiene copie con topologia invariata e due armature. La separazione per isole disconnesse ha confermato che ogni corpo è una singola isola connessa: non esiste una testa già separabile tramite loose parts. Il GLB dichiara zero animazioni. Posizione delle ossa e pesi restano non verificati; la testa non è un renderer separato. Il prototipo non è accettato come rig di produzione.

### Correzioni emerse dalla revisione

- Il runner Unity ora attende il completamento di ogni processo prima di lanciare la piattaforma successiva.
- Il report distingue test non eseguiti, XML assente, risultati inconcludenti, test falliti e codice di uscita non zero.
- L'UI si sottoscrive all'evento inventario anche quando i riferimenti vengono assegnati dopo `Awake`.
- L'apertura dell'inventario cancella il salto bufferizzato e impedisce la ricattura del cursore tramite clic.

## Verifiche e limiti

Risultati effettivamente presenti nei file di test:

- Test autonomi C#: 6/6 superati nel report `TestResults/latest.json`.
- Unity EditMode: 12/12 superati in `TestResults/Unity-20261118-103717-325/EditMode.xml`. Comprendeva 7 test inventario e 5 test di configurazione locomozione.
- Unity PlayMode: 7/7 superati in `TestResults/UnderGrind/PlayMode_20261118_092745.xml`. Quel report comprendeva la suite locomozione precedente alle nuove prove di sovraccarico, interazione e UI.

La successiva esecuzione batch del 18 novembre ha lanciato EditMode e PlayMode in parallelo. Il log EditMode documenta 12 test superati e codice di uscita 0; il log PlayMode termina con codice 1 prima dell'esecuzione, perché il progetto era già aperto dal processo EditMode. Il JSON aggregato è correttamente marcato Failed, ma i suoi conteggi a zero non rappresentano l'esito dei 12 test EditMode.

Dopo quei report sono stati aggiunti altri test:

- EditMode: 5 test per `PlayerInputSettings` e 4 ulteriori test inventario.
- PlayMode: 1 test di sovraccarico e 4 test per raccolta, rilascio, aggiornamento UI e ciclo di vita del cursore.

Il numero atteso dal codice corrente è quindi 21 test EditMode e 12 PlayMode. Questi nuovi test non sono ancora stati eseguiti. Anche il runner aggiornato non è stato ancora verificato con Windows PowerShell 5.1 e Unity 6000.6.7f1.

Non risultano verifiche GitHub Actions associate agli ultimi commit. La compilazione delle assembly runtime e di test è riuscita nel log EditMode citato, ma non dimostra il corretto funzionamento delle nuove suite PlayMode. Non è documentata una verifica visiva della scena né del modello riggato finale.

Il Player non può essere dichiarato completo finché non sono superati i test aggiornati, verificati i prefab Humanoid reali, separate e controllate le parti necessarie alla visuale in prima persona e validate le animazioni sul rig definitivo.
