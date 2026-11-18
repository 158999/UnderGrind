# Player e locomozione umanoide

## Personaggio di riferimento

Il personaggio maschile è il riferimento dimensionale del Player e misura 1,78 m nel profilo iniziale. Il modello femminile misura 1,70 m. Entrambe le altezze sono configurabili nell'asset di rig.

La sorgente Blendstra disponibile contiene due armature e copie delle mesh originali. La separazione della testa non è ancora verificata: l'ispezione delle isole geometriche ha rilevato una singola isola connessa per corpo. Il file sorgente Blender del repository non è stato sostituito automaticamente; l'exporter `Tools/Blender/export-undergrind-humans.py` valida pesi e gerarchia prima di produrre i due FBX configurati in `PlayerRigSettings.asset`.

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

I valori iniziali sono contenuti nell'asset e modificabili dall'Inspector. I modelli iniziali dei parametri di input, interazione, UI, animazione, rig e graybox sono file JSON in `Assets/UnderGrind/Settings/Defaults`; i builder li applicano quando creano il relativo ScriptableObject. Le migrazioni versionate compilano i nuovi campi senza sovrascrivere le modifiche successive effettuate nell'Inspector.

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

`PlayerRigSettings.asset` contiene i percorsi FBX distinti per personaggio maschile e femminile, i requisiti di Avatar Humanoid e i token per i renderer. Il builder richiede entrambi i prefab validi e interrompe la generazione della scena se uno dei due manca o non soddisfa i requisiti. Non viene creato un prefab statico di ripiego.

`PlayerVisualRig` gestisce gruppi separati per testa, corpo, arti FP e arti TP. In prima persona la testa viene nascosta e il corpo viene mostrato quando la visuale guarda verso il basso, secondo soglie e isteresi configurabili. La logica è implementata; la verifica sui prefab definitivi è ancora da eseguire.

`PlayerAnimationSettings.asset` configura clip, loop, parametri Animator, soglie, transizioni e opzioni di importazione FBX. Il builder genera un controller con gli stati Idle, Walk, Run, Jump, Fall, Land, Crouch Idle e Crouch Walk. Le otto animazioni PINOC in formato `mixamo-fbx` non sono ancora importate nel repository né retargettate sul rig definitivo.

## Graybox

`PlayerGrayboxSettings.asset` configura pavimento, marcatori di distanza, gradini, scala, pareti, rampe, tunnel, passaggio stretto, slalom, ostacoli e piattaforme. La scena di test deve essere rigenerata nell'Editor Unity con i due prefab Humanoid validi prima della verifica in Play Mode.

## Risultati dei test e conservazione

Gli script PowerShell `Tools/Testing/run-tests.ps1` e `Tools/Testing/run-unity-tests.ps1` salvano i report in `TestResults/test`. La cartella `TestResults/test old` conserva l'esecuzione immediatamente precedente. A ogni nuova esecuzione la precedente cartella `test old` viene eliminata, `test` diventa `test old` e viene creata una nuova `test`. Il comando `Tools/Testing/run-all-tests.ps1` esegue entrambe le suite nella stessa cartella, effettuando una sola rotazione.

Ogni suite produce un JSON riepilogativo, un TXT, i log e i report XML/TRX disponibili. Un report assente, non valido o con zero test non viene considerato superato. I log Unity riportano separatamente gli errori di compilazione.

### Ultima esecuzione disponibile nel repository

- Test autonomi C#: **6/6 superati**, build diagnostica riuscita.
- Unity EditMode: **non eseguito**; compilazione bloccata da errori CS0123 in `PlayerInventoryUI.cs` e CS0103 in `PlayerAnimationSettings.cs`.
- Unity PlayMode: **non eseguito** per gli stessi errori di compilazione.
- Nessun conteggio pari a zero è interpretabile come test superati: Unity non ha prodotto XML valido per le due piattaforme.

La correzione delle firme evento e dei campi serializzati FBX è stata applicata nel sorgente; deve ancora essere verificata eseguendo nuovamente Unity 6000.6.7f1 e le suite aggiornate. La verifica visiva di scena, modelli, deformazione e animazioni non è documentata.
