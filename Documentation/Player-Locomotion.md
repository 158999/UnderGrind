# Player e locomozione umanoide

## Personaggio di riferimento

Il personaggio maschile è il riferimento dimensionale del Player e misura 1,78 m nel profilo iniziale. Il modello femminile misura 1,70 m. Entrambe le altezze sono configurabili nell'asset di rig.

La versione corrente del progetto Blendstra progetto Blendstra `UnderGrind — Humans Source Separation` (projectId `2cdfaa98-3ea2-4a49-892a-b96488e12bc3`), salvata come `version_0ef2040af3a2bcd46fae69870ffdcb6687ff1e6b07f07d38ba1a17945152df07`, conserva le due mesh originali complete e contiene due armature distinte più sei gruppi mesh per ciascun personaggio: Body, Head, TP Arms, TP Hands, FP Arms e FP Hands. La separazione della testa e degli arti è quindi presente nella struttura del modello corrente.

L'ispezione strutturale conferma nomi, geometrie e gerarchia degli oggetti, ma non espone i modificatori Armature né i pesi dei vertici. Il file `Assets/UnderGrind/Art/Source/humans.blend` già presente nel repository non è stato sostituito automaticamente con questa versione Blendstra. Prima dell'export occorre trasferire la versione corrente nel file sorgente locale e lanciare `Tools/Blender/export-undergrind-humans.py`: lo script verifica armature, ossa Humanoid richieste, parent diretto, modificatore Armature, gruppi deformanti e peso valido per ogni vertice. Questi controlli non sono ancora stati eseguiti sulla versione Blendstra corrente; non esistono ancora FBX e prefab Unity verificati.

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

`PlayerRigSettings.asset` contiene i percorsi FBX distinti per personaggio maschile e femminile, i requisiti di Avatar Humanoid, i token per i renderer e la variante predefinita della scena di test (`Male` o `Female`). Il builder valida entrambi i prefab e istanzia la variante selezionata. `HumanLocomotionController` riceve la stessa variante: altezza della capsula, verifica dello spazio per rialzarsi e altezza della visuale vengono calcolate dai valori maschili/femminili configurati in `PlayerLocomotionSettings`. L'altezza femminile della visuale è derivata in proporzione all'altezza del modello, mantenendo un'unica fonte dimensionale. Il builder richiede entrambi i prefab validi e interrompe la generazione della scena se uno dei due manca o non soddisfa i requisiti. Non viene creato un prefab statico di ripiego.

`PlayerVisualRig` gestisce gruppi separati per testa, corpo, arti FP e arti TP. In prima persona la testa viene nascosta e il corpo viene mostrato quando la visuale guarda verso il basso, secondo soglie e isteresi configurabili. La logica è implementata; la verifica sui prefab definitivi è ancora da eseguire.

`PlayerAnimationSettings.asset` configura clip, loop, parametri Animator, soglie, transizioni e opzioni di importazione FBX. Il builder genera un controller con gli stati Idle, Walk, Run, Jump, Fall, Land, Crouch Idle e Crouch Walk. Otto clip PINOC sono state aggiunte alla libreria dell'utente in formato `mixamo-fbx` e registrate in `Assets/UnderGrind/Animations/PlayerAnimationManifest.json`: Standing Idle, Walking, Running, Jump, Fall down, Hard Landing, Crouching Idle e Crouch Walk Forward. Il formato contiene uno scheletro Mixamo e una mesh X Bot di riferimento; non è il corpo UnderGrind. Le clip non sono ancora importate nel repository né retargettate sui due Avatar definitivi.

## Graybox

`PlayerGrayboxSettings.asset` configura pavimento, marcatori di distanza, gradini, scala, pareti, rampe, tunnel, passaggio stretto, slalom, ostacoli e piattaforme. La scena completa `LocomotionTest.unity` richiede entrambi i prefab Humanoid validi. Per provare subito movimento, collisioni e camera prima dell'importazione dei modelli, il menu `UnderGrind → Build Graybox Locomotion Test (Temporary Proxy)` crea `LocomotionGraybox.unity` con una capsula esplicitamente temporanea: non è il personaggio definitivo e non sostituisce i prefab maschile/femminile.

## Risultati dei test e conservazione

Gli script PowerShell `Tools/Testing/run-tests.ps1` e `Tools/Testing/run-unity-tests.ps1` salvano i report in `TestResults/test`. La cartella `TestResults/test old` conserva l'esecuzione immediatamente precedente. A ogni nuova esecuzione la precedente cartella `test old` viene eliminata, `test` diventa `test old` e viene creata una nuova `test`. Il comando `Tools/Testing/run-all-tests.ps1` esegue entrambe le suite nella stessa cartella, effettuando una sola rotazione.

Ogni suite produce un JSON riepilogativo, un TXT, i log e i report XML/TRX disponibili. Un report assente, non valido o con zero test non viene considerato superato. I log Unity riportano separatamente gli errori di compilazione.

### Ultima esecuzione disponibile nel repository

- Test autonomi C#: **6/6 superati**, build diagnostica riuscita.
- Unity EditMode: **non eseguito**; compilazione bloccata da `CS0103` in `PlayerRigSettings.cs(60,47)`: il campo `requireThirdPersonArms` era referenziato ma non dichiarato.
- Unity PlayMode: **non eseguito** per lo stesso errore. Non è stato prodotto un report NUnit XML valido, quindi il numero di test eseguiti è zero.
- Nessun conteggio pari a zero è interpretabile come test superati: Unity non ha prodotto XML valido per le due piattaforme.

La compilazione bloccata dal campo `requireThirdPersonArms` è stata corretta dichiarando il campo serializzato e aggiornando la migrazione dell'asset. La revisione statica ha inoltre corretto il binding dell'evento `PlayerInventory.Changed` in `PlayerInventoryUI` e completato i campi di importazione FBX in `PlayerAnimationSettings`, l'istanziazione esclusiva del prefab maschile e l'uso dell'altezza maschile nel controller quando viene selezionato il personaggio femminile. La variante selezionata ora governa CharacterController, verifica dello spazio per rialzarsi e altezza della visuale. È stata aggiunta una scena Graybox indipendente con proxy temporaneo e corretta la visibilità del corpo in prima persona quando il rig non è disponibile. Il threshold dell'animazione Run è stato corretto e il rebinding controller riceve il relativo asset di input dal builder della scena. Sono stati aggiunti test EditMode per queste condizioni.

Queste modifiche devono ancora essere verificate eseguendo nuovamente Unity 6000.6.7f1 e le suite aggiornate. L'ambiente di sviluppo remoto non dispone di Unity, PowerShell o .NET SDK: non è stata dichiarata una compilazione o un'esecuzione di test successiva alle correzioni. La verifica visiva di scena, modelli, deformazione e animazioni non è documentata.
