# UnderGrind — scena di test locomozione

## Requisiti e importazione del personaggio

- Unity **6000.6.7f1**.
- Pacchetti del progetto: Input System e Unity Test Framework.
- Il modello sorgente è `Assets/UnderGrind/Art/Source/humans.blend`.
- Unity importa i file Blender tramite Blender installato sul computer. Se l'importazione non produce un modello nella finestra Project, aprire **Edit → Preferences → External Tools** e verificare che Unity trovi l'eseguibile Blender; poi selezionare il file e usare **Reimport**.

Il builder esegue una reimportazione forzata del modello prima di creare la scena. Se non trova geometria importata, interrompe la generazione e mostra un errore: non sostituisce più il personaggio reale con un manichino placeholder. Inoltre seleziona il GameObject radice del modello, non un singolo figlio (ad esempio torso o arto).

## Creazione della scena

1. Attendere che Unity termini l'importazione di `humans.blend`.
2. Verificare in **Edit → Project Settings → Player → Other Settings → Active Input Handling** che sia attivo **Input System Package (New)** oppure **Both**.
3. Dal menu scegliere **UnderGrind → Build First Locomotion Test Scene**.
4. Aprire `Assets/UnderGrind/Scenes/LocomotionTest.unity` e premere Play.

La scena contiene pavimento, marcatori di distanza, gradini, ostacoli, rampa e piattaforme con materiali distinti. Anche il personaggio riceve un materiale dedicato, distinto dal pavimento. Il builder sceglie lo shader in base alla pipeline effettivamente attiva: il progetto attuale non ha un URP Render Pipeline Asset assegnato in Graphics Settings, quindi usa lo shader Standard invece di URP/Lit (che in questo assetto viene visualizzato magenta). Il modello maschile viene scalato all’altezza definita nell’asset PlayerLocomotionSettings (1,78 m nel profilo iniziale). Le animazioni vengono collegate solo se il modello espone clip e Avatar compatibili; la locomozione non dipende dalla presenza di animazioni.

## Configurazione del Player dall'Inspector

Selezionare **Player - Human 1.78m** nella Hierarchy. Il componente **Human Locomotion Controller** contiene il riferimento all'asset `PlayerLocomotionSettings`, oltre ai riferimenti al rig camera e all'inventario. I valori di gameplay non sono duplicati sul componente.

L'asset di riferimento è `Assets/UnderGrind/Settings/PlayerLocomotionSettings.asset`. Selezionandolo nella finestra Project si possono modificare tutti gli otto gruppi:

- velocità di camminata;
- moltiplicatore e velocità di corsa;
- salto e altezza teorica;
- gravità e controllo aereo;
- accelerazione e decelerazione;
- postura accovacciata e collisioni;
- camera, sensibilità e distanza;
- coyote time e jump buffer.

Valori iniziali dell'asset:

| Parametro | Valore iniziale |
|---|---:|
| Altezza personaggio | 1,78 m |
| Camminata | 1,80 m/s |
| Corsa | 2,40 m/s |
| Moltiplicatore corsa | 1,3333× |
| Altezza teorica salto | 0,40 m |
| Gravità | 9,81 m/s² |
| Controllo aereo | 0,65 |
| Altezza accovacciata | 1,12 m |

Il custom Inspector del controller mostra i valori derivati e permette di selezionare l'asset. Le modifiche vanno effettuate sull'asset, così tutti i Player che lo referenziano condividono la stessa configurazione. Se il riferimento manca, il controller segnala l'errore e non esegue il movimento.

## Controlli e overlay

- WASD / frecce: movimento
- Shift: corsa
- Spazio: salto
- F5: alterna prima e terza persona
- Esc: libera il mouse; clic sinistro per riacquisirlo

L'overlay è ancorato all'area sicura dello schermo e mostra velocità effettiva, velocità nominali, moltiplicatore di corsa, salto, altezza, gravità, stato e visuale.

## Test e report automatici

Sono presenti assembly separati per EditMode e PlayMode. Dal menu **UnderGrind → Tests**:

- **Run EditMode Tests + Save Results**
- **Run PlayMode Tests + Save Results**
- **Choose Results Folder...**

La cartella predefinita è `TestResults/UnderGrind` nella root del progetto. È possibile scegliere una cartella diversa, purché interna al progetto. L'esportazione è automatica sia lanciando i test dai comandi UnderGrind sia dalla finestra standard **Window → General → Test Runner**: non serve premere Save Results. A fine esecuzione vengono salvati un XML con timestamp, un XML `Latest` e un riepilogo TXT. I callback vengono ripristinati anche dopo il domain reload che può avvenire durante i test PlayMode.

I report sono output generati e non vengono inclusi nei commit (la cartella `TestResults` è esclusa dal `.gitignore`).

## Limiti della verifica

La modifica dei file nel repository non equivale a una verifica nell'Editor Unity. Importazione Blender, compilazione C#, rendering effettivo, EditMode e PlayMode devono essere eseguiti sul progetto aperto in Unity. In particolare, se il modello non viene importato dal Blender installato sul computer, il builder si ferma intenzionalmente invece di mostrare un placeholder ambiguo.
